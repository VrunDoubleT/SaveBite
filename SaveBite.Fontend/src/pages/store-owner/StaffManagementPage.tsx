import { useEffect, useMemo, useState } from "react";
import {
  Mail,
  Plus,
  Search,
  UserCheck,
  UserX,
  Users,
} from "lucide-react";
import { useNavigate } from "react-router-dom";

import { shopStaffApi } from "@/features/shopStaff/api/shopStaffApi";
import type {
  ShopStaff,
  UpdateStaffInfoInput,
} from "@/features/shopStaff/types/shopStaff.types";
import { InviteStaffModal } from "@/features/shopStaff/components/InviteStaffModal";

import { StaffTable } from "@/features/shopStaff/components/StaffTable";
import { StaffStatCard } from "@/features/shopStaff/components/StaffStatCard";
import { EditStaffModal } from "@/features/shopStaff/components/EditStaffModal";
import { RemoveStaffModal } from "@/features/shopStaff/components/RemoveStaffModal";


import { shopApi } from "@/features/shop/api/shopApi";
import { APP_PATHS } from "@/app/router/paths";
import {
  getApiErrorMessage,
  getApiErrorStatus,
} from "@/shared/api/httpClient";

import { toast } from "@/shared/stores/toastStore";

export function StaffManagementPage() {
  const [staffs, setStaffs] = useState<ShopStaff[]>([]);
  const [loading, setLoading] = useState(false);
  const [keyword, setKeyword] = useState("");

  const [shopId, setShopId] = useState<string | null>(null);

  const navigate = useNavigate();

  // Edit
  const [editingStaff, setEditingStaff] =
    useState<ShopStaff | null>(null);

  const [updating, setUpdating] = useState(false);

  // Remove
  const [removingStaff, setRemovingStaff] =
    useState<ShopStaff | null>(null);

  const [removing, setRemoving] = useState(false);
  const [inviting, setInviting] = useState(false);

const [inviteModalOpen, setInviteModalOpen] = useState(false);
  // --------------------------------------------------
  // Load shop
  // --------------------------------------------------

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

  useEffect(() => {
    loadShop();
  }, []);

  // --------------------------------------------------
  // Load staff
  // --------------------------------------------------

  useEffect(() => {
    if (!shopId) return;

    loadStaff(shopId);
  }, [shopId]);

  async function loadStaff(
  shopId: string,
) {
  try {
    setLoading(true);

    const response =
      await shopStaffApi.getStaffList(
        shopId,
      );

    setStaffs(response.data.data);
  } catch (error) {
    console.error(error);

    toast.error(
      getApiErrorMessage(error) ||
        "Unable to load the staff list.",
    );
  } finally {
    setLoading(false);
  }
}

  // --------------------------------------------------
  // Update staff
  // --------------------------------------------------

  async function handleUpdateStaff(
  data: UpdateStaffInfoInput,
) {
  if (!shopId || !editingStaff) {
    return;
  }

  try {
    setUpdating(true);

    const response =
      await shopStaffApi.updateStaff(
        shopId,
        editingStaff.id,
        data,
      );

    setEditingStaff(null);

    toast.success(
      response.data.message ??
        "Staff information updated successfully.",
    );

    await loadStaff(shopId);
  } catch (error) {
    console.error(error);

    toast.error(
      getApiErrorMessage(error) ||
        "Unable to update staff information.",
    );
  } finally {
    setUpdating(false);
  }
}

  // --------------------------------------------------
  // Remove staff
  // --------------------------------------------------

 async function handleRemoveStaff() {
  if (!shopId || !removingStaff) {
    return;
  }

  try {
    setRemoving(true);

    const response =
      await shopStaffApi.removeStaff(
        shopId,
        removingStaff.id,
      );

    setRemovingStaff(null);

    toast.success(
      response.data.message ??
        "Staff member removed successfully.",
    );

    await loadStaff(shopId);
  } catch (error) {
    console.error(error);

    toast.error(
      getApiErrorMessage(error) ||
        "Unable to remove this staff member.",
    );
  } finally {
    setRemoving(false);
  }
}

async function handleInviteStaff(
  email: string,
) {
  if (!shopId) return;

  try {
    setInviting(true);

    const response =
      await shopStaffApi.inviteStaff(
        shopId,
        {
          invitedUserEmail: email,
        },
      );

    setInviteModalOpen(false);

    toast.success(
      response.data.message ??
        `Invitation sent successfully to ${email}.`,
    );
  } catch (error) {
    console.error(error);

    const apiMessage =
      getApiErrorMessage(error);

    const status =
      getApiErrorStatus(error);

    const hasSpecificConflictMessage =
      /(already|exist|invite|staff member|member of)/i.test(
        apiMessage,
      );

    toast.error(
      status === 409 &&
        !hasSpecificConflictMessage
        ? "This email has already been invited or is already a staff member of this shop."
        : apiMessage ||
            "Unable to send the staff invitation.",
    );
  } finally {
    setInviting(false);
  }
}

  // --------------------------------------------------
  // Search
  // --------------------------------------------------

  const filteredStaffs = useMemo(() => {
    const q = keyword.trim().toLowerCase();

    if (!q) return staffs;

    return staffs.filter(
      (staff) =>
        staff.userName
          .toLowerCase()
          .includes(q) ||
        staff.userEmail
          .toLowerCase()
          .includes(q),
    );
  }, [staffs, keyword]);

  // --------------------------------------------------
  // Statistics
  // --------------------------------------------------

  const activeCount = staffs.filter(
    (staff) => staff.status === "Active",
  ).length;

  const suspendedCount = staffs.filter(
    (staff) => staff.status === "Suspended",
  ).length;

  // --------------------------------------------------
  // Render
  // --------------------------------------------------

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Staff Management
          </h1>

          <p className="mt-1 text-sm text-gray-500">
            Manage staff members of your shop.
          </p>
        </div>
<div className="flex items-center gap-3">
  <button
    type="button"
    onClick={() =>
      navigate(
        `${APP_PATHS.STORE_OWNER}/staff/invitations`,
      )
    }
    className="inline-flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 shadow-sm transition hover:bg-gray-50"
  >
    <Mail size={18} />
    View Invitations
  </button>

  <button
    type="button"
    disabled={!shopId}
    onClick={() => setInviteModalOpen(true)}
    className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50"
  >
    <Plus size={18} />
    Invite Staff
  </button>
</div>
      </div>

      {/* Summary */}
      <div className="grid gap-4 sm:grid-cols-3">
        <StaffStatCard
          label="Total staff"
          value={staffs.length}
          icon={<Users size={20} />}
          accent="bg-blue-100 text-blue-600"
        />

        <StaffStatCard
          label="Active"
          value={activeCount}
          icon={<UserCheck size={20} />}
          accent="bg-emerald-100 text-emerald-600"
        />

        <StaffStatCard
          label="Suspended"
          value={suspendedCount}
          icon={<UserX size={20} />}
          accent="bg-amber-100 text-amber-600"
        />
      </div>

      {/* Search */}
      <div className="relative max-w-sm">
        <Search
          size={18}
          className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
        />

        <input
          value={keyword}
          onChange={(event) =>
            setKeyword(event.target.value)
          }
          placeholder="Search by name or email"
          className="w-full rounded-xl border border-gray-200 bg-white py-2.5 pl-10 pr-4 text-sm outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
        />
      </div>

      {/* Table */}
      {loading ? (
        <div className="space-y-3 rounded-2xl border border-gray-200 bg-white p-6">
          {[1, 2, 3].map((item) => (
            <div
              key={item}
              className="h-12 animate-pulse rounded-lg bg-gray-100"
            />
          ))}
        </div>
      ) : (
        <StaffTable
          staffs={filteredStaffs}

          onEdit={(staff) => {
            setEditingStaff(staff);
          }}

          onView={(staff) => {
            navigate(
              `${APP_PATHS.STORE_OWNER}/staff/${staff.id}`,
            );
          }}

          onRemove={(staff) => {
            setRemovingStaff(staff);
          }}
        />
      )}

      {/* Edit modal */}
      {editingStaff && (
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
      {inviteModalOpen && (
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
      {removingStaff && (
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