import { useCallback, useEffect, useMemo, useState } from "react";

import { staffInvitationApi } from "@/features/staffInvitation/api/staffInvitationApi";
import type {
  StaffInvitation,
  StaffInvitationStatus,
} from "@/features/staffInvitation/types/staffInvitation.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import { useAuthStore } from "@/shared/stores/authStore";

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en-US", {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function getDisplayedStatus(invitation: StaffInvitation): StaffInvitationStatus | "Expired" {
  const isExpired =
    invitation.status === "Pending" && new Date(invitation.expiresAt).getTime() <= Date.now();

  return isExpired ? "Expired" : invitation.status;
}

export function StaffInvitationsPage() {
  const [invitations, setInvitations] = useState<StaffInvitation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [processingId, setProcessingId] = useState<string | null>(null);
  const refreshCurrentUser = useAuthStore((state) => state.refreshCurrentUser);

  const loadInvitations = useCallback(async () => {
    try {
      setIsLoading(true);

      const result = await staffInvitationApi.getMyInvitations();

      setInvitations(result);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to load staff invitations.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadInvitations();
  }, [loadInvitations]);

  const sortedInvitations = useMemo(
    () =>
      [...invitations].sort(
        (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
      ),
    [invitations],
  );

  const handleAccept = async (invitationId: string) => {
    try {
      setProcessingId(invitationId);

      await staffInvitationApi.acceptInvitation(invitationId);

      await refreshCurrentUser();

      toast.success("Staff invitation accepted successfully.");

      await loadInvitations();
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to accept staff invitation.");
    } finally {
      setProcessingId(null);
    }
  };

  const handleDecline = async (invitationId: string) => {
    try {
      setProcessingId(invitationId);

      await staffInvitationApi.declineInvitation(invitationId);

      toast.success("Staff invitation declined successfully.");

      await loadInvitations();
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to decline staff invitation.");
    } finally {
      setProcessingId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="p-6">
        <p className="text-sm text-gray-500">Loading staff invitations...</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Staff Invitations</h1>

        <p className="mt-1 text-sm text-gray-500">View and respond to invitations from shops.</p>
      </div>

      {sortedInvitations.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-gray-300 bg-white py-16 text-center">
          <p className="font-medium text-gray-900">No staff invitations</p>

          <p className="mt-1 text-sm text-gray-500">
            You do not have any staff invitations at the moment.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {sortedInvitations.map((invitation) => {
            const status = getDisplayedStatus(invitation);

            const isPending = status === "Pending";

            const isProcessing = processingId === invitation.id;

            return (
              <div
                key={invitation.id}
                className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm transition hover:shadow-md"
              >
                <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                  <div className="space-y-2">
                    <div className="flex flex-wrap items-center gap-3">
                      <h2 className="text-lg font-semibold text-gray-900">{invitation.shopName}</h2>

                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ${
                          status === "Pending"
                            ? "bg-amber-50 text-amber-700 ring-amber-200"
                            : status === "Accepted"
                              ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
                              : status === "Declined"
                                ? "bg-red-50 text-red-700 ring-red-200"
                                : "bg-gray-100 text-gray-600 ring-gray-200"
                        }`}
                      >
                        {status}
                      </span>
                    </div>

                    <div className="space-y-1 text-sm text-gray-500">
                      <p>Invited: {formatDate(invitation.createdAt)}</p>

                      <p>Expires: {formatDate(invitation.expiresAt)}</p>
                    </div>
                  </div>

                  {isPending && (
                    <div className="flex gap-3">
                      <button
                        type="button"
                        disabled={isProcessing}
                        onClick={() => void handleDecline(invitation.id)}
                        className="rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {isProcessing ? "Processing..." : "Decline"}
                      </button>

                      <button
                        type="button"
                        disabled={isProcessing}
                        onClick={() => void handleAccept(invitation.id)}
                        className="rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {isProcessing ? "Processing..." : "Accept"}
                      </button>
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
