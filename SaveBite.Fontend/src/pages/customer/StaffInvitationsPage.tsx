import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Badge,
  Button,
  Card,
  EmptyState,
  ErrorState,
  MediaImage,
  Skeleton,
} from "@/shared/components/ui";
import { Link } from "react-router-dom";
import { ArrowUpRight, Check, Clock3, Mail, MapPin, Store, X } from "lucide-react";
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
    const initial = window.setTimeout(() => void loadInvitations(), 0);
    // Refresh status when Hangfire expires an invitation, including on returning to this tab.
    const timer = window.setInterval(() => {
      void loadInvitations();
    }, 30_000);
    const onFocus = () => {
      void loadInvitations();
    };
    window.addEventListener("focus", onFocus);
    return () => {
      window.clearTimeout(initial);
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
    <div className="min-w-0 space-y-3">
      <p className="text-caption text-text-secondary">Invitations expire after 3 days.</p>
      {error && (
        <ErrorState
          title="Couldn't load invitations"
          description={error}
          onRetry={() => void loadInvitations()}
        />
      )}
      {isLoading ? (
        <div role="status" aria-label="Loading invitations" className="space-y-3">
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
        </div>
      ) : !sortedInvitations.length && !error ? (
        <EmptyState
          icon={Mail}
          title="No invitations yet"
          description="Shop invitations and team opportunities will appear here."
        />
      ) : (
        paginatedItems.map((invitation) => {
          const status = displayedStatus(invitation, now),
            pending = status === "Pending",
            processing = processingId === invitation.id;
          return (
            <Card key={invitation.id} className="!p-3 sm:!p-4">
              <div className="flex items-start gap-3">
                <MediaImage
                  src={invitation.shopLogoUrl || invitation.shopCoverImageUrl || undefined}
                  alt={invitation.shopName}
                  fallbackIcon={Store}
                  showFallbackLabel={false}
                  className="size-11 shrink-0 rounded-md"
                />
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <h2 className="min-w-0 text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
                      <Link
                        to={`/shops/${invitation.shopId}`}
                        className="inline-flex items-start gap-1 hover:text-primary-700"
                      >
                        {invitation.shopName}
                        <ArrowUpRight size={15} aria-hidden="true" className="mt-0.5 shrink-0" />
                      </Link>
                    </h2>
                    <Badge
                      tone={
                        status === "Pending"
                          ? "warning"
                          : status === "Accepted"
                            ? "primary"
                            : status === "Declined"
                              ? "danger"
                              : "neutral"
                      }
                    >
                      {status === "Accepted" ? "Joined" : status}
                    </Badge>
                  </div>
                  {invitation.shopAddress && (
                    <p className="mt-1 flex items-start gap-1 text-caption text-text-secondary">
                      <MapPin size={13} className="mt-0.5 shrink-0" aria-hidden="true" />
                      <span className="[overflow-wrap:anywhere]">{invitation.shopAddress}</span>
                    </p>
                  )}
                  <div className="mt-2 flex flex-wrap items-center justify-between gap-2">
                    <p
                      className={`inline-flex items-center gap-1 text-caption ${pending ? "text-accent-700" : "text-text-secondary"}`}
                    >
                      <Clock3 size={13} aria-hidden="true" />
                      {status === "Expired" ? "Expired" : "Deadline"}{" "}
                      {formatDate(invitation.expiresAt)}
                    </p>
                    {pending && (
                      <div className="flex flex-wrap gap-2">
                        <Button
                          variant="secondary"
                          disabled={processingId !== null}
                          onClick={() => void respond(invitation, false)}
                          aria-label={`Decline invitation from ${invitation.shopName}`}
                          className="min-h-11"
                        >
                          <X size={14} aria-hidden="true" />
                          Decline
                        </Button>
                        <Button
                          disabled={processingId !== null}
                          loading={processing}
                          onClick={() => void respond(invitation, true)}
                          aria-label={`Accept invitation from ${invitation.shopName}`}
                          className="min-h-11"
                        >
                          <Check size={14} aria-hidden="true" />
                          Accept
                        </Button>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </Card>
          );
        })
      )}
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
  );
}

export { CustomerStaffInvitationsPage as StaffInvitationsPage };
export default CustomerStaffInvitationsPage;
