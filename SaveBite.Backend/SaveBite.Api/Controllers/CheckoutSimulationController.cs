using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

// Development-only payment simulation for customers and authorized shop operators.
[ApiController]
[Route("api/payments/simulation")]
[Route("api/checkout/simulation")]
public sealed class CheckoutSimulationController : ControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ICheckoutRepository _repository;
    private readonly ICheckoutPaymentService _payments;
    private readonly TimeProvider _clock;

    public CheckoutSimulationController(
        IHostEnvironment environment,
        IConfiguration configuration,
        ICheckoutRepository repository,
        ICheckoutPaymentService payments,
        TimeProvider clock)
    {
        _environment = environment;
        _configuration = configuration;
        _repository = repository;
        _payments = payments;
        _clock = clock;
    }

    // Select paid, cancelled, or expired for the initial payment or an optional remaining-payment ID.
    public sealed record SimulationRequest(string Outcome, Guid? PaymentId = null);
    // Load the customer's simulated initial or remaining payment after checking Development mode and ownership.
    [HttpGet("{orderId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> Get(Guid orderId, CancellationToken ct, [FromQuery] Guid? paymentId = null)
    {
        EnsureEnabled();
        var order = await _repository.GetOrderAsync(orderId, UserId(), ct) ?? throw AppException.NotFound();
        // Use the selected Remaining payment when paymentId is supplied; otherwise use the initial payment.
        var selected = paymentId.HasValue ? order.Payments.SingleOrDefault(x => x.Id == paymentId && x.PaymentType == PaymentType.Remaining) : CheckoutOrderMapper.RequiredPayment(order);
        EnsureSimulated(selected?.GatewayTransactionId);
        var result = await _payments.GetAsync(UserId(), orderId, ct);
        if (paymentId.HasValue)
        {
            order = await _repository.GetOrderAsync(orderId, UserId(), ct) ?? throw AppException.NotFound();
            selected = order.Payments.Single(x => x.Id == paymentId);
            // Display the selected remaining-payment amount and its original hard pickup deadline.
            result = result with { RequiredOnlineAmount = selected.Amount, PaymentStatus = selected.Status.ToString(), PaymentExpiresAt = order.HardDeadlineAt };
        }
        return Ok(ApiResponse<CheckoutOrderResponse>.Ok(result));
    }

    // Record one simulated outcome and reconcile it through the shared payment and inventory workflow.
    [HttpPost("{orderId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> Resolve(Guid orderId, SimulationRequest request, CancellationToken ct)
    {
        EnsureEnabled();
        return await ResolveCoreAsync(orderId, request, UserId(), ct);
    }

    // Let authorized shop operators simulate only their shop's remaining QR payment in enabled Development mode.
    [HttpPost("shops/{shopId:guid}/{orderId:guid}/remaining")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> ResolveRemaining(Guid shopId, Guid orderId,
        SimulationRequest request, [FromServices] IShopOrderRepository shops, CancellationToken ct)
    {
        EnsureEnabled();
        if (!request.PaymentId.HasValue) throw AppException.BadRequest("Choose a remaining payment.");
        await shops.RequireAccessAsync(UserId(), shopId, ct);
        if (await shops.GetAsync(shopId, orderId, ct) == null) throw AppException.NotFound();
        return await ResolveCoreAsync(orderId, request, null, ct, shopId, shops);
    }

    // Store a simulated outcome under the order lock and use normal reconciliation to notify realtime subscribers.
    private async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> ResolveCoreAsync(Guid orderId, SimulationRequest request,
        Guid? userId, CancellationToken ct, Guid? shopId = null, IShopOrderRepository? shops = null)
    {
        EnsureEnabled();
        if (request.Outcome is not ("paid" or "cancelled" or "expired")) throw AppException.BadRequest("Invalid simulation outcome.");
        // Lock the order so concurrent clicks cannot record conflicting outcomes.
        await _repository.WithOrderLockAsync(orderId, async () =>
        {
            var order = await _repository.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound();
            if (shopId.HasValue)
            {
                await shops!.RequireAccessAsync(UserId(), shopId.Value, ct);
                if (order.ShopId != shopId.Value) throw AppException.NotFound();
            }
            var payment = request.PaymentId.HasValue ? order.Payments.SingleOrDefault(x => x.Id == request.PaymentId && x.PaymentType == PaymentType.Remaining) : CheckoutOrderMapper.RequiredPayment(order);
            EnsureSimulated(payment?.GatewayTransactionId);
            // Remaining payments are eligible during preparation and expire at the hard pickup deadline.
            var remaining = request.PaymentId.HasValue;
            var active = remaining ? order.Status is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup : order.Status == OrderStatus.AwaitingPayment;
            if (!active || payment!.Status != PaymentStatus.Pending
                || (remaining ? order.HardDeadlineAt : order.PaymentExpiresAt) <= _clock.GetUtcNow().UtcDateTime)
                throw AppException.Conflict("This payment is already resolved or expired.");
            // Accept only the first outcome, even before payment reconciliation finishes.
            if (payment.GatewayTransactionId != DevelopmentCheckoutGateway.Prefix + "PENDING")
                throw AppException.Conflict("A simulation outcome has already been selected.");
            payment.GatewayTransactionId = DevelopmentCheckoutGateway.Prefix + (request.Outcome switch
            {
                "paid" => "PAID", "cancelled" => "CANCELLED", _ => "EXPIRED"
            });
            if (request.Outcome == "expired" && !remaining) order.PaymentExpiresAt = _clock.GetUtcNow().UtcDateTime;
            await _repository.SaveAsync(ct);
            return true;
        }, ct);
        // Reconcile the saved outcome through the shared order, payment, and inventory workflow.
        // Cancelling a remaining-payment attempt does not cancel the already-confirmed order.
        var result = !userId.HasValue
            ? await _payments.ReconcileOrderAsync(orderId, ct: ct)
            : request.Outcome == "cancelled" && !request.PaymentId.HasValue
                ? await _payments.CancelAsync(userId.Value, orderId, ct)
                : await _payments.GetAsync(userId.Value, orderId, ct);
        return Ok(ApiResponse<CheckoutOrderResponse>.Ok(result));
    }
    // Return not found unless simulated payments are enabled in Development.
    private void EnsureEnabled()
    {
        if (!_environment.IsDevelopment() || _configuration["Checkout:PaymentProvider"] != "Simulated") throw AppException.NotFound();
    }
    // Reject payments that are not marked as simulated.
    private static void EnsureSimulated(string? transactionId)
    {
        if (transactionId?.StartsWith(DevelopmentCheckoutGateway.Prefix) != true) throw AppException.NotFound("Simulated payment not found.");
    }
    // Read the order owner ID from JWT claims rather than a client-supplied user ID.
    private Guid UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
