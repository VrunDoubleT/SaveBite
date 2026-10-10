import { useEffect, useState, type ReactNode } from "react";
import {
  AlertCircle,
  Building2,
  CalendarDays,
  Check,
  CheckCircle2,
  Clock3,
  FileText,
  Hourglass,
  Landmark,
  Loader2,
  MapPin,
  Pencil,
  RotateCcw,
  Store,
  XCircle,
} from "lucide-react";
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
    badge: "bg-amber-50 text-amber-800 ring-amber-200",
    dot: "bg-amber-500",
    notice: "border-amber-200 bg-amber-50 text-amber-900",
    noticeTitle: "We're reviewing your application",
    noticeBody: "Your application cannot be edited while it is under review. You can cancel it if needed.",
  },
  Approved: {
    label: "Approved",
    tone: "green",
    badge: "bg-emerald-50 text-emerald-800 ring-emerald-200",
    dot: "bg-emerald-500",
    notice: "border-emerald-200 bg-emerald-50 text-emerald-900",
    noticeTitle: "Your shop is approved",
    noticeBody: "You're now a SaveBite Store Owner and can start managing your shop.",
  },
  Rejected: {
    label: "Rejected",
    tone: "red",
    badge: "bg-red-50 text-red-800 ring-red-200",
    dot: "bg-red-500",
    notice: "border-red-200 bg-red-50 text-red-900",
    noticeTitle: "Your application wasn't approved",
    noticeBody: "You can update your application details and submit it again.",
  },
  Cancelled: {
    label: "Canceled",
    tone: "gray",
    badge: "bg-slate-100 text-slate-700 ring-slate-200",
    dot: "bg-slate-400",
    notice: "border-slate-200 bg-slate-50 text-slate-700",
    noticeTitle: "This application was canceled",
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
    <div className="flex items-start gap-4 py-4">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
        {icon}
      </div>

      <div className="min-w-0 flex-1">
        <dt className="text-sm text-slate-500">{label}</dt>

        <dd className="mt-0.5 break-words text-sm font-medium text-slate-900">{children}</dd>
      </div>
    </div>
  );
}

/* Review progress */
function ReviewProgress({ status }: { status: ShopApplicationStatus }) {
  const decided = status === "Approved" || status === "Rejected";
  const steps = [
    { label: "Submitted", state: "done" as const },
    { label: "Under review", state: status === "Pending" ? ("active" as const) : ("done" as const) },
    {
      label: status === "Approved" ? "Approved" : status === "Rejected" ? "Rejected" : "Decision",
      state: decided ? (status === "Rejected" ? ("error" as const) : ("done" as const)) : ("todo" as const),
    },
  ];
  const circle = {
    done: "bg-emerald-600 text-white",
    active: "bg-white text-amber-600 ring-2 ring-amber-400",
    error: "bg-red-500 text-white",
    todo: "bg-white text-slate-300 ring-2 ring-slate-200",
  };

  return (
    <ol className="flex items-center" aria-label="Application progress">
      {steps.map((step, i) => (
        <li key={step.label} className="flex flex-1 items-center last:flex-none">
          <div className="flex items-center gap-2.5">
            <span className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full ${circle[step.state]}`}>
              {step.state === "done" && <Check className="h-4 w-4" />}
              {step.state === "active" && <Hourglass className="h-3.5 w-3.5" />}
              {step.state === "error" && <XCircle className="h-4 w-4" />}
              {step.state === "todo" && <span className="h-1.5 w-1.5 rounded-full bg-slate-300" />}
            </span>
            <span className={`text-sm ${step.state === "todo" ? "text-slate-400" : "font-medium text-slate-800"}`}>
              {step.label}
            </span>
          </div>
          {i < steps.length - 1 && (
            <span className={`mx-3 h-0.5 flex-1 rounded-full ${steps[i + 1].state === "todo" ? "bg-slate-200" : "bg-emerald-500"}`} />
          )}
        </li>
      ))}
    </ol>
  );
}

/* Loading skeleton */
function LoadingSkeleton() {
  return (
    <div className="animate-pulse overflow-hidden rounded-2xl border border-slate-200 bg-white">
      <div className="h-44 bg-slate-100 sm:h-52" />

      <div className="space-y-4 p-6 sm:p-8">
        <div className="-mt-14 h-20 w-20 rounded-2xl bg-slate-200 ring-4 ring-white" />

        <div className="h-6 w-56 rounded bg-slate-200" />

        <div className="h-4 w-full max-w-md rounded bg-slate-100" />

        <div className="space-y-3 pt-4">
          <div className="h-12 rounded-lg bg-slate-100" />
          <div className="h-12 rounded-lg bg-slate-100" />
          <div className="h-12 rounded-lg bg-slate-100" />
        </div>
      </div>
    </div>
  );
}

/* Page */
export default function LatestShopApplication({ onApplicationChanged }: { onApplicationChanged?: () => void }) {
  const navigate = useNavigate();

  const [application, setApplication] = useState<ShopApplication | null>(null);

  const [loading, setLoading] = useState(true);

  const [loadError, setLoadError] = useState<string | null>(null);

  const [actionError, setActionError] = useState<string | null>(null);

  const [cancelling, setCancelling] = useState(false);


  useEffect(() => {
    loadApplication();
  }, []);

  async function loadApplication() {
    try {
      setLoading(true);
      setLoadError(null);

      const result = await shopApplicationApi.getMine();

      setApplication(result);
    } catch (err: any) {
      setLoadError(
        err?.response?.data?.message || err?.message || "Unable to load your shop application.",
      );
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
      onApplicationChanged?.();
    } catch (err: any) {
      setActionError(
        err?.response?.data?.message || err?.message || "Unable to cancel the shop application.",
      );
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

  const btnBase =
    "inline-flex items-center justify-center gap-2 rounded-xl border px-5 py-2.5 text-sm font-semibold transition focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60";

  const primaryBtn = `${btnBase} border-emerald-200 bg-emerald-50 text-emerald-700 hover:border-emerald-300 hover:bg-emerald-100 focus-visible:ring-emerald-500`;

  const dangerBtn = `${btnBase} border-red-200 bg-red-50 text-red-700 hover:border-red-300 hover:bg-red-100 focus-visible:ring-red-500 disabled:hover:border-red-200 disabled:hover:bg-red-50`;

  return (
    <div className="w-full">
      {/* Load error */}
      {loadError && (
        <div
          role="alert"
          className="flex items-start gap-3 rounded-2xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-800"
        >
          <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-red-500" />

          <div className="flex-1">
            <p className="font-semibold">Unable to load your application</p>

            <p className="mt-1 text-red-700">{loadError}</p>
          </div>

          <button
            type="button"
            onClick={loadApplication}
            className="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-red-200 bg-white px-3 py-1.5 font-medium text-red-700 transition hover:bg-red-100"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            Try again
          </button>
        </div>
      )}

      {/* Empty state */}
      {!application && !loadError && (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white text-center shadow-sm">
          <div className="bg-gradient-to-b from-emerald-50 to-white px-6 pb-12 pt-14">
            <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-white shadow-sm ring-1 ring-emerald-100">
              <Store className="h-8 w-8 text-emerald-600" />
            </div>

            <h2 className="mt-6 text-xl font-semibold text-slate-900">
              Open your shop on SaveBite
            </h2>

            <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-600">
              You haven't submitted a shop application yet. Add your shop details to become a
              SaveBite Store Owner.
            </p>

            <button type="button" onClick={handleStartApplication} className={`${primaryBtn} mt-7`}>
              <Store className="h-4 w-4" />
              Start application
            </button>
          </div>
        </div>
      )}

      {/* Application */}
      {application && status && (
        <article className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          {/* Cover */}
          <div className="relative h-48 w-full overflow-hidden bg-gradient-to-br from-emerald-600 via-emerald-500 to-teal-500 sm:h-60">
            {application.coverImageUrl ? (
              <>
                <img
                  src={application.coverImageUrl}
                  alt={`${application.name} cover`}
                  className="h-full w-full object-cover"
                />

                <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/35 via-black/5 to-transparent" />
              </>
            ) : (
              <>
                <div
                  className="absolute inset-0 opacity-30"
                  style={{
                    backgroundImage:
                      "radial-gradient(circle, rgba(255,255,255,0.7) 1.5px, transparent 1.5px)",
                    backgroundSize: "22px 22px",
                  }}
                />

                <div className="absolute -right-16 -top-16 h-64 w-64 rounded-full bg-white/15 blur-2xl" />

                <div className="absolute -bottom-20 left-10 h-56 w-56 rounded-full bg-teal-300/30 blur-2xl" />

                <div className="relative flex h-full items-center justify-center">
                  <div className="flex items-center gap-2 rounded-full bg-white/20 px-4 py-2 text-sm font-medium text-white backdrop-blur-sm">
                    <Building2 className="h-4 w-4" />
                    No cover image
                  </div>
                </div>
              </>
            )}
          </div>

          <div className="px-5 pb-6 sm:px-8 sm:pb-8">
            {/* Header */}
            <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
              <div className="flex items-end gap-4">
                <div className="relative z-10 -mt-12 h-24 w-24 shrink-0 overflow-hidden rounded-3xl border-4 border-white bg-white shadow-lg shadow-slate-900/10 ring-1 ring-slate-200 sm:-mt-16 sm:h-28 sm:w-28">
                  {application.logoUrl ? (
                    <img
                      src={application.logoUrl}
                      alt={`${application.name} logo`}
                      className="h-full w-full rounded-[1.25rem] object-cover"
                    />
                  ) : (
                    <div className="flex h-full w-full items-center justify-center rounded-[1.25rem] bg-gradient-to-br from-emerald-50 to-teal-100">
                      <span className="text-3xl font-semibold text-emerald-700 sm:text-4xl">
                        {application.name?.trim().charAt(0).toUpperCase() || (
                          <Store className="h-10 w-10" />
                        )}
                      </span>
                    </div>
                  )}
                </div>

                <div className="min-w-0 pb-1">
                  <h1 className="truncate text-xl font-semibold tracking-tight text-slate-900 sm:text-2xl">
                    {application.name}
                  </h1>

                  <p className="mt-0.5 text-sm text-slate-500">
                    Submitted {formatDate(application.createdAt)}
                  </p>
                </div>
              </div>

              <span
                className={`inline-flex w-fit items-center gap-2 rounded-full px-3 py-1.5 text-sm font-medium ring-1 ring-inset ${status.badge}`}
              >
                <span className={`h-2 w-2 rounded-full ${status.dot}`} />

                {status.label}
              </span>
            </div>

            {/* Action error */}
            {actionError && (
              <div
                role="alert"
                className="mt-5 flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
              >
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" />

                <p>{actionError}</p>
              </div>
            )}

            {/* Status notice + progress */}
            <div className={`mt-6 rounded-xl border p-4 sm:p-5 ${status.notice}`}>
              <div className="flex items-start gap-3">
                {application.status === "Approved" ? (
                  <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0" />
                ) : application.status === "Pending" ? (
                  <Hourglass className="mt-0.5 h-5 w-5 shrink-0" />
                ) : (
                  <AlertCircle className="mt-0.5 h-5 w-5 shrink-0" />
                )}

                <div>
                  <p className="text-sm font-semibold">{status.noticeTitle}</p>

                  <p className="mt-0.5 text-sm opacity-80">{status.noticeBody}</p>
                </div>
              </div>

              {application.status !== "Cancelled" && (
                <div className="mt-5 rounded-lg bg-white/70 px-4 py-3.5">
                  <ReviewProgress status={application.status} />
                </div>
              )}
            </div>

            {/* Description */}
            {application.description && (
              <p className="mt-6 max-w-2xl text-sm leading-6 text-slate-600">
                {application.description}
              </p>
            )}

            {/* Details */}
            <dl className="mt-4 divide-y divide-slate-100 border-t border-slate-100">
              <InfoRow icon={<MapPin className="h-[18px] w-[18px]" />} label="Address">
                {address || "—"}
              </InfoRow>

              <div className="grid sm:grid-cols-2 sm:gap-x-8 sm:divide-x-0">
                <InfoRow icon={<Clock3 className="h-[18px] w-[18px]" />} label="Opening hours">
                  {formatTime(application.openingTime)} – {formatTime(application.closingTime)}
                </InfoRow>

                <div className="border-t border-slate-100 sm:border-t-0">
                  <InfoRow
                    icon={<FileText className="h-[18px] w-[18px]" />}
                    label="Business license"
                  >
                    {application.businessLicenseNo || "—"}
                  </InfoRow>
                </div>
              </div>

              <div className="grid sm:grid-cols-2 sm:gap-x-8">
                <InfoRow icon={<Landmark className="h-[18px] w-[18px]" />} label="Bank account">
                  <span>
                    {application.bankName || "—"}

                    {application.bankAccountNumber
                      ? ` · ${maskAccountNumber(application.bankAccountNumber)}`
                      : ""}
                  </span>

                  {application.bankAccountHolder && (
                    <span className="mt-0.5 block text-xs font-normal text-slate-500">
                      {application.bankAccountHolder}
                    </span>
                  )}
                </InfoRow>

                <div className="border-t border-slate-100 sm:border-t-0">
                  <InfoRow
                    icon={<CalendarDays className="h-[18px] w-[18px]" />}
                    label="Submitted on"
                  >
                    {formatDate(application.createdAt)}
                  </InfoRow>
                </div>
              </div>
            </dl>

            {/* Actions */}
            {application.status !== "Approved" && (
              <div className="mt-2 flex flex-col-reverse gap-3 border-t border-slate-100 pt-6 sm:flex-row sm:items-center sm:justify-end">
                {application.status === "Pending" && (
                  <button
                    type="button"
                    disabled={cancelling}
                    onClick={handleCancel}
                    className={dangerBtn}
                  >
                    {cancelling ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      <XCircle className="h-4 w-4" />
                    )}

                    {cancelling ? "Cancelling..." : "Cancel application"}
                  </button>
                )}

                {(application.status === "Cancelled" || application.status === "Rejected") && (
                  <button type="button" onClick={handleEdit} className={primaryBtn}>
                    <Pencil className="h-4 w-4" />
                    Edit and resubmit
                  </button>
                )}


              </div>
            )}
          </div>
        </article>
      )}
    </div>
  );
}
