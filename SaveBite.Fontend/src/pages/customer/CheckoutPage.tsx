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
      <div className="py-10">
        <p role="alert" className="text-amber-800">
          Select a shop in your cart to proceed to checkout.
        </p>
        <Link to={APP_PATHS.CART} className="mt-4 inline-block font-semibold text-emerald-700">
          Back to cart
        </Link>
      </div>
    );
  return <CheckoutFlow key={`${userId}:${shopId}`} shopId={shopId} />;
}

function CheckoutFlow({ shopId }: { shopId: string }) {
  const navigate = useNavigate();
  const [context, setContext] = useState<CheckoutContext | null>(null);
  const [quote, setQuote] = useState<CheckoutQuote | null>(null);
  const [method, setMethod] = useState<CheckoutMethod>("OnlinePayment");
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
  }, [shopId]);

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
    <div className="w-full min-w-0 py-5 md:py-6">
      <Link
        to={APP_PATHS.CART}
        className="mb-4 inline-flex items-center gap-2 text-sm font-medium text-neutral-500 hover:text-emerald-700"
      >
        <ArrowLeft size={16} />
        Back to cart
      </Link>
      {context?.isSimulatedPayment && (
        <div className="mb-5 rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900">
          <strong>DEVELOPMENT · Simulated payment</strong>
          <p>
            No real money is transferred. After placing your order, choose a successful, cancelled,
            or expired payment to test the flow.
          </p>
        </div>
      )}
      <h1 className="text-2xl font-bold text-neutral-900">Checkout</h1>
      <p className="mb-5 mt-1 text-sm text-neutral-500">
        Each order contains items from one shop. Review your deposit and pickup deadlines before
        confirming.
      </p>
      {error && (
        <div
          role="alert"
          className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm leading-relaxed text-red-800"
        >
          {error}
          <Link to={APP_PATHS.ACCOUNT_ORDERS} className="mt-2 block font-semibold underline">
            If your request was interrupted, check My orders before trying again.
          </Link>
        </div>
      )}
      {loading && (
        <p
          role="status"
          className="rounded-xl border border-neutral-200 bg-white p-10 text-center text-neutral-500"
        >
          Checking your cart and payment policy…
        </p>
      )}
      {data && (
        <div className="grid items-start gap-4 lg:grid-cols-[minmax(0,1fr)_360px]">
          <div className="min-w-0 space-y-4">
            <DepositPolicyPanel context={data} />
            <section className="rounded-xl border border-neutral-200 bg-white p-4 sm:p-5">
              <div className="flex items-start gap-3">
                <LocateFixed className="mt-1 shrink-0 text-emerald-600" />
                <div>
                  <h2 className="text-base font-semibold text-neutral-900">
                    GPS & estimated arrival
                  </h2>
                  <p className="mt-1 text-sm leading-relaxed text-neutral-500">
                    Your current GPS location is required. Motorcycle travel time is calculated
                    using the route to the shop.
                  </p>
                </div>
              </div>
              <p className="mt-4 rounded-lg bg-neutral-50 p-3 text-xs leading-relaxed text-neutral-600">
                Soft deadline = order time + max(travel time, 5 minutes). Buffer = max(30% of travel
                time, 10 minutes), rounded up. At least 15 minutes must remain before the deal ends.
                Ordering near closing does not extend pickup time. Estimates are finalized when you
                place your order. All times are GMT+7.
                {quote && <> The earliest deal ends at {checkoutTime(quote.pickup.dealEndsAt)}.</>}
              </p>
              {gpsError && (
                <p
                  role="alert"
                  className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800"
                >
                  {gpsError}
                </p>
              )}
              <button
                type="button"
                disabled={locating || placing}
                onClick={() => void locate()}
                className="mt-5 inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-2.5 text-sm font-bold text-emerald-800 hover:bg-emerald-100 disabled:opacity-50"
              >
                <LocateFixed size={18} className={locating ? "animate-pulse" : ""} />
                {locating
                  ? "Getting your location & calculating the route…"
                  : quote
                    ? "Refresh GPS & check again"
                    : "Get my GPS location"}
              </button>
              <CheckoutRouteMap
                shop={data.shop}
                gps={gpsLocation}
                route={quote?.route}
                loading={locating}
              />
              {quote && (
                <div className="mt-3 space-y-2">
                  <div className="flex flex-wrap gap-x-5 gap-y-2 text-sm text-neutral-600">
                    <span className="inline-flex items-center gap-1.5">
                      <MapPin size={16} />
                      {quote.pickup.distanceKm.toLocaleString("vi-VN")} km
                    </span>
                    <span className="inline-flex items-center gap-1.5">
                      <Clock3 size={16} />
                      About {quote.pickup.etaMinutes} minutes by motorcycle
                    </span>
                    {gpsAccuracy !== null && <span>GPS accuracy: ±{gpsAccuracy} m</span>}
                  </div>
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="rounded-xl bg-emerald-50 p-4">
                      <p className="text-xs font-semibold uppercase tracking-wide text-emerald-800">
                        Soft deadline · recommended arrival
                      </p>
                      <p className="mt-2 text-base font-semibold text-emerald-950">
                        {checkoutTime(quote.pickup.softDeadlineAt)}
                      </p>
                      <p className="mt-1 text-xs text-emerald-800">
                        Order placement time + {Math.max(5, quote.pickup.etaMinutes)} minutes
                        (minimum 5 minutes).
                      </p>
                    </div>
                    <div className="rounded-xl border border-amber-200 bg-amber-50 p-4">
                      <p className="text-xs font-semibold uppercase tracking-wide text-amber-900">
                        Hard deadline · latest pickup
                      </p>
                      <p className="mt-2 text-base font-semibold text-amber-950">
                        {checkoutTime(quote.pickup.hardDeadlineAt)}
                      </p>
                      <p className="mt-1 text-xs text-amber-900">
                        Includes a {quote.pickup.bufferMinutes}-minute buffer, capped by the deal
                        end time.
                      </p>
                    </div>
                  </div>
                  {quote.pickup.bufferWasCapped && (
                    <p role="status" className="rounded-lg bg-amber-50 p-3 text-sm text-amber-900">
                      The buffer has been shortened to fit the deal window. You must still collect
                      your order before the hard deadline.
                    </p>
                  )}
                </div>
              )}
              {!quote && !locating && (
                <p className="mt-4 rounded-lg bg-neutral-50 p-3 text-sm text-neutral-600">
                  Your GPS location is missing or your quote needs refreshing. You can only place an
                  order if you can arrive before the deal ends.
                </p>
              )}
            </section>
            <section className="rounded-xl border border-neutral-200 bg-white p-4 sm:p-5">
              <h2 className="text-base font-semibold text-neutral-900">Choose a payment method</h2>
              <fieldset disabled={placing} className="mt-3 space-y-2">
                <legend className="sr-only">Order payment method</legend>
                <label
                  className={`flex cursor-pointer items-start gap-3 rounded-lg border p-3 ${selection.method === "OnlinePayment" ? "border-emerald-500 bg-emerald-50/60 ring-1 ring-emerald-500" : "border-neutral-200"}`}
                >
                  <input
                    type="radio"
                    name="checkout-payment"
                    value="OnlinePayment"
                    checked={selection.method === "OnlinePayment"}
                    onChange={() => setMethod("OnlinePayment")}
                    className="mt-1 accent-emerald-600"
                  />
                  <CreditCard size={21} className="mt-0.5 shrink-0 text-emerald-700" />
                  <span className="min-w-0 flex-1">
                    <span className="block text-sm font-semibold text-neutral-900">
                      Pay 100% online
                    </span>
                    <span className="mt-1 block text-xs leading-relaxed text-neutral-600">
                      Pay {money(data.shop.subtotal)} online now. No remaining item balance at the
                      shop.
                    </span>
                  </span>
                </label>
                <label
                  className={`flex items-start gap-3 rounded-lg border p-3 ${!data.policy.payAtShopAllowed ? "cursor-not-allowed border-neutral-200 bg-neutral-50" : selection.method === "PayAtShop" ? "cursor-pointer border-emerald-500 bg-emerald-50/60 ring-1 ring-emerald-500" : "cursor-pointer border-neutral-200"}`}
                >
                  <input
                    type="radio"
                    name="checkout-payment"
                    value="PayAtShop"
                    checked={selection.method === "PayAtShop"}
                    disabled={!data.policy.payAtShopAllowed}
                    onChange={() => setMethod("PayAtShop")}
                    className="mt-1 accent-emerald-600"
                  />
                  <Wallet size={21} className="mt-0.5 shrink-0 text-neutral-600" />
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center gap-2 text-sm font-semibold text-neutral-900">
                      Pay at shop
                      {!data.policy.payAtShopAllowed && <LockKeyhole size={16} />}
                    </span>
                    <span className="mt-1 block text-xs leading-relaxed text-neutral-600">
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
                  className="mt-4 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm leading-relaxed text-amber-900"
                >
                  This shop cannot accept online payments yet. Orders requiring online payment or a
                  deposit cannot be placed. Eligible orders with no deposit can still use Pay at
                  shop.
                </p>
              )}
            </section>
            <section className="rounded-xl border border-neutral-200 bg-white p-4 sm:p-5">
              <Link
                to={`/shops/${data.shop.shopId}`}
                className="inline-flex items-center gap-2 text-base font-semibold text-neutral-900 hover:text-emerald-700"
              >
                <Store size={21} />
                {data.shop.shopName}
              </Link>
              <p className="mt-1 text-sm text-neutral-500">{data.shop.shopAddress}</p>
              <ul className="mt-4">
                {data.shop.items.map((item) => (
                  <CartItemRow key={item.id} item={item} compact />
                ))}
              </ul>
            </section>
          </div>
          <aside className="rounded-xl border border-neutral-200 bg-white p-4 shadow-xs sm:p-5 lg:sticky lg:top-24">
            <h2 className="text-base font-semibold text-neutral-900">Payment summary</h2>
            <p className="mt-2 text-sm text-neutral-500">For {data.shop.shopName} only</p>
            <dl className="mt-4 space-y-3 text-sm">
              <div className="flex justify-between gap-3">
                <dt className="text-neutral-500">Order total</dt>
                <dd className="font-semibold tabular-nums">{money(data.shop.subtotal)}</dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-neutral-500">Payment method</dt>
                <dd className="text-right font-semibold">
                  {selection.method === "OnlinePayment"
                    ? "Online 100%"
                    : "Online deposit + pay at shop"}
                </dd>
              </div>
              <div className="flex justify-between gap-3 border-t border-neutral-100 pt-4">
                <dt className="font-semibold">
                  {selection.method === "PayAtShop" ? "Deposit due online" : "Due online now"}
                </dt>
                <dd className="text-xl font-bold tabular-nums text-emerald-700">
                  {money(selection.dueOnline)}
                </dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-neutral-500">Remaining at shop</dt>
                <dd className="font-bold tabular-nums">{money(selection.remainingAtShop)}</dd>
              </div>
            </dl>
            {quote && (
              <p
                role="status"
                className={`mt-5 rounded-lg p-3 text-sm ${selection.expired ? "bg-amber-50 text-amber-900" : "bg-neutral-50 text-neutral-600"}`}
              >
                {selection.expired
                  ? "Your quote has expired. Refresh GPS to check prices and policies again."
                  : `Quote expires in ${secondsLeft}s. Prices, stock, and policies are checked again when you place your order.`}
              </p>
            )}
            <button
              type="button"
              disabled={!selection.canSubmit || placing || locating}
              onClick={() => void place()}
              className="mt-5 flex min-h-12 w-full items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-3 text-sm font-bold text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:bg-neutral-200 disabled:text-neutral-500"
            >
              <ShieldCheck size={18} className="shrink-0" />
              {placing
                ? "Placing your order…"
                : selection.dueOnline === 0
                  ? "Place order · No online payment required"
                  : selection.method === "PayAtShop"
                    ? `Place order & deposit ${money(selection.dueOnline)}`
                    : `Place order & pay ${money(selection.dueOnline)}`}
            </button>
            <p className="mt-3 text-xs leading-relaxed text-neutral-500">
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
