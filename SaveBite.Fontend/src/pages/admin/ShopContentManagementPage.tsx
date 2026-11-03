import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import {
  AlertCircle,
  ArrowLeft,
  Ban,
  CheckCircle2,
  ChevronRight,
  PauseCircle,
  RotateCw,
  SearchX,
  ShieldCheck,
  X,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

import { APP_PATHS } from "@/app/router/paths";
import { shopApi } from "@/features/shops/api/shopApi";
import { ShopContentManagement } from "@/features/shops/components/ShopContentManagement";
import { ShopStatusHistory } from "@/features/shops/components/ShopStatusHistory";
import type { ShopContentManagement as ShopContentModel } from "@/features/shops/types/shopManagement.types";
import type { ShopStatus } from "@/features/shops/types/ownerShop.types";
import { ShopModerationActions } from "@/features/shops/components/ShopModerationActions";

const actionButtonClass =
  "inline-flex items-center gap-2 rounded-lg border border-border-default bg-white px-3.5 py-2 text-sm font-medium text-text-primary shadow-sm transition hover:bg-neutral-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500";

const STATUS_STYLES: Partial<
  Record<ShopStatus, { icon: LucideIcon; dot: string; banner: string }>
> = {
  Active: {
    icon: CheckCircle2,
    dot: "bg-green-500",
    banner: "",
  },
  Paused: {
    icon: PauseCircle,
    dot: "bg-amber-400",
    banner: "border-amber-400 bg-amber-50 text-amber-950",
  },
  Suspended: {
    icon: Ban,
    dot: "bg-red-500",
    banner: "border-red-500 bg-red-50 text-red-950",
  },
};

type Notice = { tone: "success" | "error"; message: string };
type Tab = "review" | "history";

function PageSkeleton() {
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label="Loading shop content"
      className="space-y-6 motion-safe:animate-pulse"
    >
      <span className="sr-only">Loading shop content…</span>

      <div className="flex gap-6 border-b border-border-default pb-3">
        <div className="h-5 w-16 rounded bg-neutral-100" />
        <div className="h-5 w-16 rounded bg-neutral-100" />
      </div>

      <div className="h-56 rounded-2xl bg-neutral-100" />

      <div className="grid gap-4 sm:grid-cols-2">
        {Array.from({ length: 4 }).map((_, index) => (
          <div key={index} className="h-24 rounded-xl bg-neutral-100" />
        ))}
      </div>

      <div className="h-64 rounded-2xl bg-neutral-100" />
    </div>
  );
}

function StateMessage({
  icon: Icon,
  tone,
  role,
  title,
  description,
  actions,
}: {
  icon: LucideIcon;
  tone: "error" | "muted";
  role?: "alert";
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  const isError = tone === "error";

  return (
    <div
      role={role}
      className={`flex flex-col items-center gap-3 rounded-2xl border border-dashed px-6 py-16 text-center ${
        isError ? "border-red-300" : "border-border-default"
      }`}
    >
      <Icon
        className={`size-8 ${isError ? "text-red-500" : "text-text-muted"}`}
        aria-hidden="true"
      />

      <p className={`text-lg font-semibold ${isError ? "text-red-700" : "text-text-primary"}`}>
        {title}
      </p>

      {description && <p className="max-w-sm text-sm text-text-secondary">{description}</p>}

      {actions && (
        <div className="mt-3 flex flex-wrap items-center justify-center gap-3">{actions}</div>
      )}
    </div>
  );
}

function BackToListLink({ className }: { className: string }) {
  return (
    <Link to={APP_PATHS.ADMIN_STORES} className={className}>
      <ArrowLeft className="size-4" aria-hidden="true" />
      Back to shops
    </Link>
  );
}

function NoticeBanner({ notice, onDismiss }: { notice: Notice; onDismiss: () => void }) {
  const isError = notice.tone === "error";
  const Icon = isError ? AlertCircle : CheckCircle2;

  return (
    <div
      role={isError ? "alert" : "status"}
      className={`flex items-start gap-3 rounded-xl border-l-4 px-4 py-3 text-sm ${
        isError
          ? "border-red-500 bg-red-50 text-red-800"
          : "border-green-500 bg-green-50 text-green-800"
      }`}
    >
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden="true" />

      <p className="min-w-0 flex-1">{notice.message}</p>

      <button
        type="button"
        onClick={onDismiss}
        aria-label="Dismiss message"
        className="rounded-md p-0.5 opacity-70 transition-opacity hover:opacity-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-current"
      >
        <X className="size-4" aria-hidden="true" />
      </button>
    </div>
  );
}

export function ShopContentManagementPage() {
  const { shopId } = useParams();

  const [shop, setShop] = useState<ShopContentModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [notice, setNotice] = useState<Notice | null>(null);
  const [tab, setTab] = useState<Tab>("review");

  const drawerRef = useRef<HTMLDialogElement>(null);

  const openModeration = () => {
    drawerRef.current?.showModal();
  };

  const closeModeration = () => {
    drawerRef.current?.close();
  };

  const retry = useCallback(() => {
    setReloadKey((current) => current + 1);
  }, []);

  const handleUpdated = useCallback((id: string, message?: string) => {
    drawerRef.current?.close();
    setNotice({ tone: "success", message: message || "Shop status updated." });

    void shopApi
      .getShopContentForManagement(id)
      .then((data) => {
        setShop((previous) => (previous?.id === id ? (data ?? null) : previous));
      })
      .catch(() => {
        setNotice({
          tone: "error",
          message:
            "The status was updated, but the latest shop data could not be loaded. Reload the page to see the current status.",
        });
      });
  }, []);

  useEffect(() => {
    const controller = new AbortController();

    setLoading(true);
    setError(null);
    setShop(null);
    setNotice(null);
    setTab("review");

    if (!shopId) {
      setError("Missing shop ID.");
      setLoading(false);

      return () => {
        controller.abort();
      };
    }

    void shopApi
      .getShopContentForManagement(shopId, controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) {
          setShop(data ?? null);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError("Unable to load shop content.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [shopId, reloadKey]);

  const statusStyle = STATUS_STYLES[shop?.status as ShopStatus];
  const historyCount = shop?.statusHistory?.length ?? 0;
  const isRestricted = shop?.status === "Paused" || shop?.status === "Suspended";

  const tabs: { id: Tab; label: string; count?: number }[] = [
    { id: "review", label: "Review" },
    { id: "history", label: "History", count: historyCount },
  ];

  return (
    <div className="space-y-6">
      <header className="space-y-4">
        <nav
          aria-label="Breadcrumb"
          className="flex flex-wrap items-center gap-1.5 text-sm text-text-secondary"
        >
          <Link
            to={APP_PATHS.ADMIN_STORES}
            className="rounded-md font-medium transition-colors hover:text-primary-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
          >
            Shops
          </Link>

          <ChevronRight className="size-4 text-text-muted" aria-hidden="true" />

          <span aria-current="page" className="font-medium text-text-primary">
            Content management
          </span>
        </nav>
      </header>

      {loading ? (
        <PageSkeleton />
      ) : error ? (
        <StateMessage
          icon={AlertCircle}
          tone="error"
          role="alert"
          title={error}
          description="Check your connection, then try again."
          actions={
            <>
              <button type="button" onClick={retry} className={actionButtonClass}>
                <RotateCw className="size-4" aria-hidden="true" />
                Try again
              </button>

              <BackToListLink className={actionButtonClass} />
            </>
          }
        />
      ) : shop ? (
        <>
          {notice && <NoticeBanner notice={notice} onDismiss={() => setNotice(null)} />}

          {isRestricted && statusStyle && (
            <div
              className={`rounded-xl border-l-4 px-4 py-3 text-sm ${statusStyle.banner}`}
              role="note"
            >
              <p className="font-semibold">This shop is {String(shop.status).toLowerCase()}</p>
              <p className="mt-0.5 whitespace-pre-wrap break-words opacity-90">
                {shop.currentStatusReason?.trim() || "The status reason is not available."}
              </p>
            </div>
          )}

          {/* Tabs */}
          <div
            role="tablist"
            aria-label="Shop sections"
            className="flex gap-6 border-b border-border-default"
          >
            {tabs.map(({ id, label, count }) => {
              const selected = tab === id;

              return (
                <button
                  key={id}
                  type="button"
                  role="tab"
                  id={`tab-${id}`}
                  aria-selected={selected}
                  aria-controls={`panel-${id}`}
                  onClick={() => setTab(id)}
                  className={`-mb-px inline-flex items-center gap-2 border-b-2 px-0.5 pb-3 pt-1 text-sm font-semibold transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 ${
                    selected
                      ? "border-primary-600 text-text-primary"
                      : "border-transparent text-text-secondary hover:text-text-primary"
                  }`}
                >
                  {label}
                  {count !== undefined && count > 0 && (
                    <span className="rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium tabular-nums text-text-secondary">
                      {count}
                    </span>
                  )}
                </button>
              );
            })}
          </div>

          <div
            role="tabpanel"
            id={`panel-${tab}`}
            aria-labelledby={`tab-${tab}`}
            tabIndex={0}
            className="focus:outline-none"
          >
            {tab === "review" ? (
              <ShopContentManagement shop={shop} />
            ) : (
              <ShopStatusHistory history={shop.statusHistory ?? []} />
            )}
          </div>

          {/* Thanh quyết định nổi ở đáy màn hình */}
          <div className="pointer-events-none fixed inset-x-0 bottom-5 z-30 flex justify-center px-4">
            <div className="pointer-events-auto flex items-center gap-4 rounded-full border border-border-default bg-white/95 py-2 pl-5 pr-2 shadow-lg backdrop-blur">
              <p className="flex items-center gap-2 text-sm">
                <span
                  className={`size-2.5 rounded-full ${statusStyle?.dot ?? "bg-neutral-300"}`}
                  aria-hidden="true"
                />
                <span className="text-text-secondary">Status</span>
                <span className="font-semibold text-text-primary">{String(shop.status)}</span>
              </p>

              <button
                type="button"
                onClick={openModeration}
                className="inline-flex items-center gap-2 rounded-full bg-neutral-900 px-4 py-2 text-sm font-semibold text-white transition hover:bg-neutral-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-neutral-900 focus-visible:ring-offset-2"
              >
                <ShieldCheck className="size-4" aria-hidden="true" />
                Change status
              </button>
            </div>
          </div>

          {/* Ngăn kéo moderation */}
          <dialog
            ref={drawerRef}
            aria-labelledby="moderation-title"
            onClick={(event) => {
              if (event.target === event.currentTarget) {
                closeModeration();
              }
            }}
            className="my-0 mr-0 ml-auto h-dvh max-h-none w-full max-w-md bg-white p-0 text-text-primary shadow-2xl backdrop:bg-neutral-900/40 open:flex open:flex-col"
          >
            <header className="flex items-start justify-between gap-4 border-b border-border-default px-6 py-4">
              <div className="min-w-0">
                <h2 id="moderation-title" className="text-lg font-semibold">
                  Change shop status
                </h2>
                <p className="mt-0.5 truncate text-sm text-text-secondary">{shop.name}</p>
              </div>

              <button
                type="button"
                onClick={closeModeration}
                aria-label="Close"
                className="rounded-lg p-1.5 text-text-secondary transition hover:bg-neutral-100 hover:text-text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
              >
                <X className="size-5" aria-hidden="true" />
              </button>
            </header>

            <ShopModerationActions
              key={`${shop.id}-${shop.status}`}
              shopId={shop.id}
              currentStatus={shop.status}
              currentStatusReason={shop.currentStatusReason}
              onUpdated={(message) => handleUpdated(shop.id, message)}
            />
          </dialog>
        </>
      ) : (
        <StateMessage
          icon={SearchX}
          tone="muted"
          title="Shop not found"
          description="This shop may have been removed. Go back to the list and pick another one."
          actions={<BackToListLink className={actionButtonClass} />}
        />
      )}
    </div>
  );
}
