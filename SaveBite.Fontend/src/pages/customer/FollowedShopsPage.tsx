import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { MapPin, Store } from "lucide-react";
import {
  shopFollowApi,
  type FollowedShopsPage as Page,
} from "@/features/shop-follows/api/shopFollowApi";
import { ShopFollowControls } from "@/features/shop-follows/components/ShopFollowControls";
import {
  ButtonLink,
  Card,
  EmptyState,
  ErrorState,
  MediaImage,
  Skeleton,
} from "@/shared/components/ui";
import { AccountPagination } from "@/shared/components/pagination/AccountPagination";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

export function FollowedShopsPage() {
  const userId = useAuthStore((state) => state.user?.id);
  return <FollowedShopsContent key={userId} />;
}
function FollowedShopsContent() {
  const [page, setPage] = useState(1);
  const [attempt, setAttempt] = useState(0);
  const [data, setData] = useState<Page | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    const controller = new AbortController();
    shopFollowApi
      .list(page, 10, controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return;
        if (page > 1 && result.items.length === 0) {
          setPage(Math.max(1, result.totalPages));
          return;
        }
        setData(result);
        setError(null);
        setLoading(false);
      })
      .catch((cause: unknown) => {
        if (!controller.signal.aborted) {
          setError(getApiErrorMessage(cause));
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [page, attempt]);
  function refresh() {
    setLoading(true);
    setAttempt((value) => value + 1);
  }
  if (loading)
    return (
      <div role="status" aria-label="Loading followed stores" className="space-y-4">
        {[1, 2, 3].map((item) => (
          <Skeleton key={item} className="h-40 w-full rounded-lg" />
        ))}
      </div>
    );
  if (error)
    return (
      <ErrorState title="Couldn't load followed stores" description={error} onRetry={refresh} />
    );
  if (!data?.items.length)
    return (
      <EmptyState
        icon={Store}
        title="No followed stores yet"
        description="Follow a store to keep it here. Turn on its bell to hear about new deals."
        action={<ButtonLink to="/">Explore nearby deals</ButtonLink>}
      />
    );
  return (
    <div className="space-y-4">
      <p className="text-body text-text-secondary">
        {data.totalItems} followed {data.totalItems === 1 ? "store" : "stores"} · Use the bell to
        manage new deal notifications.
      </p>
      <ul className="space-y-4">
        {data.items.map((shop) => (
          <li key={shop.shopId}>
            <Card className="grid items-center gap-4 !p-4 lg:grid-cols-[minmax(0,1fr)_220px]">
              <div className="flex min-w-0 items-center gap-3">
                <MediaImage
                  src={shop.logoUrl ?? undefined}
                  alt=""
                  fallbackIcon={Store}
                  showFallbackLabel={false}
                  className="size-14 shrink-0 rounded-md"
                />
                <div className="min-w-0 space-y-2">
                  <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                    <Link
                      to={`/shops/${shop.shopId}`}
                      className="rounded-sm text-primary-700 underline-offset-4 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
                    >
                      {shop.shopName}
                    </Link>
                  </h2>
                  <p className="flex items-start gap-1 text-body text-text-secondary">
                    <MapPin className="mt-1 size-4 shrink-0" aria-hidden="true" />
                    <span className="[overflow-wrap:anywhere]">
                      {shop.addressLine || "Address not provided"}
                    </span>
                  </p>
                  {shop.shopStatus !== "Active" && (
                    <p className="text-caption text-text-secondary">
                      This store is currently unavailable.
                    </p>
                  )}
                </div>
              </div>
              <div className="min-w-0 border-t border-border-default pt-4 lg:border-l lg:border-t-0 lg:pl-5 lg:pt-0">
                <ShopFollowControls
                  compact
                  shopId={shop.shopId}
                  shopName={shop.shopName}
                  initialFollow={shop}
                  onChange={(next) => {
                    if (!next) refresh();
                    else
                      setData((current) =>
                        current
                          ? {
                              ...current,
                              items: current.items.map((item) =>
                                item.shopId === next.shopId ? next : item,
                              ),
                            }
                          : current,
                      );
                  }}
                />
              </div>
            </Card>
          </li>
        ))}
      </ul>
      <AccountPagination
        currentPage={page}
        totalPages={data.totalPages}
        totalItems={data.totalItems}
        pageSize={10}
        itemLabel="stores"
        onPageChange={(next) => {
          setLoading(true);
          setPage(next);
        }}
      />
    </div>
  );
}
