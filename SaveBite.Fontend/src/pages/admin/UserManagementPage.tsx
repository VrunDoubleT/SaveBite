import { useState } from "react";
import type { AdminUser, UserDetailsResponse } from "@/features/users/types/user.types";
import { USER_PAGE_SIZE, useUserManagement } from "@/features/users/hooks/useUserManagement";
import {
  UserTable,
  UserActionModal,
  UserDetailsModal,
  type UserActionTarget,
} from "@/features/users/components";
import { useAuthStore } from "@/shared/stores/authStore";
import { Pagination } from "@/shared/components/pagination";
import { ErrorModal } from "@/shared/components/modal/ErrorModal";
import { SearchInput } from "@/shared/components/modal/SearchInput";

interface DetailsState {
  isOpen: boolean;
  isLoading: boolean;
  data: UserDetailsResponse | null;
}

const CLOSED_DETAILS: DetailsState = { isOpen: false, isLoading: false, data: null };

export function UserManagementPage() {
  const currentUser = useAuthStore((state) => state.user);

  const {
    users,
    totalItems,
    totalPages,
    page,
    searchInput,
    setSearchInput,
    errorMessage,
    clearError,
    goToPage,
    updateUserStatus,
    fetchUserDetails,
  } = useUserManagement();

  // UI state for the popups
  const [actionTarget, setActionTarget] = useState<UserActionTarget | null>(null);
  const [details, setDetails] = useState<DetailsState>(CLOSED_DETAILS);

  const openActionModal = (user: AdminUser, isCustomerProfile: boolean) => {
    const currentStatus = isCustomerProfile ? user.customerStatus : user.status;
    setActionTarget({ user, isCustomerProfile, isSuspended: currentStatus === "Active" });
  };

  const handleConfirmAction = async (reason: string) => {
    if (!actionTarget) return;

    const success = await updateUserStatus(actionTarget.user.id, {
      isSuspended: actionTarget.isSuspended,
      isCustomerProfile: actionTarget.isCustomerProfile,
      reason,
    });
    if (success) setActionTarget(null);
  };

  const openDetailsModal = async (id: string) => {
    setDetails({ isOpen: true, isLoading: true, data: null });
    const data = await fetchUserDetails(id);
    setDetails(data ? { isOpen: true, isLoading: false, data } : CLOSED_DETAILS);
  };

  return (
    <div className="space-y-6 relative">
      {/* Header & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-text-primary">User Management</h2>
          <p className="text-sm text-text-secondary">
            View and manage platform accounts and profiles.
          </p>
        </div>
        <SearchInput
          value={searchInput}
          onChange={setSearchInput}
          placeholder="Search email or name..."
          className="w-full sm:w-72"
        />
      </div>

      {/* Table + Pagination */}
      <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm">
        <UserTable
          users={users}
          currentUserId={currentUser?.id}
          onViewDetails={openDetailsModal}
          onAction={openActionModal}
        />

        {totalItems > 0 && (
          <div className="flex w-full items-center justify-around border-t border-border-default bg-neutral-50/30 px-6 py-5">
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              onPageChange={goToPage}
              totalItems={totalItems}
              pageSize={USER_PAGE_SIZE}
              itemLabel="users"
              showTotalItems={true}
              showFirstLast={true}
              className="w-full"
            />
          </div>
        )}
      </div>

      {/* Popups */}
      <UserActionModal
        target={actionTarget}
        onClose={() => setActionTarget(null)}
        onConfirm={handleConfirmAction}
      />

      <UserDetailsModal
        isOpen={details.isOpen}
        isLoading={details.isLoading}
        data={details.data}
        onClose={() => setDetails(CLOSED_DETAILS)}
      />

      {/* Keep last: all modals share z-50, so the later one in the DOM sits on top */}
      <ErrorModal message={errorMessage} onClose={clearError} />
    </div>
  );
}
