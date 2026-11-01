import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowUpRight, Check, Clock3, Mail, MapPin, RefreshCw, Store, X } from "lucide-react";
import { staffInvitationApi } from "@/features/staff-invitations/api/staffInvitationApi";
import type {
  StaffInvitation,
  StaffInvitationStatus,
} from "@/features/staff-invitations/types/staffInvitation.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import { useAuthStore } from "@/shared/stores/authStore";
import { Pagination, usePagination } from "@/shared/components/pagination";

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function displayedStatus(invitation: StaffInvitation, now: number): StaffInvitationStatus {
  return invitation.status === "Pending" && new Date(invitation.expiresAt).getTime() <= now
    ? "Expired"
    : invitation.status;
}

const statusStyles: Record<StaffInvitationStatus, string> = {
  Pending: "bg-amber-50 text-amber-800 ring-amber-200",
  Accepted: "bg-emerald-50 text-emerald-800 ring-emerald-200",
  Declined: "bg-rose-50 text-rose-700 ring-rose-200",
  Expired: "bg-slate-100 text-slate-600 ring-slate-200",
  Cancelled: "bg-slate-100 text-slate-600 ring-slate-200",
};

function ShopImage({ src, name }: { src?: string | null; name: string }) {
  const [failed, setFailed] = useState(false);
  return src && !failed ? (
    <img
      src={src}
      alt={`${name} shop`}
      loading="lazy"
      onError={() => setFailed(true)}
      className="h-full w-full object-cover"
    />
  ) : (
    <div className="flex h-full w-full items-center justify-center bg-emerald-50 text-emerald-600">
      <Store size={40} strokeWidth={1.5} aria-hidden="true" />
    </div>
  );
}

export function CustomerStaffInvitationsPage() {
  const [invitations, setInvitations] = useState<StaffInvitation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [processingId, setProcessingId] = useState<string | null>(null);
  const [now, setNow] = useState(Date.now);
  const refreshUser = useAuthStore((state) => state.refreshUser);

  const loadInvitations = useCallback(async () => {
    try {
      const result = await staffInvitationApi.getMyInvitations();
      setInvitations(result);
      setError("");
      setNow(Date.now());
    } catch (err) {
      setError(getApiErrorMessage(err) || "Unable to load invitations. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadInvitations();
    // Refresh status when Hangfire expires an invitation, including on returning to this tab.
    const timer = window.setInterval(() => {
      void loadInvitations();
    }, 30_000);
    const onFocus = () => {
      void loadInvitations();
    };
    window.addEventListener("focus", onFocus);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", onFocus);
    };
  }, [loadInvitations]);

  // Disable response buttons at the deadline even if the next server refresh has not completed.
  useEffect(() => {
    const deadline = Math.min(
      ...invitations
        .filter((i) => i.status === "Pending")
        .map((i) => new Date(i.expiresAt).getTime())
        .filter((time) => time > now),
    );
    if (!Number.isFinite(deadline)) return;
    const timer = window.setTimeout(
      () => setNow(Date.now()),
      Math.min(deadline - now + 50, 2_147_483_647),
    );
    return () => window.clearTimeout(timer);
  }, [invitations, now]);

  const sortedInvitations = useMemo(
    () =>
      [...invitations].sort(
        (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
      ),
    [invitations],
  );
  const { currentPage, setCurrentPage, totalPages, paginatedItems, totalItems } = usePagination({
    items: sortedInvitations,
    initialPageSize: 5,
  });

  const respond = async (invitation: StaffInvitation, accept: boolean) => {
    try {
      setProcessingId(invitation.id);
      if (accept) await staffInvitationApi.acceptInvitation(invitation.id);
      else await staffInvitationApi.declineInvitation(invitation.id);
      toast.success(
        accept
          ? `You have joined ${invitation.shopName}. Welcome to the team!`
          : "Invitation declined.",
      );
      await loadInvitations();
      if (accept) await refreshUser();
    } catch (err) {
      toast.error(getApiErrorMessage(err) || "Unable to respond to this invitation.");
      await loadInvitations();
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <div className="mx-auto max-w-5xl space-y-4 p-4 sm:p-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-lg font-semibold text-slate-900">Your invitations</h1>
          <p className="mt-1 text-sm text-slate-500">Invitations expire after 3 days.</p>
        </div>
        <button
          type="button"
          onClick={() => void loadInvitations()}
          disabled={isLoading}
          className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:opacity-50"
        >
          <RefreshCw size={15} /> Refresh
        </button>
      </div>

      {error && (
        <div
          role="alert"
          className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700"
        >
          {error}{" "}
          <button
            type="button"
            onClick={() => void loadInvitations()}
            className="ml-2 font-semibold underline"
          >
            Retry
          </button>
        </div>
      )}
      {isLoading ? (
        <div role="status" className="space-y-4" aria-label="Loading invitations">
          {[1, 2].map((i) => (
            <div key={i} className="h-28 animate-pulse rounded-2xl bg-slate-100" />
          ))}
        </div>
      ) : sortedInvitations.length === 0 && !error ? (
        <div className="rounded-3xl border border-dashed border-slate-200 bg-white px-6 py-16 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
            <Mail size={30} />
          </div>
          <h2 className="text-lg font-semibold text-slate-900">
            Your next opportunity starts here
          </h2>
          <p className="mx-auto mt-2 max-w-sm text-sm leading-6 text-slate-500">
            When a shop invites you to join their team, you will find the invitation here and
            receive a notification.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {paginatedItems.map((invitation) => {
            const status = displayedStatus(invitation, now);
            const pending = status === "Pending";
            const processing = processingId === invitation.id;
            const image = invitation.shopCoverImageUrl || invitation.shopLogoUrl;
            return (
              <article
                key={invitation.id}
                className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm"
              >
                <div className="flex items-start gap-3 p-4 sm:gap-4">
                  <div className="h-16 w-16 shrink-0 overflow-hidden rounded-xl sm:h-20 sm:w-20">
                    <ShopImage key={image} src={image} name={invitation.shopName} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <Link
                        to={`/shops/${invitation.shopId}`}
                        className="inline-flex min-w-0 max-w-full items-center gap-1 text-base font-semibold text-slate-900 hover:text-emerald-700"
                      >
                        <span className="truncate">{invitation.shopName}</span>
                        <ArrowUpRight size={15} className="shrink-0" />
                      </Link>
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${statusStyles[status]}`}
                      >
                        {status === "Accepted" ? "Joined" : status}
                      </span>
                    </div>
                    {invitation.shopAddress && (
                      <p className="mt-1 flex items-center gap-1 text-xs text-slate-500">
                        <MapPin size={13} className="shrink-0" />
                        <span className="truncate">{invitation.shopAddress}</span>
                      </p>
                    )}
                    <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
                      <p
                        className={`inline-flex items-center gap-1 text-xs ${pending ? "text-amber-700" : "text-slate-500"}`}
                      >
                        <Clock3 size={13} /> {status === "Expired" ? "Expired" : "Deadline"}{" "}
                        {formatDate(invitation.expiresAt)}
                      </p>
                      {pending && (
                        <div className="flex gap-2">
                          <button
                            type="button"
                            disabled={processingId !== null}
                            onClick={() => void respond(invitation, false)}
                            aria-label={`Decline invitation from ${invitation.shopName}`}
                            className="inline-flex items-center justify-center gap-1 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50"
                          >
                            <X size={14} /> Decline
                          </button>
                          <button
                            type="button"
                            disabled={processingId !== null}
                            onClick={() => void respond(invitation, true)}
                            aria-label={`Accept invitation from ${invitation.shopName}`}
                            className="inline-flex items-center justify-center gap-1 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
                          >
                            <Check size={14} /> {processing ? "Processing…" : "Accept"}
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </article>
            );
          })}
          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalItems}
              pageSize={5}
              itemLabel="invitations"
              onPageChange={setCurrentPage}
              showFirstLast
            />
          )}
        </div>
      )}
    </div>
  );
}

export { CustomerStaffInvitationsPage as StaffInvitationsPage };
export default CustomerStaffInvitationsPage;
