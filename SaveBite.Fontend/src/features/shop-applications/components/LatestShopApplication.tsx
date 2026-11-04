import { useEffect, useState, type ReactNode } from "react";
import {
  CalendarDays,
  Check,
  Clock3,
  FileText,
  Hourglass,
  Landmark,
  MapPin,
  Pencil,
  Store,
  XCircle,
} from "lucide-react";
import {
  Badge,
  Button,
  Card,
  EmptyState,
  ErrorState,
  MediaImage,
  Modal,
  Skeleton,
} from "@/shared/components/ui";
import { getApiErrorMessage } from "@/shared/api";
import { useNavigate } from "react-router-dom";

import { shopApplicationApi } from "@/features/shop-applications/api/shopApplicationApi";
import type {
  ShopApplication,
  ShopApplicationStatus,
} from "@/features/shop-applications/types/shopApplication.types";

/* Status config */
type StatusTone = "amber" | "green" | "red" | "gray";

const STATUS_META: Record<
  ShopApplicationStatus,
  {
    label: string;
    tone: StatusTone;
    badge: string;
    dot: string;
    notice: string;
    noticeTitle: string;
    noticeBody: string;
  }
> = {
  Pending: {
    label: "Under review",
    tone: "amber",
    badge: "bg-accent-50 text-accent-700 ring-accent-100",
    dot: "bg-accent-500",
    notice: "border-accent-100 bg-accent-50 text-accent-700",
    noticeTitle: "We're reviewing your application",
    noticeBody:
      "Your application cannot be edited while it is under review. You can cancel it if needed.",
  },
  NeedsRevision: {
    label: "Needs revision",
    tone: "amber",
    badge: "bg-accent-50 text-accent-700 ring-accent-100",
    dot: "bg-accent-500",
    notice: "border-accent-100 bg-accent-50 text-accent-700",
    noticeTitle: "Please update your application",
    noticeBody: "Review the administrator's note, update your details and submit again.",
  },
  Approved: {
    label: "Approved",
    tone: "green",
    badge: "bg-primary-50 text-primary-800 ring-primary-100",
    dot: "bg-primary-500",
    notice: "border-primary-100 bg-primary-50 text-primary-800",
    noticeTitle: "Your shop is approved",
    noticeBody: "You're now a SaveBite Store Owner and can start managing your shop.",
  },
  Rejected: {
    label: "Rejected",
    tone: "red",
    badge: "bg-danger-soft text-danger-strong ring-danger-soft",
    dot: "bg-danger-soft0",
    notice: "border-danger-soft bg-danger-soft text-danger-strong",
    noticeTitle: "Your application wasn't approved",
    noticeBody: "You can update your application details and submit it again.",
  },
  Cancelled: {
    label: "Cancelled",
    tone: "gray",
    badge: "bg-neutral-100 text-text-secondary ring-border-default",
    dot: "bg-neutral-400",
    notice: "border-border-default bg-neutral-50 text-text-secondary",
    noticeTitle: "This application was Cancelled",
    noticeBody: "You can edit this application and submit it again.",
  },
};

function getStatusMeta(status: ShopApplicationStatus) {
  return STATUS_META[status];
}

/* Helpers */
function formatAddress(application: ShopApplication) {
  return [application.addressLine, application.ward, application.district, application.city]
    .filter(Boolean)
    .join(", ");
}

function formatTime(value?: string | null) {
  if (!value) return "—";

  return value.length >= 5 ? value.slice(0, 5) : value;
}

function maskAccountNumber(value?: string | null) {
  if (!value) return "—";

  if (value.length <= 4) {
    return value;
  }

  return `•••• ${value.slice(-4)}`;
}

function formatDate(value?: string | null) {
  if (!value) return "—";

  return new Date(value).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

/* Small UI pieces */
function InfoRow({
  icon,
  label,
  children,
}: {
  icon: ReactNode;
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="flex items-start gap-3 py-3">
      <div className="flex size-7 shrink-0 items-center justify-center rounded-md bg-primary-50 text-primary-700">
        {icon}
      </div>

      <div className="min-w-0 flex-1">
        <dt className="text-caption text-text-secondary">{label}</dt>

        <dd className="mt-0.5 break-words text-sm font-medium text-text-primary">{children}</dd>
      </div>
    </div>
  );
}

/* Review progress */
function ReviewProgress({ status }: { status: ShopApplicationStatus }) {
  const decided = status === "Approved" || status === "Rejected";
  const steps = [
    { label: "Submitted", state: "done" as const },
    {
      label: "Under review",
      state: status === "Pending" ? ("active" as const) : ("done" as const),
    },
    {
      label: status === "Approved" ? "Approved" : status === "Rejected" ? "Rejected" : "Decision",
      state: decided
        ? status === "Rejected"
          ? ("error" as const)
          : ("done" as const)
        : ("todo" as const),
    },
  ];
  const circle = {
    done: "bg-primary-700 text-text-inverse",
    active: "bg-bg-surface text-accent-700 ring-2 ring-accent-300",
    error: "bg-danger-soft0 text-text-inverse",
    todo: "bg-bg-surface text-text-muted ring-2 ring-border-default",
  };

  return (
    <ol className="grid grid-cols-3" aria-label="Application progress">
      {steps.map((step, index) => (
        <li key={step.label} className="relative flex min-w-0 flex-col items-center gap-1.5 text-center">
          {index < steps.length - 1 && (
            <span
              aria-hidden="true"
              className={`absolute left-[calc(50%+1rem)] top-3.5 h-0.5 w-[calc(100%-2rem)] ${step.state === "done" ? "bg-primary-300" : "bg-neutral-300"}`}
            />
          )}
          <span
            className={`flex size-7 items-center justify-center rounded-full ${circle[step.state]}`}
            aria-hidden="true"
          >
            {step.state === "done" ? (
              <Check size={15} />
            ) : step.state === "active" ? (
              <Hourglass size={15} />
            ) : step.state === "error" ? (
              <XCircle size={15} />
            ) : (
              <span className="size-1.5 rounded-full bg-neutral-300" />
            )}
          </span>
          <span className="px-1 text-caption text-text-secondary">{step.label}</span>
        </li>
      ))}
    </ol>
  );
}

/* Loading skeleton */
function LoadingSkeleton() {
  return (
    <Card padding="compact" className="space-y-3">
      <Skeleton className="h-14 w-2/3" />
      <Skeleton className="h-20" />
      <Skeleton className="h-24" />
    </Card>
  );
}

/* Page */
export default function LatestShopApplication({
  onApplicationChanged,
}: {
  onApplicationChanged?: () => void;
}) {
  const navigate = useNavigate();

  const [application, setApplication] = useState<ShopApplication | null>(null);

  const [loading, setLoading] = useState(true);

  const [loadError, setLoadError] = useState<string | null>(null);

  const [actionError, setActionError] = useState<string | null>(null);

  const [cancelling, setCancelling] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);

  useEffect(() => {
    let active = true;
    void shopApplicationApi
      .getMine()
      .then((result) => {
        if (active) setApplication(result);
      })
      .catch((e: unknown) => {
        if (active) setLoadError(getApiErrorMessage(e));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  async function loadApplication() {
    try {
      setLoading(true);
      setLoadError(null);

      const result = await shopApplicationApi.getMine();

      setApplication(result);
    } catch (err) {
      setLoadError(getApiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  async function handleCancel() {
    if (!application || cancelling) {
      return;
    }

    try {
      setCancelling(true);
      setActionError(null);

      await shopApplicationApi.cancel(application.id);

      await loadApplication();
      setConfirmCancel(false);
      onApplicationChanged?.();
    } catch (err) {
      setActionError(getApiErrorMessage(err));
    } finally {
      setCancelling(false);
    }
  }

  function handleEdit() {
    if (!application) {
      return;
    }

    navigate(`/account/shop-application/edit/${application.id}`);
  }

  function handleStartApplication() {
    navigate("/account/shop-application/new");
  }

  if (loading) {
    return (
      <div className="w-full" role="status" aria-label="Loading your application">
        <LoadingSkeleton />
      </div>
    );
  }

  const status = application ? getStatusMeta(application.status) : null;

  const address = application ? formatAddress(application) : "";

  return (
    <div className="min-w-0 space-y-3">
      {loadError && (
        <ErrorState
          title="Unable to load your application"
          description={loadError}
          onRetry={() => void loadApplication()}
        />
      )}
      {!loadError && !application && (
        <EmptyState
          icon={Store}
          title="Register your shop"
          description="Submit your business details and documents to become a SaveBite Store Owner."
          action={
            <Button onClick={handleStartApplication} className="min-h-11">
              Start application
            </Button>
          }
        />
      )}
      {application && status && (
        <Card padding="none" aria-label="Latest shop application" className="overflow-hidden">
          {application.coverImageUrl && (
            <MediaImage
              src={application.coverImageUrl}
              alt={`${application.name} cover`}
              className="h-28 w-full sm:h-32"
              showFallbackLabel={false}
            />
          )}
          <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border-default bg-primary-50/40 p-4 sm:p-5">
            <div className="flex min-w-0 items-center gap-3">
              <MediaImage
                src={application.logoUrl ?? undefined}
                alt={application.name}
                fallbackIcon={Store}
                showFallbackLabel={false}
                className="size-14 shrink-0 rounded-md"
              />
              <div className="min-w-0">
                <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {application.name}
                </h2>
                <p className="mt-1 text-caption text-text-secondary">
                  Submitted {formatDate(application.createdAt)} · Revision{" "}
                  {application.revisionNumber}
                </p>
              </div>
            </div>
            <Badge
              tone={
                application.status === "Approved"
                  ? "primary"
                  : application.status === "Rejected"
                    ? "danger"
                    : application.status === "Cancelled"
                      ? "neutral"
                      : "warning"
              }
            >
              {status.label}
            </Badge>
          </header>
          <div className="space-y-4 p-4 sm:p-5">
            {actionError && (
              <p
                role="alert"
                className="rounded-md bg-danger-soft p-3 text-body text-danger-strong"
              >
                {actionError}
              </p>
            )}
            <div className={`rounded-md border p-4 ${status.notice}`}>
              <p className="text-body font-semibold">{status.noticeTitle}</p>
              <p className="mt-1 text-body">{status.noticeBody}</p>
              {application.status !== "Cancelled" && (
                <div className="mt-4 border-t border-current/10 pt-4">
                  <ReviewProgress status={application.status} />
                </div>
              )}
            </div>
            {application.description && (
              <p className="text-body text-text-secondary [overflow-wrap:anywhere]">
                {application.description}
              </p>
            )}
            <dl className="divide-y divide-border-default">
              <InfoRow icon={<MapPin size={16} />} label="Address">
                {address || "—"}
              </InfoRow>
              <div className="grid gap-x-6 sm:grid-cols-2">
                <InfoRow icon={<Clock3 size={16} />} label="Opening hours">
                  {formatTime(application.openingTime)} – {formatTime(application.closingTime)}
                </InfoRow>
                <InfoRow icon={<FileText size={16} />} label="Business license">
                  {application.businessLicenseNo || "—"}
                </InfoRow>
              </div>
              <div className="grid gap-x-6 sm:grid-cols-2">
                <InfoRow icon={<Landmark size={16} />} label="Bank account">
                  {application.bankName || "—"}
                  {application.bankAccountNumber
                    ? ` · ${maskAccountNumber(application.bankAccountNumber)}`
                    : ""}
                  {application.bankAccountHolder && (
                    <span className="mt-0.5 block text-caption font-normal text-text-secondary">
                      {application.bankAccountHolder}
                    </span>
                  )}
                </InfoRow>
                <InfoRow icon={<CalendarDays size={16} />} label="Submitted on">
                  {formatDate(application.createdAt)}
                </InfoRow>
              </div>
            </dl>
            {application.status !== "Approved" && (
              <div className="flex flex-wrap justify-end gap-2 border-t border-border-default pt-3">
                {application.status === "Pending" && (
                  <Button
                    variant="secondary"
                    disabled={cancelling}
                    onClick={() => setConfirmCancel(true)}
                    className="min-h-11 !text-danger-strong"
                  >
                    Cancel application
                  </Button>
                )}
                {["Cancelled", "Rejected", "NeedsRevision"].includes(application.status) && (
                  <Button onClick={handleEdit} className="min-h-11">
                    <Pencil size={16} aria-hidden="true" />
                    Edit and resubmit
                  </Button>
                )}
              </div>
            )}
          </div>
        </Card>
      )}
      <Modal
        isOpen={confirmCancel}
        onClose={() => {
          if (!cancelling) setConfirmCancel(false);
        }}
        closeOnClickOutside={!cancelling}
        title="Cancel application?"
      >
        <p className="text-body text-text-secondary">
          Cancel this pending shop application? It will no longer be under review. You can edit and
          resubmit it later.
        </p>
        {actionError && (
          <p role="alert" className="mt-3 text-body text-danger-strong">
            {actionError}
          </p>
        )}
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button
            variant="secondary"
            disabled={cancelling}
            onClick={() => setConfirmCancel(false)}
            className="min-h-11"
          >
            Keep application
          </Button>
          <Button
            variant="danger"
            loading={cancelling}
            onClick={() => void handleCancel()}
            className="min-h-11"
          >
            Confirm cancellation
          </Button>
        </div>
      </Modal>
    </div>
  );
}
