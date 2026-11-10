import { useEffect, useState } from "react";
import { Users, UserCheck, UserX, Store, User } from "lucide-react";
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
import { platformAnalyticsApi } from "@/features/admin-analytics/api/platformAnalyticsApi";

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

  // Summary counts state
  const [counts, setCounts] = useState<{
    total: number;
    active: number;
    suspended: number;
    storeOwners: number;
    customers: number;
  }>({
    total: 0,
    active: 0,
    suspended: 0,
    storeOwners: 0,
    customers: 0,
  });
  const [loadingStats, setLoadingStats] = useState(true);
  const [statsReloadKey, setStatsReloadKey] = useState(0);

  useEffect(() => {
    let cancelled = false;

    platformAnalyticsApi
      .getUsers()
      .then((data) => {
        if (cancelled || !data) return;
        const storeOwners =
          data.roleBreakdown?.find((r) => r.label.toLowerCase() === "storeowner")?.count ?? 0;
        const customers =
          data.roleBreakdown?.find((r) => r.label.toLowerCase() === "customer")?.count ?? 0;
        setCounts({
          total: data.totalUsers ?? 0,
          active: data.activeAccounts ?? 0,
          suspended: data.suspendedAccounts ?? 0,
          storeOwners,
          customers,
        });
        setLoadingStats(false);
      })
      .catch(() => {
        if (!cancelled) {
          setCounts((prev) => ({ ...prev, total: totalItems }));
          setLoadingStats(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [totalItems, statsReloadKey]);

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
    if (success) {
      setActionTarget(null);
      setLoadingStats(true);
      setStatsReloadKey((k) => k + 1);
    }
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
          <h2 className="text-2xl font-bold tracking-tight text-neutral-900">User Management</h2>
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

      {/* 5 Overview Stat Cards with icons (no hover, non-clickable) */}
      <div
        className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5 sm:gap-4"
        role="region"
        aria-label="User summary metrics"
      >
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <Users className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">All Users</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.total}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <UserCheck className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Active</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.active}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-rose-50 text-rose-600">
              <UserX className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Suspended</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.suspended}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
              <Store className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Store Owners</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.storeOwners}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-amber-50 text-amber-600">
              <User className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Customers</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.customers}
              </p>
            </div>
          </div>
        </div>
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