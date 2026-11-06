import { useFlashDealRealtime } from "@/features/flash-deals/hooks/useFlashDealRealtime";
import { subscribeFeedbackSync } from "@/features/feedbacks/utils/feedbackSync";
import { ShopFollowControls } from "@/features/shop-follows/components/ShopFollowControls";
import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { isAxiosError } from "axios";
import {
  ArrowLeft,
  ChevronDown,
  ChevronUp,
  Clock3,
  Flame,
  MapPin,
  Package,
  RefreshCw,
  Star,
  Store,
} from "lucide-react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import { shopApi } from "@/features/shops/api/shopApi";
import { DealGrid } from "@/features/flash-deals/components/DealGrid";
import { StoreReviewList } from "@/features/shops/components/StoreReviewList";
import { usePagination } from "@/shared/components/pagination";
import {
  Badge,
  Button,
  ButtonLink,
  Card,
  EmptyState,
  ErrorState,
  MediaImage,
  PageHeader,
  Pagination,
  Skeleton,
  Tabs,
} from "@/shared/components/ui";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";
import type { ShopProfile, StoreReviewsSummary } from "@/features/shops/types/shop.types";

function StoreUnavailable() {
  return (
    <div className="space-y-6 py-8">
      <PageHeader title="Store details" />
      <EmptyState
        title="Store not found"
        description="This store may be unavailable or no longer listed. Find another bite nearby."
        icon={Store}
        action={
          <ButtonLink to="/">
            <ArrowLeft className="size-4" aria-hidden="true" />
            Browse nearby deals
          </ButtonLink>
        }
      />
    </div>
  );
}

export function CustomerShopPage() {
  const { id } = useParams<{ id: string }>();
  return id ? (
    <ShopContent key={id} shopId={id} />
  ) : (
    <StoreUnavailable />
  );
}

function ShopContent({ shopId }: { shopId: string }) {
  const [profile, setProfile] = useState<ShopProfile | null>(null);
  const [isProfileLoading, setIsProfileLoading] = useState(true);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [profileAttempt, setProfileAttempt] = useState(0);
  const [deals, setDeals] = useState<FlashDeal[]>([]);
  const [isDealsLoading, setIsDealsLoading] = useState(true);
  const [dealsError, setDealsError] = useState<string | null>(null);
  const [dealsAttempt, setDealsAttempt] = useState(0);
  const [reviewsSummary, setReviewsSummary] = useState<StoreReviewsSummary | null>(null);
  const [isReviewsLoading, setIsReviewsLoading] = useState(true);
  const [reviewsError, setReviewsError] = useState<string | null>(null);
  const [reviewsAttempt, setReviewsAttempt] = useState(0);
  const [reviewRatingFilter, setReviewRatingFilter] = useState<number | null>(null);
  const [reviewHasImagesOnly, setReviewHasImagesOnly] = useState(false);
  const [reviewPage, setReviewPage] = useState(1);
  const [activeTab, setActiveTab] = useState("deals");
  const [isStoreInfoExpanded, setIsStoreInfoExpanded] = useState(false);
  const dealRealtimeStatus = useFlashDealRealtime("shop", shopId, () => {
    setIsDealsLoading(true);
    setDealsAttempt((value) => value + 1);
    setProfileAttempt((value) => value + 1);
    setReviewsAttempt((value) => value + 1);
  });

  // Real-time synchronization when admin hides feedback: update reviews and star rating immediately
  useEffect(() => {
    return subscribeFeedbackSync((detail) => {
      if (!detail?.shopId || detail.shopId.toLowerCase() === shopId.toLowerCase()) {
        if (detail?.action === "hide" && detail.feedbackId) {
          setReviewsSummary((prev) => {
            if (!prev) return prev;
            const target = prev.reviews.items.find((r) => r.id === detail.feedbackId);
            const rating = detail.rating ?? target?.rating;
            const updatedItems = prev.reviews.items.filter((r) => r.id !== detail.feedbackId);
            const newTotal = Math.max(0, prev.totalReviews - 1);
            const breakdown = { ...prev.ratingBreakdown };
            if (rating && breakdown[rating]) {
              breakdown[rating] = Math.max(0, breakdown[rating] - 1);
            }
            let sumRating = 0;
            let sumCount = 0;
            for (let s = 1; s <= 5; s++) {
              const c = breakdown[s] ?? 0;
              sumRating += s * c;
              sumCount += c;
            }
            const newAvg = sumCount > 0 ? sumRating / sumCount : 0;
            setProfile((prevProf) => {
              if (!prevProf) return prevProf;
              return {
                ...prevProf,
                averageRating: newAvg,
                totalReviews: newTotal,
              };
            });
            return {
              ...prev,
              averageRating: newAvg,
              totalReviews: newTotal,
              ratingBreakdown: breakdown,
              reviews: {
                ...prev.reviews,
                items: updatedItems,
                totalItems: Math.max(0, prev.reviews.totalItems - 1),
              },
            };
          });
        }
        setReviewsAttempt((v) => v + 1);
        setProfileAttempt((v) => v + 1);
      }
    });
  }, [shopId]);

  // Real-time synchronization when admin blocks/locks this store
  useEffect(() => {
    const handleShopStatus = (e: Event) => {
      const detail = (e as CustomEvent<{ shopId?: string; status?: string; isBlocked?: boolean }>).detail;
      const rawShopId = detail?.shopId || (detail as any)?.ShopId;
      const rawStatus = detail?.status || (detail as any)?.Status;
      const isBlocked = detail?.isBlocked || (detail as any)?.IsBlocked;
      const targetShopId = rawShopId?.toLowerCase();
      if (targetShopId && targetShopId === shopId.toLowerCase()) {
        const isInactive = !rawStatus || rawStatus.toLowerCase() !== "active" || Boolean(isBlocked);
        if (isInactive) {
          setProfile(null);
          setDeals([]);
          setIsProfileLoading(false);
          setIsDealsLoading(false);
        } else {
          setProfileAttempt((v) => v + 1);
          setDealsAttempt((v) => v + 1);
        }
      }
    };
    window.addEventListener("shop:status-changed", handleShopStatus);
    return () => window.removeEventListener("shop:status-changed", handleShopStatus);
  }, [shopId]);

  useEffect(() => {
    let active = true;
    shopApi
      .getShopProfile(shopId)
      .then((data) => {
        if (active) {
          setProfile(data ?? null);
          setProfileError(null);
        }
      })
      .catch((error: unknown) => {
        if (active) {
          if (isAxiosError(error) && error.response?.status === 404) setProfile(null);
          else setProfileError("We couldn't load this store. Please try again in a moment.");
        }
      })
      .finally(() => {
        if (active) setIsProfileLoading(false);
      });
    return () => {
      active = false;
    };
  }, [shopId, profileAttempt]);

  useEffect(() => {
    let active = true;
    flashDealApi
      .getDealsByShop(shopId)
      .then((data) => {
        if (active) {
          setDeals(data);
          setDealsError(null);
        }
      })
      .catch(() => {
        if (active) setDealsError("Please try loading this store's flash deals again.");
      })
      .finally(() => {
        if (active) setIsDealsLoading(false);
      });
    return () => {
      active = false;
    };
  }, [shopId, dealsAttempt]);

  useEffect(() => {
    let active = true;
    shopApi
      .getStoreReviews(shopId, {
        rating: reviewRatingFilter ?? undefined,
        page: reviewPage,
        pageSize: 10,
      })
      .then((data) => {
        if (active) {
          setReviewsSummary(data);
          setReviewsError(null);
        }
      })
      .catch(() => {
        if (active) setReviewsError("Please try loading customer reviews again.");
      })
      .finally(() => {
        if (active) setIsReviewsLoading(false);
      });
    return () => {
      active = false;
    };
  }, [shopId, reviewRatingFilter, reviewPage, reviewsAttempt]);

  const {
    paginatedItems: paginatedDeals,
    currentPage: dealsPage,
    setCurrentPage: setDealsPage,
    totalPages: dealsTotalPages,
    pageSize: dealsPageSize,
    totalItems: dealsTotalItems,
  } = usePagination({ items: deals, initialPageSize: 8 });
  const retryDeals = () => {
    setIsDealsLoading(true);
    setDealsError(null);
    setDealsAttempt((value) => value + 1);
  };
  const retryReviews = () => {
    setIsReviewsLoading(true);
    setReviewsError(null);
    setReviewsAttempt((value) => value + 1);
  };
  const retryProfile = () => {
    setIsProfileLoading(true);
    setProfileError(null);
    setProfileAttempt((value) => value + 1);
    retryDeals();
    retryReviews();
  };
  const changeRating = (rating: number | null) => {
    if (rating === reviewRatingFilter) return;
    setIsReviewsLoading(true);
    setReviewsError(null);
    setReviewRatingFilter(rating);
    setReviewPage(1);
  };
  const changeReviewPage = (page: number) => {
    if (page === reviewPage) return;
    setIsReviewsLoading(true);
    setReviewsError(null);
    setReviewPage(page);
  };

  if (isProfileLoading)
    return (
      <div role="status" aria-label="Loading store" className="space-y-6 py-6 sm:py-8">
        <Skeleton className="h-5 w-40" />
        <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface">
          <Skeleton className="h-40 rounded-none sm:h-48" />
          <div className="space-y-4 p-6">
            <Skeleton className="h-8 w-2/3" />
            <Skeleton className="h-5 w-1/2" />
            <Skeleton className="h-12" />
          </div>
        </div>
        <DealGrid deals={[]} desktopColumns={4} isLoading />
      </div>
    );
  if (profileError)
    return (
      <div className="space-y-6 py-8">
        <PageHeader title="Store details" />
        <ErrorState title="Couldn't load store" description={profileError} onRetry={retryProfile} />
        <ButtonLink to="/" variant="secondary">
          Back to deals
        </ButtonLink>
      </div>
    );
  if (!profile) return <StoreUnavailable />;

  const shopName = profile.name || "Store details";
  const shopAddress = [profile.addressLine, profile.ward, profile.district, profile.city]
    .filter((part): part is string => Boolean(part?.trim()))
    .reduce(
      (address, part) =>
        address.toLocaleLowerCase("vi").includes(part.toLocaleLowerCase("vi"))
          ? address
          : address
            ? `${address}, ${part}`
            : part,
      "",
    );
  const operatingHours =
    profile.openingTime && profile.closingTime
      ? (profile.openingTime.slice(0, 5) === profile.closingTime.slice(0, 5)
          ? "00:00 – 00:00 (Open 24/7)"
          : `${profile.openingTime.slice(0, 5)} – ${profile.closingTime.slice(0, 5)}`)
      : "Opening hours not provided";
  const averageRating = reviewsSummary?.averageRating ?? profile.averageRating;
  const totalReviewsCount = reviewsSummary?.totalReviews ?? profile.totalReviews;

  return (
    <div className="space-y-6 py-6 sm:py-8">
      <nav
        aria-label="Breadcrumb"
        className="flex min-w-0 items-center gap-2 text-caption text-text-secondary"
      >
        <Link
          to="/"
          className="inline-flex min-h-8 shrink-0 items-center gap-1 font-medium hover:text-primary-700"
        >
          All deals
        </Link>
        <span aria-hidden="true">/</span>
        <span
          aria-current="page"
          title={shopName}
          className="min-w-0 truncate font-medium text-text-primary"
        >
          {shopName}
        </span>
      </nav>

      <Card aria-label="Store profile" className="overflow-hidden !p-0">
        <div className="relative">
          <MediaImage
            src={profile.coverImageUrl ?? undefined}
            alt={`${shopName} cover`}
            loading="eager"
            fallbackIcon={Store}
            className="h-40 w-full sm:h-48 lg:h-52"
          />
          <Badge
            tone={profile.isOpen ? "primary" : "neutral"}
            className="absolute right-4 top-4 border border-bg-surface bg-bg-surface shadow-sm"
          >
            <span
              className={`size-2 rounded-full ${profile.isOpen ? "bg-primary-500" : "bg-neutral-400"}`}
              aria-hidden="true"
            />
            {profile.isOpen ? "Open now" : "Closed"}
          </Badge>
        </div>
        <div className="space-y-5 p-4 sm:p-6">
          <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
            <div className="flex min-w-0 items-center gap-3 sm:gap-4">
              <MediaImage
                src={profile.logoUrl ?? undefined}
                alt={`${shopName} logo`}
                fallbackIcon={Store}
                showFallbackLabel={false}
                className="size-16 shrink-0 rounded-lg border border-border-default sm:size-20"
              />
              <div className="min-w-0 space-y-2">
                <h1 className="text-page font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {shopName}
                </h1>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                  <a
                    href="#store-content"
                    onClick={() => setActiveTab("reviews")}
                    className="inline-flex min-h-9 items-center gap-1.5 rounded-sm text-caption text-text-secondary transition-colors hover:text-primary-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
                  >
                    <Star
                      className="size-4 shrink-0 fill-accent-700 text-accent-700"
                      aria-hidden="true"
                    />
                    <strong className="tabular-nums text-text-primary">
                      {totalReviewsCount > 0 ? averageRating.toFixed(1) : "—"}
                    </strong>
                    <span>({totalReviewsCount} reviews)</span>
                  </a>
                  <Badge tone={isDealsLoading ? "neutral" : "warning"}>
                    {isDealsLoading ? (
                      "Loading deals…"
                    ) : (
                      <>
                        <Flame className="size-3" aria-hidden="true" />
                        {deals.length} flash deals
                      </>
                    )}
                  </Badge>
                </div>
              </div>
            </div>
            <div className="flex w-full flex-col gap-3 sm:w-64 sm:shrink-0">
              <p className="text-sm text-text-secondary">{profile.followerCount ?? 0} followers</p>
              <ShopFollowControls
                shopId={shopId}
                shopName={shopName}
                ownerUserId={profile.ownerUserId}
                initialFollow={profile.currentUserFollow ?? null}
                onChange={() => setProfileAttempt((value) => value + 1)}
              />
            </div>
          </div>
          <div className="grid gap-3 border-t border-border-default pt-4 text-body text-text-secondary md:grid-cols-[2fr_1fr_1fr]">
            <p className="flex min-w-0 items-start gap-2">
              <MapPin className="mt-1 size-4 shrink-0 text-secondary-700" aria-hidden="true" />
              <span className="[overflow-wrap:anywhere]">
                {shopAddress || "Pickup address not provided"}
              </span>
            </p>
            <p className="flex items-start gap-2">
              <Clock3 className="mt-1 size-4 shrink-0 text-primary-700" aria-hidden="true" />
              <span>{operatingHours}</span>
            </p>
            <p className="flex items-start gap-2">
              <Package className="mt-1 size-4 shrink-0 text-primary-700" aria-hidden="true" />
              <span>Pickup at this store</span>
            </p>
          </div>
          <div className="flex justify-center">
            <Button
              variant="ghost"
              className="min-h-11 hover:!bg-transparent hover:text-primary-700"
              aria-expanded={isStoreInfoExpanded}
              aria-controls="store-info"
              onClick={() => setIsStoreInfoExpanded((value) => !value)}
            >
              {isStoreInfoExpanded ? "Collapse info" : "More store info"}
              {isStoreInfoExpanded ? (
                <ChevronUp className="size-4" aria-hidden="true" />
              ) : (
                <ChevronDown className="size-4" aria-hidden="true" />
              )}
            </Button>
          </div>
          {isStoreInfoExpanded && (
            <section
              id="store-info"
              aria-labelledby="store-info-heading"
              className="space-y-3 rounded-md border border-secondary-100 bg-secondary-50 p-4"
            >
              <h2 id="store-info-heading" className="text-section font-semibold text-text-primary">
                About this store
              </h2>
              <p className="whitespace-pre-line text-body leading-relaxed text-text-secondary [overflow-wrap:anywhere]">
                {profile.description || "The store hasn't shared a description yet."}
              </p>
              <dl className="grid gap-3 border-t border-secondary-100 pt-3 text-body sm:grid-cols-2">
                <div>
                  <dt className="font-medium text-secondary-700">Store address</dt>
                  <dd className="mt-1 text-text-secondary [overflow-wrap:anywhere]">
                    {shopAddress || "Pickup address not provided"}
                  </dd>
                </div>
                <div>
                  <dt className="font-medium text-secondary-700">Opening hours</dt>
                  <dd className="mt-1 text-text-secondary">{operatingHours}</dd>
                </div>
              </dl>
            </section>
          )}
        </div>
      </Card>

      {dealRealtimeStatus === "disconnected" && (
        <p
          role="status"
          className="rounded-md border border-accent-100 bg-accent-50 p-3 text-body text-accent-700"
        >
          Live updates are disconnected. Reconnecting… Use Refresh to load the latest deals.
        </p>
      )}
      <div id="store-content" className="scroll-mt-24">
        <Tabs
          label="Store sections"
          value={activeTab}
          onChange={setActiveTab}
          items={[
            { value: "deals", label: `Flash deals (${isDealsLoading ? "…" : deals.length})` },
            { value: "reviews", label: `Reviews (${totalReviewsCount})` },
          ]}
        >
          {activeTab === "deals" ? (
            <section aria-labelledby="store-deals-heading" className="space-y-5">
              <header className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2
                    id="store-deals-heading"
                    className="flex items-center gap-2 text-section font-semibold text-text-primary"
                  >
                    <span className="flex size-9 items-center justify-center rounded-md bg-accent-100 text-accent-700">
                      <Flame className="size-5" aria-hidden="true" />
                    </span>
                    Flash deals from this store
                  </h2>
                  <p className="mt-1 text-body text-text-secondary">
                    Choose a deal to see its options and pickup details.
                  </p>
                </div>
                <Button
                  variant="secondary"
                  className="min-h-11"
                  aria-label="Refresh store deals"
                  loading={isDealsLoading}
                  onClick={retryDeals}
                >
                  {!isDealsLoading && <RefreshCw className="size-4" aria-hidden="true" />}Refresh
                </Button>
              </header>
              {dealsError ? (
                <ErrorState
                  title="Couldn't load store deals"
                  description={dealsError}
                  onRetry={retryDeals}
                />
              ) : (
                <>
                  {!isDealsLoading && deals.length === 0 ? (
                    <EmptyState
                      title="No flash deals at this store"
                      description="Check back later for new offers, or find another bite nearby."
                      icon={Store}
                      action={<ButtonLink to="/">Explore nearby deals</ButtonLink>}
                    />
                  ) : (
                    <DealGrid
                      deals={paginatedDeals}
                      desktopColumns={4}
                      isSameShop
                      isLoading={isDealsLoading}
                    />
                  )}
                  {!isDealsLoading && dealsTotalPages > 1 && (
                    <Pagination
                      currentPage={dealsPage}
                      totalPages={dealsTotalPages}
                      onPageChange={setDealsPage}
                      totalItems={dealsTotalItems}
                      pageSize={dealsPageSize}
                      itemLabel="deals"
                    />
                  )}
                </>
              )}
            </section>
          ) : (
            <Card aria-labelledby="store-reviews-heading" className="space-y-5">
              <header className="flex flex-wrap items-center justify-between gap-3">
                <h2
                  id="store-reviews-heading"
                  className="text-section font-semibold text-text-primary"
                >
                  Customer reviews
                </h2>
              </header>
              {reviewsError ? (
                <ErrorState
                  title="Couldn't load customer reviews"
                  description={reviewsError}
                  onRetry={retryReviews}
                />
              ) : (
                <StoreReviewList
                  summary={reviewsSummary}
                  isLoading={isReviewsLoading}
                  activeRatingFilter={reviewRatingFilter}
                  onRatingFilterChange={changeRating}
                  hasImagesOnly={reviewHasImagesOnly}
                  onHasImagesFilterChange={setReviewHasImagesOnly}
                  currentPage={reviewPage}
                  onPageChange={changeReviewPage}
                />
              )}
            </Card>
          )}
        </Tabs>
      </div>
    </div>
  );
}
