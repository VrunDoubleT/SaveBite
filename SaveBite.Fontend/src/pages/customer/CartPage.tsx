import { useEffect, useRef } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, ArrowRight, ShoppingBag, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { useCartStore } from "@/features/cart/stores/cartStore";
import { CartItemRow } from "@/features/cart/components/CartItemRow";
import { formatCartMoney } from "@/features/cart/utils/formatCartMoney";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";

export function CartPage() {
  const {
    cart,
    isLoading,
    isMutating,
    error,
    refreshPages: refresh,
    updateQuantity,
    removeItem,
    hasMore,
    isLoadingMore,
    loadMore,
  } = useCartStore();
  const navigate = useNavigate();

  const sentinel = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!hasMore || isLoading || isMutating || isLoadingMore || error || !sentinel.current) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) void loadMore();
      },
      { rootMargin: "300px" },
    );
    observer.observe(sentinel.current);
    return () => observer.disconnect();
  }, [hasMore, isLoading, isMutating, isLoadingMore, error, loadMore, cart?.shops.length]);
  useEffect(() => {
    useCartStore.getState().setPageActive(true);
    void refresh();
    return () => useCartStore.getState().setPageActive(false);
  }, [refresh]);

  async function change(action: () => Promise<void>) {
    try {
      await action();
    } catch (error) {
      toast.error(getApiErrorMessage(error));
      await refresh();
    }
  }

  return (
    <div className="w-full min-w-0 py-5 md:py-6">
      <Link
        to={APP_PATHS.HOME}
        className="mb-4 inline-flex items-center gap-2 text-sm font-medium text-neutral-500 hover:text-emerald-700"
      >
        <ArrowLeft size={16} /> Continue exploring
      </Link>
      <div className="mb-5 flex items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-neutral-900">Your cart</h1>
          <p className="mt-1 text-sm text-neutral-500">
            Checkout with any shop below. Items from other shops stay in your cart.
          </p>
        </div>
      </div>
      {error && (
        <div
          role="alert"
          className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
        >
          {error}{" "}
          <button
            type="button"
            onClick={() => void refresh()}
            className="ml-2 font-semibold underline"
          >
            Try again
          </button>
        </div>
      )}
      {!cart && isLoading && (
        <div
          role="status"
          className="rounded-2xl border border-neutral-200 bg-white p-12 text-center text-neutral-500"
        >
          Loading your cart…
        </div>
      )}
      {cart?.shops.length === 0 && (
        <div className="rounded-2xl border border-neutral-200 bg-white px-6 py-16 text-center">
          <ShoppingBag size={44} className="mx-auto text-emerald-600" />
          <h2 className="mt-5 text-xl font-bold text-neutral-900">Your cart is empty</h2>
          <p className="mt-1 text-sm text-neutral-500">Discover a deal and save a meal today.</p>
          <Link
            to={APP_PATHS.HOME}
            className="mt-6 inline-flex rounded-xl bg-emerald-600 px-6 py-3 text-sm font-bold text-white hover:bg-emerald-700"
          >
            Explore deals
          </Link>
        </div>
      )}
      {!!cart?.shops.length && (
        <div className="space-y-4">
          {cart.shops.map((shop) => (
            <section
              key={shop.shopId}
              className="overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-xs"
            >
              <div className="flex items-center gap-3 bg-neutral-50/80 px-4 py-3 sm:px-5">
                <Link
                  to={`/shops/${shop.shopId}`}
                  aria-label={`Visit ${shop.shopName}`}
                  className="group flex min-w-0 flex-1 items-center gap-3 rounded-lg focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-emerald-600"
                >
                  {shop.shopLogoUrl ? (
                    <img
                      src={shop.shopLogoUrl}
                      alt=""
                      className="size-8 shrink-0 rounded-full object-cover"
                    />
                  ) : (
                    <Store size={20} className="shrink-0 text-emerald-600" />
                  )}
                  <span className="min-w-0">
                    <span className="block truncate text-sm font-semibold text-neutral-900 group-hover:text-emerald-700">
                      {shop.shopName}
                    </span>
                    <span className="mt-0.5 block truncate text-xs text-neutral-500">
                      {shop.shopAddress}
                    </span>
                    <span className="mt-0.5 block text-xs text-neutral-500">
                      {shop.totalQuantity} items
                    </span>
                  </span>
                </Link>
                <button
                  type="button"
                  disabled={!shop.canCheckout || isMutating || isLoading || !!error}
                  onClick={() =>
                    navigate(`${APP_PATHS.CHECKOUT}?shopId=${encodeURIComponent(shop.shopId)}`)
                  }
                  aria-label={`Checkout with ${shop.shopName}`}
                  className="inline-flex h-10 shrink-0 items-center justify-center gap-2 rounded-lg bg-emerald-600 px-3 text-xs font-semibold text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:bg-neutral-200 disabled:text-neutral-500 sm:px-4"
                >
                  Checkout <ArrowRight size={16} className="shrink-0" />
                </button>
              </div>
              <ul className="px-4 sm:px-5" aria-label={`Items from ${shop.shopName}`}>
                {shop.items.map((item) => (
                  <CartItemRow
                    key={item.id}
                    item={item}
                    compact
                    busy={isMutating || isLoading}
                    onQuantity={(quantity) => void change(() => updateQuantity(item.id, quantity))}
                    onRemove={() => void change(() => removeItem(item.id))}
                  />
                ))}
              </ul>
              {!shop.canCheckout && (
                <p
                  role="status"
                  className="mx-4 mb-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-800 sm:mx-5"
                >
                  Remove unavailable items or adjust quantities before checking out with this shop.
                </p>
              )}
              <div className="flex justify-between border-t border-neutral-100 px-4 py-3 text-sm sm:px-5">
                <span className="text-neutral-500">Shop subtotal</span>
                <span className="font-bold text-neutral-900">{formatCartMoney(shop.subtotal)}</span>
              </div>
            </section>
          ))}
        </div>
      )}
      <div
        ref={sentinel}
        className="mt-6 min-h-10 text-center text-sm text-neutral-500"
        aria-live="polite"
      >
        {isLoadingMore
          ? "Loading more shops…"
          : hasMore && !error
            ? "Scroll to load more shops"
            : cart?.shops.length
              ? "All shops loaded"
              : null}
        {error && hasMore && (
          <button
            type="button"
            disabled={isLoadingMore || isLoading || isMutating}
            onClick={() => void loadMore()}
            className="ml-3 font-semibold text-emerald-700 underline"
          >
            Retry loading more
          </button>
        )}
      </div>
    </div>
  );
}
