import { useEffect, useRef } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowRight, ShoppingBag, Store, Package, CircleAlert, CheckCircle2 } from "lucide-react";
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

  async function change(action: () => Promise<void>, successMessage: string) {
    try {
      await action();
      toast.success(successMessage);
    } catch (error) {
      toast.error(getApiErrorMessage(error));
      await refresh();
    }
  }

  const shops = cart?.shops ?? [];

  return (
    <div className="min-w-0 space-y-6 py-6 sm:py-8">
      <div className="flex items-start justify-between gap-4">
        <PageHeader
          title="Your cart"
          description="Checkout with one shop at a time. Items from other shops stay in your cart."
          breadcrumb={
            <ol className="flex min-w-0 flex-wrap items-center gap-2">
              <li>
                <Link
                  to={APP_PATHS.HOME}
                  className="inline-flex min-h-8 items-center gap-1 font-medium hover:text-primary-700"
                >
                  All deals
                </Link>
              </li>
              <li className="flex min-w-0 items-center gap-2">
                <span className="text-text-muted" aria-hidden="true">
                  ›
                </span>
                <span aria-current="page" className="font-medium text-text-primary">
                  Cart
                </span>
              </li>
            </ol>
          }
        />
        <span className="hidden size-12 shrink-0 items-center justify-center rounded-full bg-primary-100 text-primary-700 sm:flex">
          <ShoppingBag className="size-6" aria-hidden="true" />
        </span>
      </div>
      {error && (
        <ErrorState
          title="Couldn't refresh your cart"
          description="We couldn't update your cart view. Please try again."
          onRetry={() => void refresh()}
        />
      )}
      {!cart && !error && (
        <div role="status" aria-label="Loading your cart" className="space-y-4">
          <span className="sr-only">Loading your cart</span>
          {[0, 1].map((index) => (
            <Card key={index} className="space-y-4">
              <div className="flex gap-3">
                <Skeleton className="size-10" />
                <Skeleton className="h-10 w-48" />
              </div>
              <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_17rem]">
                <div className="space-y-4">
                  <Skeleton className="h-32" />
                  <Skeleton className="h-32" />
                </div>
                <Skeleton className="h-48" />
              </div>
            </Card>
          ))}
        </div>
      )}
      {cart?.shops.length === 0 && (
        <EmptyState
          title="Your cart is empty"
          description="Discover a deal and save a meal today."
          icon={ShoppingBag}
          action={
            <ButtonLink to={APP_PATHS.HOME}>
              Explore deals
              <ArrowRight className="size-4" aria-hidden="true" />
            </ButtonLink>
          }
        />
      )}
      {shops.length > 0 && (
        <div className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p role="status" className="text-body text-text-secondary">
              {isLoading
                ? "Refreshing your cart…"
                : `${shops.length} ${shops.length === 1 ? "shop" : "shops"} loaded`}
            </p>
            <Badge tone="info">
              <Package className="size-3" aria-hidden="true" />
              In-store pickup
            </Badge>
          </div>
          {shops.map((shop) => (
            <Card
              key={shop.shopId}
              aria-label={`Cart from ${shop.shopName}`}
              className="overflow-hidden !p-0"
            >
              <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border-default bg-primary-50/50 p-4 sm:px-5">
                <Link
                  to={`/shops/${shop.shopId}`}
                  aria-label={`Visit ${shop.shopName}`}
                  className="group flex min-w-0 flex-1 items-center gap-3"
                >
                  <MediaImage
                    src={shop.shopLogoUrl ?? undefined}
                    alt=""
                    fallbackIcon={Store}
                    showFallbackLabel={false}
                    className="size-10 shrink-0 rounded-md"
                  />
                  <div className="min-w-0">
                    <h2
                      className="line-clamp-2 text-section font-semibold text-text-primary group-hover:text-primary-700 [overflow-wrap:anywhere]"
                      title={shop.shopName}
                    >
                      {shop.shopName}
                    </h2>
                    <span
                      className="mt-1 block truncate text-caption text-text-secondary"
                      title={shop.shopAddress}
                    >
                      {shop.shopAddress || "Pickup address not provided"}
                    </span>
                  </div>
                </Link>
                <div className="flex items-center gap-2">
                  <Link
                    to={`/shops/${shop.shopId}`}
                    className="inline-flex items-center gap-1.5 rounded-lg border border-primary-200 bg-white px-3 py-1.5 text-xs font-semibold text-primary-700 shadow-sm transition hover:bg-primary-50"
                  >
                    <Store className="size-3.5" aria-hidden="true" />
                    <span>Visit Store</span>
                  </Link>
                  <Badge tone="primary">
                    {shop.totalQuantity} {shop.totalQuantity === 1 ? "item" : "items"}
                  </Badge>
                </div>
              </header>
              <div className="grid lg:grid-cols-[minmax(0,1fr)_17rem]">
                <ul className="min-w-0 px-4 sm:px-5" aria-label={`Items from ${shop.shopName}`}>
                  {shop.items.map((item) => (
                    <CartItemRow
                      key={item.id}
                      item={item}
                      compact
                      busy={isMutating || isLoading}
                      onQuantity={(quantity) =>
                        void change(() => updateQuantity(item.id, quantity), "Quantity updated.")
                      }
                      onRemove={() =>
                        void change(() => removeItem(item.id), "Item removed from your cart.")
                      }
                    />
                  ))}
                </ul>
                <aside
                  aria-label={`Checkout summary for ${shop.shopName}`}
                  className="space-y-4 border-t border-border-default bg-neutral-50/60 p-4 sm:p-5 lg:border-l lg:border-t-0"
                >
                  <p className="flex items-center gap-2 text-body font-semibold text-text-primary">
                    <Package className="size-4 text-secondary-700" aria-hidden="true" />
                    This shop's checkout
                  </p>
                  <dl className="space-y-3 text-body">
                    <div className="flex justify-between gap-3">
                      <dt className="text-text-secondary">Items</dt>
                      <dd className="font-medium tabular-nums text-text-primary">
                        {shop.totalQuantity}
                      </dd>
                    </div>
                    <div className="flex flex-wrap items-baseline justify-between gap-2 border-t border-border-default pt-3">
                      <dt className="text-text-secondary">Shop subtotal</dt>
                      <dd className="text-section font-bold tabular-nums text-primary-700">
                        {formatCartMoney(shop.subtotal)}
                      </dd>
                    </div>
                  </dl>
                  {!shop.canCheckout && (
                    <p
                      role="status"
                      className="flex items-start gap-2 rounded-md border border-accent-100 bg-accent-50 p-3 text-body text-accent-700"
                    >
                      <CircleAlert className="mt-1 size-4 shrink-0" aria-hidden="true" />
                      Review unavailable items or quantities before checking out with this shop.
                    </p>
                  )}
                  <Button
                    size="lg"
                    className="w-full"
                    disabled={!shop.canCheckout || isMutating || isLoading || Boolean(error)}
                    onClick={() =>
                      navigate(`${APP_PATHS.CHECKOUT}?shopId=${encodeURIComponent(shop.shopId)}`)
                    }
                    aria-label={`Checkout with ${shop.shopName}`}
                  >
                    Checkout
                    <ArrowRight className="size-4 shrink-0" aria-hidden="true" />
                  </Button>
                </aside>
              </div>
            </Card>
          ))}
        </div>
      )}
      <div ref={sentinel} className="flex min-h-12 justify-center py-2" aria-live="polite">
        {hasMore ? (
          <Button
            variant="secondary"
            disabled={isLoading || isMutating}
            loading={isLoadingMore}
            onClick={() => void loadMore()}
          >
            {error
              ? "Retry loading more"
              : isLoadingMore
                ? "Loading more shops…"
                : "Load more shops"}
          </Button>
        ) : (
          shops.length > 0 &&
          !error && (
            <p className="flex items-center gap-2 text-caption text-text-secondary">
              <CheckCircle2 className="size-4 text-primary-700" aria-hidden="true" />
              All shops loaded
            </p>
          )
        )}
      </div>
    </div>
  );
}
