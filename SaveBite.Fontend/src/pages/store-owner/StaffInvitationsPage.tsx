import { useEffect, useState } from "react";
import {
  Mail,
  RefreshCw,
  Send,
  Trash2,
  Users,
} from "lucide-react";

import { shopApi } from "@/features/shop/api/shopApi";
import { shopStaffApi } from "@/features/shopStaff/api/shopStaffApi";

import type {
  StaffInvitation,
} from "@/features/shopStaff/types/shopStaff.types";
import {
  getApiErrorMessage,
} from "@/shared/api/httpClient";

import { toast } from "@/shared/stores/toastStore";

function formatDate(date: string) {
  return new Date(date).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function formatDateTime(date: string) {
  return new Date(date).toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function getStatusClass(status: string) {
  switch (status.toLowerCase()) {
    case "pending":
      return "bg-amber-50 text-amber-700 ring-1 ring-amber-200";

    case "accepted":
      return "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200";

    case "declined":
      return "bg-red-50 text-red-700 ring-1 ring-red-200";

    case "cancelled":
    case "canceled":
      return "bg-gray-100 text-gray-600 ring-1 ring-gray-200";

    default:
      return "bg-gray-100 text-gray-600 ring-1 ring-gray-200";
  }
}

function getStatusLabel(status: string) {
  switch (status.toLowerCase()) {
    case "pending":
      return "Pending";

    case "accepted":
      return "Accepted";

    case "declined":
      return "Declined";

    case "cancelled":
    case "canceled":
      return "Cancelled";

    default:
      return status;
  }
}

interface RevokeModalProps {
  invitation: StaffInvitation;
  loading: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

function RevokeInvitationModal({
  invitation,
  loading,
  onClose,
  onConfirm,
}: RevokeModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-xl">
        <div className="border-b border-gray-100 px-6 py-5">
          <h2 className="text-lg font-semibold text-gray-900">
            Revoke invitation
          </h2>

          <p className="mt-1 text-sm text-gray-500">
            This invitation will no longer be available to
            the customer.
          </p>
        </div>

        <div className="px-6 py-5">
          <div className="rounded-xl bg-gray-50 p-4">
            <p className="font-medium text-gray-900">
              {invitation.invitedUserName}
            </p>

            <p className="mt-1 text-sm text-gray-500">
              {invitation.invitedUserEmail}
            </p>
          </div>

          <p className="mt-4 text-sm leading-6 text-gray-600">
            Are you sure you want to revoke this invitation?
          </p>
        </div>

        <div className="flex justify-end gap-3 border-t border-gray-100 bg-gray-50 px-6 py-4">
          <button
            type="button"
            disabled={loading}
            onClick={onClose}
            className="rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-100 disabled:opacity-50"
          >
            Cancel
          </button>

          <button
            type="button"
            disabled={loading}
            onClick={onConfirm}
            className="inline-flex items-center gap-2 rounded-xl bg-red-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-red-700 disabled:opacity-50"
          >
            <Trash2 size={16} />

            {loading ? "Revoking..." : "Revoke"}
          </button>
        </div>
      </div>
    </div>
  );
}

export function StaffInvitationsPage() {
  const [shopId, setShopId] = useState<string | null>(null);

  const [invitations, setInvitations] = useState<
    StaffInvitation[]
  >([]);

  const [loading, setLoading] = useState(false);
  const [revoking, setRevoking] = useState(false);

  const [selectedInvitation, setSelectedInvitation] =
    useState<StaffInvitation | null>(null);

 async function loadShop() {
  try {
    const response =
      await shopApi.getMyShop();

    setShopId(
      response.data.data.id,
    );
  } catch (error) {
    console.error(error);

    toast.error(
      getApiErrorMessage(error) ||
        "Unable to load your shop information.",
    );
  }
}

 async function loadInvitations(
  id: string,
) {
  try {
    setLoading(true);

    const response =
      await shopStaffApi.getInvitations(
        id,
      );

    setInvitations(
      response.data.data,
    );
  } catch (error) {
    console.error(error);

    toast.error(
      getApiErrorMessage(error) ||
        "Unable to load staff invitations.",
    );
  } finally {
    setLoading(false);
  }
}

  useEffect(() => {
    loadShop();
  }, []);

  useEffect(() => {
    if (!shopId) return;

    loadInvitations(shopId);
  }, [shopId]);

 async function handleRevoke() {
  if (
    !shopId ||
    !selectedInvitation
  ) {
    return;
  }

  try {
    setRevoking(true);

    const response =
      await shopStaffApi.revokeInvitation(
        shopId,
        selectedInvitation.id,
      );

    setSelectedInvitation(null);

    toast.success(
      response.data.message ??
        "Staff invitation revoked successfully.",
    );

    await loadInvitations(shopId);
  } catch (error) {
    console.error(error);

    toast.error(
      getApiErrorMessage(error) ||
        "Unable to revoke this invitation.",
    );
  } finally {
    setRevoking(false);
  }
}

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Staff Invitations
          </h1>

          <p className="mt-1 text-sm text-gray-500">
            View and manage staff invitations sent from your
            shop.
          </p>
        </div>

        <button
          type="button"
          disabled={!shopId || loading}
          onClick={() => {
            if (shopId) {
              loadInvitations(shopId);
            }
          }}
          className="inline-flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 shadow-sm transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <RefreshCw
            size={16}
            className={loading ? "animate-spin" : ""}
          />

          Refresh
        </button>
      </div>

      {/* Summary */}
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
              <Send size={20} />
            </div>

            <div>
              <p className="text-sm text-gray-500">
                Total invitations
              </p>

              <p className="text-2xl font-bold text-gray-900">
                {invitations.length}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-50 text-amber-600">
              <Mail size={20} />
            </div>

            <div>
              <p className="text-sm text-gray-500">
                Pending
              </p>

              <p className="text-2xl font-bold text-gray-900">
                {
                  invitations.filter(
                    (item) =>
                      item.status.toLowerCase() ===
                      "pending",
                  ).length
                }
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
              <Users size={20} />
            </div>

            <div>
              <p className="text-sm text-gray-500">
                Accepted
              </p>

              <p className="text-2xl font-bold text-gray-900">
                {
                  invitations.filter(
                    (item) =>
                      item.status.toLowerCase() ===
                      "accepted",
                  ).length
                }
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Table */}
      {loading ? (
        <div className="rounded-2xl border border-gray-200 bg-white p-6">
          <div className="space-y-3">
            {[1, 2, 3].map((item) => (
              <div
                key={item}
                className="h-14 animate-pulse rounded-xl bg-gray-100"
              />
            ))}
          </div>
        </div>
      ) : invitations.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-gray-300 bg-white py-16 text-center">
          <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-gray-100 text-gray-400">
            <Mail size={22} />
          </div>

          <p className="font-medium text-gray-900">
            No invitations yet
          </p>

          <p className="mt-1 text-sm text-gray-500">
            Invitations you send to customers will appear
            here.
          </p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px]">
              <thead className="border-b border-gray-200 bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Customer
                  </th>

                  <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Status
                  </th>

                  <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Sent date
                  </th>

                  <th className="px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Expires
                  </th>

                  <th className="px-6 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                    Actions
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-gray-100">
                {invitations.map((invitation) => {
                  const isPending =
                    invitation.status.toLowerCase() ===
                    "pending";

                  return (
                    <tr
                      key={invitation.id}
                      className="transition hover:bg-gray-50/70"
                    >
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-sm font-semibold text-emerald-700">
                            {invitation.invitedUserName
                              ?.charAt(0)
                              .toUpperCase() ?? "U"}
                          </div>

                          <div>
                            <p className="font-medium text-gray-900">
                              {invitation.invitedUserName}
                            </p>

                            <p className="text-sm text-gray-500">
                              {invitation.invitedUserEmail}
                            </p>
                          </div>
                        </div>
                      </td>

                      <td className="px-6 py-4">
                        <span
                          className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${getStatusClass(
                            invitation.status,
                          )}`}
                        >
                          {getStatusLabel(
                            invitation.status,
                          )}
                        </span>
                      </td>

                      <td className="px-6 py-4 text-sm text-gray-600">
                        {formatDateTime(
                          invitation.createdAt,
                        )}
                      </td>

                      <td className="px-6 py-4 text-sm text-gray-600">
                        {formatDate(
                          invitation.expiresAt,
                        )}
                      </td>

                      <td className="px-6 py-4 text-right">
                        {isPending ? (
                          <button
                            type="button"
                            onClick={() =>
                              setSelectedInvitation(
                                invitation,
                              )
                            }
                            className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-red-600 transition hover:bg-red-50"
                          >
                            <Trash2 size={16} />
                            Revoke
                          </button>
                        ) : (
                          <span className="text-sm text-gray-400">
                            —
                          </span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Revoke modal */}
      {selectedInvitation && (
        <RevokeInvitationModal
          invitation={selectedInvitation}
          loading={revoking}
          onClose={() => {
            if (!revoking) {
              setSelectedInvitation(null);
            }
          }}
          onConfirm={handleRevoke}
        />
      )}
    </div>
  );
}