import { TableLoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeStaffEvents } from "@/features/shop-staffs/realtime/subscribeStaffEvents";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { Mail, Plus, Search, UserCheck, UserX, Users } from "lucide-react";

import { useNavigate } from "react-router-dom";

import { shopStaffApi } from "@/features/shop-staffs/api/shopStaffApi";

import type { ShopStaff, UpdateStaffInfoInput } from "@/features/shop-staffs/types/shopStaff.types";

import { InviteStaffModal } from "@/features/shop-staffs/components/InviteStaffModal";
import { StaffTable } from "@/features/shop-staffs/components/StaffTable";
import { StaffStatCard } from "@/features/shop-staffs/components/StaffStatCard";
import { EditStaffModal } from "@/features/shop-staffs/components/EditStaffModal";
import { RemoveStaffModal } from "@/features/shop-staffs/components/RemoveStaffModal";

import { APP_PATHS } from "@/app/router/paths";

import { getApiErrorMessage, getApiErrorStatus } from "@/shared/api/httpClient";

import { toast } from "@/shared/stores/toastStore";
import { Pagination, usePagination } from "@/shared/components/pagination";
import { useShopSuspended } from "@/shared/hooks/useShopSuspended";
import { useDebounce } from "@/shared/hooks/useDebounce";

export function StaffManagementPage() {
  const shopSuspended = useShopSuspended();
  const realtime = useRealtimeSubscriptions();
  const loadState = useRef({ version: 0 });
  const [staffs, setStaffs] = useState<ShopStaff[]>([]);
  const [loading, setLoading] = useState(false);

  const [keyword, setKeyword] = useState("");
  const debouncedKeyword = useDebounce(keyword, 350);

  const navigate = useNavigate();

  // Edit
  const [editingStaff, setEditingStaff] = useState<ShopStaff | null>(null);
  const [updating, setUpdating] = useState(false);

  // Remove
  const [removingStaff, setRemovingStaff] = useState<ShopStaff | null>(null);
  const [removing, setRemoving] = useState(false);

  // Invite
  const [inviting, setInviting] = useState(false);
  const [inviteModalOpen, setInviteModalOpen] = useState(false);

  // --------------------------------------------------
  // Load staff
  // --------------------------------------------------

  const loadStaff = useCallback(
    async (showLoading = true) => {
      const version = ++loadState.current.version;
      try {
        if (showLoading) setLoading(true);
        const result = await shopStaffApi.getStaffList(true);
        if (version === loadState.current.version) setStaffs(result);
      } catch (error) {
        if (version !== loadState.current.version) return;
        console.error(error);
        toast.error(getApiErrorMessage(error) || "Unable to load the staff list.");
      } finally {
        if (version === loadState.current.version) setLoading(false);
      }
    },
    [loadState],
  );

  useEffect(() => {
    const initial = window.setTimeout(() => void loadStaff(), 0);
    const off = subscribeStaffEvents(realtime, "staff", () => loadStaff(false));
    const currentLoadState = loadState.current;
    return () => {
      window.clearTimeout(initial);
      off();
      currentLoadState.version++;
    };
  }, [realtime, loadStaff, loadState]);

  // --------------------------------------------------
  // Update staff
  // --------------------------------------------------

  async function handleUpdateStaff(data: UpdateStaffInfoInput) {
    if (!editingStaff || shopSuspended) {
      return;
    }

    try {
      setUpdating(true);
      const response = await shopStaffApi.updateStaff(editingStaff.id, data);
      setEditingStaff(null);
      toast.success(response.data.message ?? "Staff information updated successfully.");
      await loadStaff();
    } catch (error) {
      console.error(error);
      toast.error(getApiErrorMessage(error) || "Unable to update staff information.");
    } finally {
      setUpdating(false);
    }
  }

  // --------------------------------------------------
  // Remove staff
  // --------------------------------------------------

  async function handleRemoveStaff() {
    if (!removingStaff || shopSuspended) {
      return;
    }

    try {
      setRemoving(true);
      const response = await shopStaffApi.removeStaff(removingStaff.id);
      setRemovingStaff(null);
      toast.success(response.data.message ?? "Staff member removed successfully.");
      await loadStaff();
    } catch (error) {
      console.error(error);
      toast.error(getApiErrorMessage(error) || "Unable to remove this staff member.");
    } finally {
      setRemoving(false);
    }
  }

  // --------------------------------------------------
  // Invite staff
  // --------------------------------------------------

  async function handleInviteStaff(userId: string) {
    if (shopSuspended) return;
    try {
      setInviting(true);
      const response = await shopStaffApi.inviteStaff({
        userId,
      });
      setInviteModalOpen(false);
      toast.success(response.data.message ?? "Staff invitation sent successfully.");
    } catch (error) {
      console.error(error);
      const apiMessage = getApiErrorMessage(error);
      const status = getApiErrorStatus(error);
      const hasSpecificConflictMessage = /(already|exist|invite|staff member|member of)/i.test(
        apiMessage,
      );

      toast.error(
        status === 409 && !hasSpecificConflictMessage
          ? "This user has already been invited or is already a staff member of this shop."
          : apiMessage || "Unable to send the staff invitation.",
      );
    } finally {
      setInviting(false);
    }
  }

  // --------------------------------------------------
  // Search with useDebounce
  // --------------------------------------------------

  const filteredStaffs = useMemo(() => {
    const q = debouncedKeyword.trim().toLowerCase();

    if (!q) {
      return staffs;
    }

    return staffs.filter(
      (staff) =>
        staff.displayName.toLowerCase().includes(q) ||
        Boolean(staff.staffNickname?.toLowerCase().includes(q)) ||
        staff.userEmail.toLowerCase().includes(q),
    );
  }, [staffs, debouncedKeyword]);

  // --------------------------------------------------
  // Pagination
  // --------------------------------------------------

  const {
    currentPage,
    setCurrentPage,
    totalPages,
    paginatedItems: paginatedStaffs,
    totalItems: totalStaffCount,
  } = usePagination({
    items: filteredStaffs,
    initialPageSize: 10,
  });

  // --------------------------------------------------
  // Statistics
  // --------------------------------------------------

  const activeCount = staffs.filter((staff) => staff.status === "Active").length;
  const suspendedCount = staffs.filter((staff) => staff.status === "Suspended").length;

  // --------------------------------------------------
  // Render
  // --------------------------------------------------

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Staff Management</h1>
          <p className="mt-1 text-sm text-neutral-500">Manage staff members of your shop.</p>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => navigate(`${APP_PATHS.STORE_OWNER}/staff/invitations`)}
            className="inline-flex items-center gap-2 rounded-lg border border-neutral-200 bg-white px-4 py-2.5 text-sm font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50"
          >
            <Mail size={18} />
            View Invitations
          </button>

          <button
            type="button"
            disabled={inviting || shopSuspended}
            title={
              shopSuspended
                ? "Inviting staff is unavailable while your shop is suspended."
                : undefined
            }
            onClick={() => setInviteModalOpen(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Plus size={18} />
            Invite Staff
          </button>
        </div>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 sm:gap-4">
        <StaffStatCard
          label="Total staff"
          value={staffs.length}
          icon={<Users size={20} />}
          accent="bg-neutral-100 text-neutral-600"
        />

        <StaffStatCard
          label="Active"
          value={activeCount}
          icon={<UserCheck size={20} />}
          accent="bg-emerald-50 text-emerald-600"
        />

        <StaffStatCard
          label="Suspended"
          value={suspendedCount}
          icon={<UserX size={20} />}
          accent="bg-rose-50 text-rose-600"
        />
      </div>

      {/* Search Input */}
      <div className="relative max-w-sm">
        <Search
          size={16}
          className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-neutral-400"
        />

        <input
          value={keyword}
          onChange={(event) => {
            setKeyword(event.target.value);
            setCurrentPage(1);
          }}
          placeholder="Search by name or email"
          className="h-10 w-full rounded-lg border border-neutral-200 bg-white pl-9 pr-4 text-sm text-neutral-900 placeholder:text-neutral-400 outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
        />
      </div>

      {/* Table */}
      {loading ? (
        <TableLoadingSkeleton
          tableClassName="min-w-[720px]"
          cellClassName="px-6 py-4"
          headers={["Staff", "Role", "Status", "Joined date", "Actions"]}
          label="Loading staff records"
        />
      ) : (
        <div className="space-y-4">
          <StaffTable
            staffs={paginatedStaffs}
            actionsDisabled={shopSuspended}
            onEdit={(staff) => {
              setEditingStaff(staff);
            }}
            onView={(staff) => {
              navigate(`${APP_PATHS.STORE_OWNER}/staff/${staff.id}`, {
                state: {
                  staff,
                },
              });
            }}
            onRemove={(staff) => {
              setRemovingStaff(staff);
            }}
          />

          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalStaffCount}
              pageSize={10}
              itemLabel="staff members"
              onPageChange={setCurrentPage}
            />
          )}
        </div>
      )}

      {/* Edit modal */}
      {editingStaff && !shopSuspended && (
        <EditStaffModal
          staff={editingStaff}
          loading={updating}
          onClose={() => {
            if (!updating) {
              setEditingStaff(null);
            }
          }}
          onSubmit={handleUpdateStaff}
        />
      )}

      {/* Invite modal */}
      {inviteModalOpen && !shopSuspended && (
        <InviteStaffModal
          loading={inviting}
          onClose={() => {
            if (!inviting) {
              setInviteModalOpen(false);
            }
          }}
          onSubmit={handleInviteStaff}
        />
      )}

      {/* Remove modal */}
      {removingStaff && !shopSuspended && (
        <RemoveStaffModal
          staff={removingStaff}
          loading={removing}
          onClose={() => {
            if (!removing) {
              setRemovingStaff(null);
            }
          }}
          onConfirm={handleRemoveStaff}
        />
      )}
    </div>
  );
}
