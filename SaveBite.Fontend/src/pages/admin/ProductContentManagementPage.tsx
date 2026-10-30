import { useCallback, useEffect, useState, type ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import {
  AlertCircle,
  ArrowDown,
  ArrowLeft,
  CheckCircle2,
  ChevronRight,
  PackageSearch,
  RotateCw,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { productApi } from "@/features/products/api/productApi";
import { ProductContentManagement } from "@/features/products/components/ProductContentManagement";
import type { ProductContentManagement as ProductContentManagementType } from "@/features/products/types/product.types";
import { ProductModerationActions } from "@/features/products/components/ProductModerationActions";

const actionButtonClass =
  "inline-flex items-center gap-2 rounded-xl border border-border-default bg-white px-4 py-2 text-sm font-medium text-text-primary shadow-sm transition-colors hover:bg-neutral-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500";

const primaryButtonClass =
  "inline-flex items-center gap-2 rounded-xl bg-primary-700 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2";

const cardClass =
  "rounded-3xl border border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)]";

function ContentSkeleton() {
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label="Loading product content"
      className="grid gap-6 motion-safe:animate-pulse lg:grid-cols-[minmax(0,1fr)_320px]"
    >
      <span className="sr-only">Loading product content…</span>

      <div className="space-y-6">
        <div className={`grid gap-8 p-6 md:grid-cols-[300px_minmax(0,1fr)] ${cardClass}`}>
          <div className="aspect-square rounded-2xl bg-neutral-100" />
          <div className="space-y-4">
            <div className="h-6 w-24 rounded-full bg-neutral-100" />
            <div className="h-8 w-3/4 rounded-lg bg-neutral-100" />
            <div className="h-7 w-1/3 rounded-lg bg-neutral-100" />
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="h-14 rounded-2xl bg-neutral-100" />
              <div className="h-14 rounded-2xl bg-neutral-100" />
              <div className="h-14 rounded-2xl bg-neutral-100" />
              <div className="h-14 rounded-2xl bg-neutral-100" />
            </div>
          </div>
        </div>
        <div className={`h-40 ${cardClass}`} />
        <div className={`h-56 ${cardClass}`} />
      </div>

      <div className="space-y-6">
        <div className={`h-28 ${cardClass}`} />
        <div className={`h-52 ${cardClass}`} />
      </div>
    </div>
  );
}

type Tone = "neutral" | "error" | "success";

const toneStyles: Record<Tone, { border: string; icon: string; title: string }> = {
  neutral: {
    border: "border-border-default",
    icon: "bg-neutral-100 text-text-muted",
    title: "text-text-primary",
  },
  error: {
    border: "border-red-200",
    icon: "bg-red-50 text-red-600",
    title: "text-red-700",
  },
  success: {
    border: "border-green-200",
    icon: "bg-green-50 text-green-600",
    title: "text-text-primary",
  },
};

function StateMessage({
  icon: Icon,
  title,
  description,
  tone = "neutral",
  role,
  actions,
}: {
  icon: typeof AlertCircle;
  title: string;
  description?: string;
  tone?: Tone;
  role?: "alert" | "status";
  actions?: ReactNode;
}) {
  const styles = toneStyles[tone];

  return (
    <div
      role={role}
      className={`flex flex-col items-center gap-3 rounded-3xl border bg-white px-6 py-20 text-center ${styles.border}`}
    >
      <span className={`flex size-14 items-center justify-center rounded-2xl ${styles.icon}`}>
        <Icon className="size-7" aria-hidden="true" />
      </span>
      <p className={`text-lg font-semibold tracking-tight ${styles.title}`}>{title}</p>
      {description && <p className="max-w-sm text-sm text-text-secondary">{description}</p>}
      {actions && (
        <div className="mt-3 flex flex-wrap items-center justify-center gap-3">{actions}</div>
      )}
    </div>
  );
}

function BackToListLink({ className = "" }: { className?: string }) {
  return (
    <Link to={APP_PATHS.ADMIN_PRODUCTS} className={className}>
      <ArrowLeft className="size-4" aria-hidden="true" />
      Back to Product Management
    </Link>
  );
}

export function ProductContentManagementPage() {
  const { productId } = useParams<{ productId: string }>();

  const [product, setProduct] = useState<ProductContentManagementType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [removed, setRemoved] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);

  const retry = useCallback(() => setReloadKey((key) => key + 1), []);

  useEffect(() => {
    setRemoved(false);

    if (!productId) {
      setProduct(null);
      setError("Product ID is missing.");
      setLoading(false);
      return;
    }

    const controller = new AbortController();

    setLoading(true);
    setError(null);
    setProduct(null);

    void productApi
      .getProductContentForManagement(productId, controller.signal)
      .then((data) => {
        if (controller.signal.aborted) return;

        setProduct(data ?? null);
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError("Unable to load product content.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [productId, reloadKey]);

  return (
    <div className="space-y-6">
      <header className="space-y-4">
        <nav
          aria-label="Breadcrumb"
          className="flex flex-wrap items-center gap-1.5 text-sm text-text-secondary"
        >
          <Link
            to={APP_PATHS.ADMIN_PRODUCTS}
            className="inline-flex items-center gap-1.5 rounded-md font-medium transition-colors hover:text-primary-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
          >
            <ArrowLeft className="size-4" aria-hidden="true" />
            Product Management
          </Link>
          <ChevronRight className="size-4 text-text-muted" aria-hidden="true" />
          <span aria-current="page" className="font-medium text-text-primary">
            Content management
          </span>
        </nav>

        <div className="flex flex-wrap items-end justify-between gap-4">
          <div className="min-w-0 space-y-1.5">
            <h1 className="truncate text-3xl font-bold tracking-tight text-text-primary">
              {product ? product.name : "Product content management"}
            </h1>
            <p className="text-sm text-text-secondary">
              {product
                ? "Review the product details and content, then decide on moderation at the bottom."
                : "Review registered product details and content."}
            </p>
          </div>

          {product && (
            <button
              type="button"
              onClick={() =>
                document
                  .getElementById("moderation")
                  ?.scrollIntoView({ behavior: "smooth", block: "start" })
              }
              className={primaryButtonClass}
            >
              <ArrowDown className="size-4" aria-hidden="true" />
              Go to moderation
            </button>
          )}
        </div>
      </header>

      {loading ? (
        <ContentSkeleton />
      ) : error ? (
        <StateMessage
          role="alert"
          tone="error"
          icon={AlertCircle}
          title={error}
          description="Check your connection, then try again."
          actions={
            <>
              <button type="button" onClick={retry} className={primaryButtonClass}>
                <RotateCw className="size-4" aria-hidden="true" />
                Try again
              </button>
              <BackToListLink className={actionButtonClass} />
            </>
          }
        />
      ) : removed ? (
        <StateMessage
          role="status"
          tone="success"
          icon={CheckCircle2}
          title="Product removed"
          description="The product is no longer listed. You can return to the list to review another one."
          actions={<BackToListLink className={actionButtonClass} />}
        />
      ) : !product ? (
        <StateMessage
          icon={PackageSearch}
          title="Product not found"
          description="This product may have been removed. Go back to the list and pick another one."
          actions={<BackToListLink className={actionButtonClass} />}
        />
      ) : (
        <>
          <ProductContentManagement product={product} />
          <div id="moderation" className="scroll-mt-6">
            <ProductModerationActions
              productId={product.id}
              latestReview={product.latestReview}
              onReviewed={retry}
              onRemoved={() => {
                setProduct(null);
                setRemoved(true);
              }}
            />
          </div>
        </>
      )}
    </div>
  );
}
