import {
  Badge,
  Button,
  ButtonLink,
  Card,
  EmptyState,
  ErrorState,
  MediaImage,
  PageHeader,
  Skeleton,
} from "@/shared/components/ui";
import { CheckoutRouteMap } from "@/features/checkout/components/CheckoutRouteMap";
import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  ArrowLeft,
  Clock3,
  CreditCard,
  LocateFixed,
  LockKeyhole,
  MapPin,
  ShieldCheck,
  Store,
  Wallet,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { checkoutApi } from "@/features/checkout/api/checkoutApi";
import type {
  CheckoutContext,
  CheckoutGps,
  CheckoutMethod,
  CheckoutQuote,
} from "@/features/checkout/types/checkout.types";
import { DepositPolicyPanel } from "@/features/checkout/components/DepositPolicyPanel";
import { CheckoutOrderReceipt } from "@/features/checkout/components/CheckoutOrderReceipt";
import {
  checkoutSelection,
  checkoutTime,
  safePaymentUrl,
} from "@/features/checkout/utils/checkoutPolicy";
import { CartItemRow } from "@/features/cart/components/CartItemRow";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { useCartStore } from "@/features/cart/stores/cartStore";
import { useAuthStore } from "@/shared/stores/authStore";
import { getApiErrorMessage } from "@/shared/api";
import { getApiErrorStatus } from "@/shared/api/httpClient";

function freshGps(): Promise<CheckoutGps> {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(
        new Error("Your browser does not support GPS. Please use a device with location access."),
      );
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (position) => {
        if (position.coords.accuracy > 1000) {
          reject(
            new Error(
              "Your GPS location is not accurate enough. Enable precise location and try again.",
            ),
          );
          return;
        }
        resolve({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracyMeters: position.coords.accuracy,
          capturedAt: new Date(position.timestamp).toISOString(),
        });
      },
      (error) =>
        reject(
          new Error(
            error.code === 1
              ? "Allow GPS access to place an order. A saved address cannot replace your current GPS location."
              : error.code === 3
                ? "Location request timed out. Please try again."
                : "Unable to find your GPS location. Check location permissions and try again.",
          ),
        ),
      { enableHighAccuracy: true, maximumAge: 0, timeout: 15000 },
    );
  });
}

export function CheckoutPage() {
  const [params] = useSearchParams();
  const userId = useAuthStore((state) => state.user?.id);
  const shopId = params.get("shopId");
  const orderId = params.get("orderId");
  if (orderId) return <CheckoutOrderReceipt key={`${userId}:${orderId}`} orderId={orderId} />;
  if (!shopId)
    return (
      <div className="space-y-6 py-6 sm:py-8">
        <PageHeader title="Checkout" />
        <EmptyState
          title="Choose a shop to check out"
          description="Select a shop in your cart to review its items and payment options."
          icon={Store}
          action={<ButtonLink to={APP_PATHS.CART}>Back to cart</ButtonLink>}
        />
      </div>
    );
  return <CheckoutFlow key={`${userId}:${shopId}`} shopId={shopId} />;
}

function CheckoutFlow({ shopId }: { shopId: string }) {
  const navigate = useNavigate();
  const [context, setContext] = useState<CheckoutContext | null>(null);
  const [quote, setQuote] = useState<CheckoutQuote | null>(null);
  const [method, setMethod] = useState<CheckoutMethod>("OnlinePayment");
  const [retry, setRetry] = useState(0);
  const [loading, setLoading] = useState(true);
  const [locating, setLocating] = useState(false);
  const [placing, setPlacing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [gpsError, setGpsError] = useState<string | null>(null);
  const [gpsLocation, setGpsLocation] = useState<CheckoutGps | null>(null);
  const [gpsAccuracy, setGpsAccuracy] = useState<number | null>(null);
  const [now, setNow] = useState(Date.now);
  const requestId = useRef<string | null>(null);
  const alive = useRef(true);
  const data = quote ?? context;
  const selection = checkoutSelection(data, method, quote, now);
  const secondsLeft = quote
    ? Math.max(0, Math.ceil((Date.parse(quote.expiresAt) - now) / 1000))
    : 0;

  useEffect(() => {
    alive.current = true;
    const abort = new AbortController();
    checkoutApi
      .context(shopId, abort.signal)
      .then((value) => {
        if (!abort.signal.aborted) setContext(value);
      })
      .catch((error) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(error));
      })
      .finally(() => {
        if (!abort.signal.aborted) setLoading(false);
      });
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => {
      alive.current = false;
      abort.abort();
      window.clearInterval(timer);
    };
  }, [shopId, retry]);

  function retryContext() {
    setError(null);
    setLoading(true);
    setRetry((value) => value + 1);
  }

  async function locate() {
    if (locating || placing) return;
    setLocating(true);
    setError(null);
    setGpsError(null);
    setGpsLocation(null);
    setQuote(null);
    requestId.current = null;
    try {
      const gps = await freshGps();
      if (!alive.current) return;
      setGpsLocation(gps);
      const next = await checkoutApi.quote(shopId, gps);
      if (alive.current) {
        setQuote(next);
        setContext(next);
        setGpsAccuracy(Math.ceil(gps.accuracyMeters));
        setNow(Date.now());
      }
    } catch (error) {
      if (alive.current) setGpsError(getApiErrorMessage(error));
    } finally {
      if (alive.current) setLocating(false);
    }
  }

  async function place() {
    if (!quote || !selection.canSubmit || locating || placing) return;
    setPlacing(true);
    setError(null);
    requestId.current ??= crypto.randomUUID();
    try {
      const order = await checkoutApi.place(quote.id, selection.method, requestId.current);
      if (!alive.current) return;
      void useCartStore.getState().refresh();
      navigate(`${APP_PATHS.CHECKOUT}?orderId=${order.id}`, { replace: true });
      const url = safePaymentUrl(order.checkoutUrl);
      if (order.paymentStatus === "Pending" && url) window.location.assign(url);
    } catch (error) {
      if (alive.current) {
        setError(getApiErrorMessage(error));
        if ([403, 409].includes(getApiErrorStatus(error) ?? 0)) {
          setQuote(null);
          try {
            const updated = await checkoutApi.context(shopId);
            if (alive.current) setContext(updated);
          } catch {
            /* Keep the checkout failure visible. */
          }
        }
      }
    } finally {
      if (alive.current) setPlacing(false);
    }
  }

  return (
    <div className="min-w-0 space-y-6 py-6 sm:py-8">
      <PageHeader
        title="Checkout"
        description="One shop, one order. Review your items, pickup time and payment before confirming."
        breadcrumb={
          <Link
            to={APP_PATHS.CART}
            className="inline-flex min-h-8 items-center gap-2 font-medium text-text-secondary hover:text-primary-700"
          >
            <ArrowLeft className="size-4" aria-hidden="true" />
            Back to cart
          </Link>
        }
        actions={
          <Badge tone="primary">
            <Store className="size-3" aria-hidden="true" />
            In-store pickup
          </Badge>
        }
      />
      {context?.isSimulatedPayment && (
        <div
          role="note"
          className="rounded-md border border-accent-100 bg-accent-50 px-4 py-3 text-caption text-accent-700"
        >
          <strong>DEVELOPMENT · Simulated payment</strong>
          <p className="mt-1">
            No real money is transferred. After placing your order, choose a successful, cancelled,
            or expired payment to test the flow.
          </p>
        </div>
      )}
      {error && !context && (
        <ErrorState title="Couldn't load checkout" description={error} onRetry={retryContext} />
      )}
      {error && context && (
        <div
          role="alert"
          className="mb-5 rounded-md border border-danger-soft bg-danger-soft p-4 text-body leading-relaxed text-danger-strong"
        >
          {error}
          <Link to={APP_PATHS.ACCOUNT_ORDERS} className="mt-2 block font-semibold underline">
            If your request was interrupted, check My orders before trying again.
          </Link>
        </div>
      )}
      {loading && (
        <div
          role="status"
          aria-label="Loading checkout"
          className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_22rem]"
        >
          <span className="sr-only">Checking your cart and payment policy…</span>
          <div className="space-y-4">
            <Skeleton className="h-72" />
            <Skeleton className="h-96" />
          </div>
          <Skeleton className="h-96" />
        </div>
      )}
      {data && data.shop.items.length === 0 && (
        <EmptyState
          title="No items to check out"
          description="This shop has no items in your checkout. Return to your cart to choose a deal."
          icon={Store}
          action={<ButtonLink to={APP_PATHS.CART}>Back to cart</ButtonLink>}
        />
      )}
      {data && data.shop.items.length > 0 && (
        <div className="grid items-start gap-4 lg:grid-cols-[minmax(0,1fr)_22rem]">
          <div className="min-w-0 space-y-4">
            <Card aria-label="Shop and checkout items" className="!p-0 overflow-hidden">
              <header className="flex items-start justify-between gap-3 border-b border-border-default bg-primary-50/50 p-4 sm:p-5">
                <Link
                  to={`/shops/${data.shop.shopId}`}
                  className="group flex min-w-0 items-center gap-3"
                >
                  <MediaImage
                    src={data.shop.shopLogoUrl ?? undefined}
                    alt=""
                    fallbackIcon={Store}
                    showFallbackLabel={false}
                    className="size-10 shrink-0 rounded-md"
                  />
                  <div className="min-w-0">
                    <p className="text-caption text-text-secondary">Your pickup shop</p>
                    <h2
                      title={data.shop.shopName}
                      className="line-clamp-2 text-section font-semibold text-text-primary group-hover:text-primary-700 [overflow-wrap:anywhere]"
                    >
                      {data.shop.shopName}
                    </h2>
                    <p
                      title={data.shop.shopAddress}
                      className="mt-1 truncate text-caption text-text-secondary"
                    >
                      {data.shop.shopAddress || "Pickup address not provided"}
                    </p>
                  </div>
                </Link>
                <Badge tone="primary" className="shrink-0 whitespace-nowrap">
                  {data.shop.totalQuantity} items
                </Badge>
              </header>
              <div className="px-4 sm:px-5">
                <ul aria-label="Checkout items">
                  {data.shop.items.map((item) => (
                    <CartItemRow key={item.id} item={item} compact />
                  ))}
                </ul>
              </div>
              <div className="border-t border-border-default px-4 py-3 sm:px-5">
                <Link
                  to={APP_PATHS.CART}
                  className="inline-flex min-h-8 items-center gap-2 text-body font-medium text-primary-700 hover:text-primary-800"
                >
                  <ArrowLeft className="size-4" aria-hidden="true" />
                  Edit items in cart
                </Link>
              </div>
            </Card>
            <Card>
              <div className="flex items-start gap-3">
                <LocateFixed aria-hidden="true" className="mt-1 shrink-0 text-primary-600" />
                <div>
                  <h2 className="text-section font-semibold text-text-primary">
                    GPS & estimated arrival
                  </h2>
                  <p className="mt-1 text-body leading-relaxed text-text-secondary">
                    Your current GPS location is required. Motorcycle travel time is calculated
                    using the route to the shop.
                  </p>
                </div>
              </div>
              <details className="mt-3 rounded-md border border-border-default bg-neutral-50 px-3">
                <summary className="min-h-11 cursor-pointer py-3 text-caption font-medium text-text-secondary">
                  How pickup time is calculated · GMT+7
                </summary>
                <p className="pb-3 text-caption leading-relaxed text-text-secondary">
                  Soft deadline = order time + max(travel time, 5 minutes). Buffer = max(30% of
                  travel time, 10 minutes), rounded up. At least 15 minutes must remain before the
                  deal ends. Ordering near closing does not extend pickup time. Estimates are
                  finalized when you place your order. All times are GMT+7.
                  {quote && (
                    <> The earliest deal ends at {checkoutTime(quote.pickup.dealEndsAt)}.</>
                  )}
                </p>
              </details>
              {gpsError && (
                <p
                  role="alert"
                  className="mt-3 rounded-md border border-danger-soft bg-danger-soft p-3 text-body text-danger-strong"
                >
                  {gpsError}
                </p>
              )}
              <Button
                variant="secondary"
                loading={locating}
                type="button"
                disabled={locating || placing}
                onClick={() => void locate()}
                className="mt-4 min-h-11 max-w-full whitespace-normal text-left"
              >
                <LocateFixed
                  aria-hidden="true"
                  size={18}
                  className={locating ? "animate-pulse" : ""}
                />
                {locating
                  ? "Getting your location & calculating the route…"
                  : quote
                    ? "Refresh GPS & check again"
                    : "Get my GPS location"}
              </Button>
              <CheckoutRouteMap
                shop={data.shop}
                gps={gpsLocation}
                route={quote?.route}
                loading={locating}
              />
              {quote && (
                <div className="mt-3 space-y-2">
                  <div className="flex flex-wrap gap-x-5 gap-y-2 text-body text-text-secondary">
                    <span className="inline-flex items-center gap-1.5">
                      <MapPin aria-hidden="true" size={16} />
                      {quote.pickup.distanceKm.toLocaleString("vi-VN")} km
                    </span>
                    <span className="inline-flex items-center gap-1.5">
                      <Clock3 aria-hidden="true" size={16} />
                      About {quote.pickup.etaMinutes} minutes by motorcycle
                    </span>
                    {gpsAccuracy !== null && <span>GPS accuracy: ±{gpsAccuracy} m</span>}
                  </div>
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="rounded-md bg-primary-50 p-4">
                      <p className="text-caption font-semibold uppercase tracking-wide text-primary-800">
                        Soft deadline · recommended arrival
                      </p>
                      <p className="mt-2 text-section font-semibold text-primary-900">
                        {checkoutTime(quote.pickup.softDeadlineAt)}
                      </p>
                      <p className="mt-1 text-caption text-primary-800">
                        Order placement time + {Math.max(5, quote.pickup.etaMinutes)} minutes
                        (minimum 5 minutes).
                      </p>
                    </div>
                    <div className="rounded-md border border-accent-100 bg-accent-50 p-4">
                      <p className="text-caption font-semibold uppercase tracking-wide text-accent-700">
                        Hard deadline · latest pickup
                      </p>
                      <p className="mt-2 text-section font-semibold text-accent-700">
                        {checkoutTime(quote.pickup.hardDeadlineAt)}
                      </p>
                      <p className="mt-1 text-caption text-accent-700">
                        Includes a {quote.pickup.bufferMinutes}-minute buffer, capped by the deal
                        end time.
                      </p>
                    </div>
                  </div>
                  {quote.pickup.bufferWasCapped && (
                    <p
                      role="status"
                      className="rounded-md bg-accent-50 p-3 text-body text-accent-700"
                    >
                      The buffer has been shortened to fit the deal window. You must still collect
                      your order before the hard deadline.
                    </p>
                  )}
                </div>
              )}
              {!quote && !locating && (
                <p className="mt-4 rounded-md bg-neutral-50 p-3 text-body text-text-secondary">
                  Your GPS location is missing or your quote needs refreshing. You can only place an
                  order if you can arrive before the deal ends.
                </p>
              )}
            </Card>
            <Card>
              <h2 className="flex items-center gap-2 text-section font-semibold text-text-primary">
                <CreditCard className="size-5 text-secondary-700" aria-hidden="true" />
                Choose a payment method
              </h2>
              <fieldset disabled={placing} className="mt-3 space-y-2">
                <legend className="sr-only">Order payment method</legend>
                <label
                  className={`flex cursor-pointer items-start gap-3 rounded-md border p-4 transition-colors focus-within:ring-2 focus-within:ring-primary-700 ${selection.method === "OnlinePayment" ? "border-primary-500 bg-primary-50/60 ring-1 ring-primary-500" : "border-border-default"}`}
                >
                  <input
                    type="radio"
                    name="checkout-payment"
                    value="OnlinePayment"
                    checked={selection.method === "OnlinePayment"}
                    onChange={() => setMethod("OnlinePayment")}
                    className="mt-1 accent-primary-600"
                  />
                  <CreditCard
                    aria-hidden="true"
                    size={21}
                    className="mt-0.5 shrink-0 text-primary-700"
                  />
                  <span className="min-w-0 flex-1">
                    <span className="block text-body font-semibold text-text-primary">
                      Pay 100% online
                    </span>
                    <span className="mt-1 block text-caption leading-relaxed text-text-secondary">
                      Pay {money(data.shop.subtotal)} online now. No remaining item balance at the
                      shop.
                    </span>
                  </span>
                </label>
                <label
                  className={`flex items-start gap-3 rounded-md border p-4 transition-colors focus-within:ring-2 focus-within:ring-primary-700 ${!data.policy.payAtShopAllowed ? "cursor-not-allowed border-border-default bg-neutral-50" : selection.method === "PayAtShop" ? "cursor-pointer border-primary-500 bg-primary-50/60 ring-1 ring-primary-500" : "cursor-pointer border-border-default"}`}
                >
                  <input
                    type="radio"
                    name="checkout-payment"
                    value="PayAtShop"
                    checked={selection.method === "PayAtShop"}
                    disabled={!data.policy.payAtShopAllowed}
                    onChange={() => setMethod("PayAtShop")}
                    className="mt-1 accent-primary-600"
                  />
                  <Wallet
                    aria-hidden="true"
                    size={21}
                    className="mt-0.5 shrink-0 text-text-secondary"
                  />
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center gap-2 text-body font-semibold text-text-primary">
                      Pay at shop
                      {!data.policy.payAtShopAllowed && (
                        <LockKeyhole aria-hidden="true" size={16} />
                      )}
                    </span>
                    <span className="mt-1 block text-caption leading-relaxed text-text-secondary">
                      {data.policy.payAtShopAllowed
                        ? `Pay a ${money(data.policy.requiredDeposit)} deposit online and ${money(data.shop.subtotal - data.policy.requiredDeposit)} at pickup.`
                        : "Unavailable for this order. See the reason in Your payment policy."}
                    </span>
                  </span>
                </label>
              </fieldset>
              {!data.onlinePaymentAvailable && (
                <p
                  role="status"
                  className="mt-4 rounded-md border border-accent-100 bg-accent-50 p-4 text-body leading-relaxed text-accent-700"
                >
                  This shop cannot accept online payments yet. Orders requiring online payment or a
                  deposit cannot be placed. Eligible orders with no deposit can still use Pay at
                  shop.
                </p>
              )}
            </Card>

            <DepositPolicyPanel context={data} />
          </div>
          <aside
            aria-label="Payment summary"
            className="min-w-0 rounded-lg border border-border-default bg-bg-surface p-4 shadow-sm sm:p-5 lg:sticky lg:top-24"
          >
            <h2 className="flex items-center gap-2 text-section font-semibold text-text-primary">
              <ShieldCheck className="size-5 text-primary-700" aria-hidden="true" />
              Payment summary
            </h2>
            <p
              title={`For ${data.shop.shopName} only`}
              className="mt-2 line-clamp-2 text-body text-text-secondary [overflow-wrap:anywhere]"
            >
              For {data.shop.shopName} only
            </p>
            <dl className="mt-4 space-y-3 text-body">
              <div className="flex justify-between gap-3">
                <dt className="text-text-secondary">Order total</dt>
                <dd className="font-semibold tabular-nums">{money(data.shop.subtotal)}</dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-text-secondary">Payment method</dt>
                <dd className="text-right font-semibold">
                  {selection.method === "OnlinePayment"
                    ? "Online 100%"
                    : "Online deposit + pay at shop"}
                </dd>
              </div>
              <div className="flex justify-between gap-3 border-t border-border-default pt-4">
                <dt className="font-semibold">
                  {selection.method === "PayAtShop" ? "Deposit due online" : "Due online now"}
                </dt>
                <dd className="text-section font-bold tabular-nums text-primary-700">
                  {money(selection.dueOnline)}
                </dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-text-secondary">Remaining at shop</dt>
                <dd className="font-bold tabular-nums">{money(selection.remainingAtShop)}</dd>
              </div>
            </dl>
            {!data.shop.canCheckout && (
              <p
                role="status"
                className="mt-4 rounded-md bg-accent-50 p-3 text-body text-accent-700"
              >
                Some items are unavailable or have changed. Review your cart before placing this
                order.
              </p>
            )}
            {quote && (
              <p
                role="status"
                className={`mt-5 rounded-md p-3 text-body ${selection.expired ? "bg-accent-50 text-accent-700" : "bg-neutral-50 text-text-secondary"}`}
              >
                {selection.expired
                  ? "Your quote has expired. Refresh GPS to check prices and policies again."
                  : `Quote expires in ${secondsLeft}s. Prices, stock, and policies are checked again when you place your order.`}
              </p>
            )}
            <Button
              size="lg"
              loading={placing}
              type="button"
              disabled={!selection.canSubmit || placing || locating}
              onClick={() => void place()}
              className="mt-5 w-full whitespace-normal !px-3 py-3"
            >
              <ShieldCheck aria-hidden="true" size={18} className="shrink-0" />
              {placing
                ? "Placing your order…"
                : selection.dueOnline === 0
                  ? "Place order · No online payment required"
                  : selection.method === "PayAtShop"
                    ? `Place order & deposit ${money(selection.dueOnline)}`
                    : `Place order & pay ${money(selection.dueOnline)}`}
            </Button>
            <p className="mt-3 text-caption leading-relaxed text-text-secondary">
              {!quote
                ? "Get your GPS location and check travel time to continue."
                : "Payment is confirmed only after verification by the payment provider. Pickup deadlines do not extend while you pay."}
            </p>
          </aside>
        </div>
      )}
    </div>
  );
}
