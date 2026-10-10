import { useEffect, useState, useCallback } from "react";
import { useSearchParams } from "react-router-dom";
import {
  CheckCircle,
  Search,
  X,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  MoreHorizontal,
  AlertCircle,
  Shield,
  ShoppingBag,
  Eye,
  Clock,
  CalendarDays,
  User as UserIcon,
  Repeat,
} from "lucide-react";
import { userApi } from "@/features/admin/api/userApi";
import type { AdminUser, UserDetailsResponse } from "@/features/admin/types/user.types";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

const PAGE_SIZE = 5;

const renderChangedValues = (jsonString: string) => {
  if (!jsonString) return null;
  try {
    const parsed = JSON.parse(jsonString);
    const keys = Object.keys(parsed);
    if (keys.length === 0) return null;

    return (
      <div className="mt-3 rounded-md bg-neutral-50 p-2.5 border border-neutral-100">
        <p className="text-[11px] font-bold text-neutral-400 uppercase tracking-wider mb-1.5">
          Changes
        </p>
        <div className="space-y-1.5">
          {keys.map((key) => (
            <div key={key} className="flex justify-between items-center text-xs">
              <span className="text-text-secondary capitalize">
                {key.replace(/([A-Z])/g, " $1").trim()}
              </span>
              <span className="font-semibold text-text-primary px-2 py-0.5 bg-white rounded shadow-sm border border-neutral-100">
                {String(parsed[key])}
              </span>
            </div>
          ))}
        </div>
      </div>
    );
  } catch {
    return (
      <div className="mt-3 text-[11px] bg-neutral-50 text-neutral-600 p-2 rounded border border-neutral-100 font-mono break-all">
        {jsonString}
      </div>
    );
  }
};

export function UserManagementPage() {
  const currentUser = useAuthStore((state) => state.user);
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page")) || 1;
  const currentSearch = searchParams.get("search") || "";

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [searchInput, setSearchInput] = useState(currentSearch);

  const [errorPopup, setErrorPopup] = useState<string | null>(null);

  const [actionModal, setActionModal] = useState<{
    isOpen: boolean;
    isCustomerProfile: boolean;
    isSuspended: boolean;
    user: AdminUser | null;
  }>({ isOpen: false, isCustomerProfile: false, isSuspended: false, user: null });
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [detailsModal, setDetailsModal] = useState<{
    isOpen: boolean;
    isLoading: boolean;
    activeTab: "audit" | "role"; // Thêm state quản lý Tab
    data: UserDetailsResponse | null;
  }>({ isOpen: false, isLoading: false, activeTab: "audit", data: null });

  useEffect(() => {
    const handler = setTimeout(() => {
      if (searchInput !== currentSearch) {
        setSearchParams(
          (prev) => {
            if (searchInput) prev.set("search", searchInput);
            else prev.delete("search");
            prev.set("page", "1");
            return prev;
          },
          { replace: true },
        );
      }
    }, 500);
    return () => clearTimeout(handler);
  }, [searchInput, currentSearch, setSearchParams]);

  const fetchUsers = useCallback(async () => {
    try {
      const res = await userApi.getUsers({
        page,
        pageSize: PAGE_SIZE,
        search: currentSearch || undefined,
      });
      setUsers(res.items);
      setTotalItems(res.total ?? 0);
      setTotalPages(res.totalPages ?? 1);
    } catch (error) {
      setErrorPopup(getApiErrorMessage(error));
    }
  }, [page, currentSearch]);

  useEffect(() => {
    const loadData = async () => await fetchUsers();
    void loadData();
  }, [fetchUsers]);

  const goToPage = (newPage: number) => {
    setSearchParams((prev) => {
      prev.set("page", newPage.toString());
      return prev;
    });
  };

  const getPageNumbers = () => {
    if (totalPages <= 4) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }
    if (page <= 2) {
      return [1, 2, 3, "...", totalPages];
    }
    if (page >= totalPages - 1) {
      return [1, "...", totalPages - 2, totalPages - 1, totalPages];
    }
    return [1, "...", page - 1, page, page + 1, "...", totalPages];
  };

  const openActionModal = (user: AdminUser, isCustomerProfile: boolean) => {
    const currentStatus = isCustomerProfile ? user.customerStatus : user.status;
    const isSuspended = currentStatus === "Active";
    setActionModal({ isOpen: true, isCustomerProfile, isSuspended, user });
  };

  const handleAction = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!actionModal.user || !reason.trim()) return;

    setIsSubmitting(true);
    try {
      await userApi.updateStatus(actionModal.user.id, {
        isSuspended: actionModal.isSuspended,
        isCustomerProfile: actionModal.isCustomerProfile,
        reason: reason.trim(),
      });

      setActionModal({ isOpen: false, isCustomerProfile: false, isSuspended: false, user: null });
      setReason("");
      await fetchUsers();
    } catch (error) {
      setErrorPopup(getApiErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  const openDetailsModal = async (id: string) => {
    setDetailsModal({ isOpen: true, isLoading: true, activeTab: "audit", data: null });
    try {
      const data = await userApi.getUserDetails(id);
      setDetailsModal({ isOpen: true, isLoading: false, activeTab: "audit", data });
    } catch (error) {
      setDetailsModal({ isOpen: false, isLoading: false, activeTab: "audit", data: null });
      setErrorPopup(getApiErrorMessage(error));
    }
  };

  const StatusBadge = ({ status, label }: { status: string; label?: string }) => (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold tracking-wide ${status === "Active" ? "bg-primary-50 text-primary-700 border border-primary-100" : "bg-red-50 text-danger border border-red-100"}`}
    >
      <span
        className={`size-1.5 rounded-full ${status === "Active" ? "bg-primary-500" : "bg-danger"}`}
      ></span>
      {label ? `${label}: ` : ""}
      {status}
    </span>
  );

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
        <div className="relative">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-neutral-400" />
          <input
            type="text"
            placeholder="Search email or name..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="w-full sm:w-72 rounded-md border border-border-default py-2 pl-9 pr-9 text-sm focus:border-border-focus focus:outline-none transition-colors shadow-sm"
          />
          {searchInput && (
            <button
              onClick={() => setSearchInput("")}
              className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-neutral-400 hover:text-neutral-700 transition-colors"
            >
              <X className="size-3.5" />
            </button>
          )}
        </div>
      </div>

      {/* Main Table */}
      <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm">
        <table className="min-w-full divide-y divide-border-default text-left text-sm">
          <thead className="bg-neutral-50 text-text-secondary">
            <tr>
              <th className="px-6 py-4 font-semibold">User Profile</th>
              <th className="px-6 py-4 font-semibold">Role</th>
              <th className="px-6 py-4 font-semibold">Account Status</th>
              <th className="px-6 py-4 font-semibold">Customer Status</th>
              <th className="px-6 py-4 font-semibold text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-default text-text-primary">
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-neutral-50/50 transition-colors">
                <td className="px-6 py-4">
                  <p className="font-semibold text-text-primary">{u.fullName}</p>
                  <p className="text-xs text-text-muted mt-0.5">{u.email}</p>
                </td>
                <td className="px-6 py-4">
                  <span className="rounded-md bg-neutral-100 border border-neutral-200 px-2 py-1.5 text-[11px] font-bold text-neutral-700 uppercase tracking-wider">
                    {u.role}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <StatusBadge status={u.status} />
                </td>
                <td className="px-6 py-4">
                  <StatusBadge status={u.customerStatus} />
                </td>
                <td className="px-6 py-4 text-right">
                  <div className="flex justify-end gap-2">
                    <button
                      onClick={() => openDetailsModal(u.id)}
                      title="View Details & Logs"
                      className="p-1.5 text-neutral-500 hover:text-primary-600 hover:bg-primary-50 rounded transition"
                    >
                      <Eye className="size-4.5" />
                    </button>

                    {u.id !== currentUser?.id && u.role !== "Admin" ? (
                      <>
                        <button
                          onClick={() => openActionModal(u, false)}
                          title={`${u.status === "Active" ? "Suspend" : "Reactivate"} System Account`}
                          className={`p-1.5 rounded transition ${u.status === "Active" ? "text-neutral-500 hover:text-danger hover:bg-red-50" : "text-danger hover:text-primary-600 hover:bg-primary-50"}`}
                        >
                          <Shield className="size-4.5" />
                        </button>
                        <button
                          onClick={() => openActionModal(u, true)}
                          title={`${u.customerStatus === "Active" ? "Suspend" : "Reactivate"} Customer Profile`}
                          className={`p-1.5 rounded transition ${u.customerStatus === "Active" ? "text-neutral-500 hover:text-danger hover:bg-red-50" : "text-danger hover:text-primary-600 hover:bg-primary-50"}`}
                        >
                          <ShoppingBag className="size-4.5" />
                        </button>
                      </>
                    ) : (
                      <span className="text-xs text-text-muted italic px-2 flex items-center">
                        Protected
                      </span>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {users.length === 0 && (
              <tr>
                <td colSpan={5} className="p-10 text-center text-text-muted">
                  No users found matching your search.
                </td>
              </tr>
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {totalItems > 0 && (
          <div className="flex flex-col items-center justify-center border-t border-border-default bg-neutral-50/30 py-5">
            <div className="flex items-center gap-1.5">
              <button
                disabled={page === 1}
                onClick={() => goToPage(1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 shadow-sm"
              >
                <ChevronsLeft className="size-4" />
              </button>
              <button
                disabled={page === 1}
                onClick={() => goToPage(page - 1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 shadow-sm"
              >
                <ChevronLeft className="size-4" />
              </button>
              {getPageNumbers().map((p, index) =>
                p === "..." ? (
                  <span
                    key={`ellipsis-${index}`}
                    className="flex h-8 w-8 items-center justify-center text-text-muted"
                  >
                    <MoreHorizontal className="size-4" />
                  </span>
                ) : (
                  <button
                    key={p}
                    onClick={() => goToPage(p as number)}
                    className={`flex h-8 w-8 items-center justify-center rounded-md text-sm font-semibold shadow-sm ${page === p ? "bg-primary-600 text-white" : "bg-white border border-border-default text-text-secondary hover:bg-neutral-50"}`}
                  >
                    {p}
                  </button>
                ),
              )}
              <button
                disabled={page >= totalPages}
                onClick={() => goToPage(page + 1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 shadow-sm"
              >
                <ChevronRight className="size-4" />
              </button>
              <button
                disabled={page >= totalPages}
                onClick={() => goToPage(totalPages)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 shadow-sm"
              >
                <ChevronsRight className="size-4" />
              </button>
            </div>
            <p className="mt-3 text-xs font-medium text-text-muted opacity-80">
              Showing <span className="text-text-secondary">{(page - 1) * PAGE_SIZE + 1}</span> to{" "}
              <span className="text-text-secondary">{Math.min(page * PAGE_SIZE, totalItems)}</span>{" "}
              of <span className="text-text-secondary">{totalItems}</span> users
            </p>
          </div>
        )}
      </div>

      {/* Global Error Popup */}
      {errorPopup && (
        <div className="fixed inset-0 z-60 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm">
          <div className="w-full max-w-sm rounded-xl bg-bg-surface p-6 shadow-xl animate-in zoom-in-95 duration-200 text-center">
            <AlertCircle className="mx-auto size-10 text-danger mb-4" />
            <h3 className="text-lg font-bold text-text-primary mb-2">Error Occurred</h3>
            <p className="text-sm text-text-secondary mb-6">{errorPopup}</p>
            <button
              onClick={() => setErrorPopup(null)}
              className="w-full rounded-md bg-neutral-100 text-neutral-800 px-4 py-2.5 font-semibold hover:bg-neutral-200 transition"
            >
              Dismiss
            </button>
          </div>
        </div>
      )}

      {/* Action Modal */}
      {actionModal.isOpen && actionModal.user && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm transition-opacity">
          <div className="w-full max-w-md rounded-xl bg-bg-surface p-6 shadow-xl animate-in fade-in zoom-in-95 duration-200">
            <div
              className={`mx-auto flex size-12 items-center justify-center rounded-full mb-4 ${actionModal.isSuspended ? "bg-red-100" : "bg-primary-100"}`}
            >
              {actionModal.isSuspended ? (
                <AlertCircle className="size-6 text-danger" />
              ) : (
                <CheckCircle className="size-6 text-primary-600" />
              )}
            </div>
            <h3 className="text-xl font-bold text-center text-text-primary mb-1">
              {actionModal.isSuspended ? "Suspend" : "Reactivate"}{" "}
              {actionModal.isCustomerProfile ? "Customer Profile" : "Account"}
            </h3>
            <p className="mb-6 text-sm text-center text-text-secondary">
              Target user:{" "}
              <span className="font-semibold text-text-primary">{actionModal.user.email}</span>
            </p>
            <form onSubmit={handleAction} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-text-primary mb-1.5">
                  Reason for action
                </label>
                <textarea
                  required
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  className="w-full rounded-md border border-border-default px-3 py-2.5 text-sm focus:border-border-focus focus:ring-1 focus:ring-border-focus focus:outline-none transition-all"
                  rows={3}
                  placeholder="Please provide a valid reason..."
                />
              </div>
              <div className="mt-6 flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() =>
                    setActionModal({
                      isOpen: false,
                      isCustomerProfile: false,
                      isSuspended: false,
                      user: null,
                    })
                  }
                  className="rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary hover:bg-neutral-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting || !reason.trim()}
                  className={`rounded-md px-5 py-2.5 text-sm font-semibold text-white transition-colors disabled:opacity-60 shadow-sm ${actionModal.isSuspended ? "bg-danger hover:bg-red-700" : "bg-primary-600 hover:bg-primary-700"}`}
                >
                  {isSubmitting ? "Processing..." : "Confirm Action"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Details Modal */}
      {detailsModal.isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm transition-opacity">
          <div className="w-full max-w-2xl max-h-[85vh] flex flex-col rounded-xl bg-bg-surface shadow-xl animate-in zoom-in-95 duration-200">
            {/* Header Modal */}
            <div className="flex justify-between items-center p-5 border-b border-border-default">
              <h3 className="text-lg font-bold text-text-primary">User Profile & Activity</h3>
              <button
                onClick={() =>
                  setDetailsModal({
                    isOpen: false,
                    isLoading: false,
                    activeTab: "audit",
                    data: null,
                  })
                }
                className="text-neutral-400 hover:text-neutral-700 p-1 rounded-md transition-colors"
              >
                <X className="size-5" />
              </button>
            </div>

            <div className="p-6 overflow-y-auto flex-1 bg-neutral-50/30">
              {detailsModal.isLoading ? (
                <div className="flex justify-center items-center py-20 text-text-muted">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
                </div>
              ) : detailsModal.data ? (
                <div className="space-y-6">
                  {/* Giao diện Profile Header */}
                  <div className="flex flex-col sm:flex-row gap-5 items-start bg-white p-5 rounded-xl border border-border-default shadow-sm">
                    <div className="flex size-16 shrink-0 items-center justify-center rounded-full bg-primary-50 text-2xl font-black text-primary-600 border border-primary-100 shadow-sm">
                      {detailsModal.data.fullName.charAt(0).toUpperCase()}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-2">
                        <div className="truncate">
                          <h4 className="text-xl font-bold text-text-primary truncate">
                            {detailsModal.data.fullName}
                          </h4>
                          <p className="text-sm text-text-secondary truncate">
                            {detailsModal.data.email}
                          </p>
                        </div>
                        <span className="inline-flex rounded-md bg-neutral-100 border border-neutral-200 px-2.5 py-1 text-[11px] font-bold text-neutral-700 uppercase tracking-widest shrink-0">
                          {detailsModal.data.role}
                        </span>
                      </div>

                      <div className="mt-4 flex flex-wrap gap-2 items-center">
                        <StatusBadge status={detailsModal.data.status} label="Account" />
                        <StatusBadge status={detailsModal.data.customerStatus} label="Customer" />
                        <div className="ml-auto flex items-center gap-1.5 text-xs font-medium text-text-muted">
                          <CalendarDays className="size-3.5" />
                          Joined {new Date(detailsModal.data.createdAt).toLocaleDateString()}
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Tabs Section */}
                  <div>
                    <div className="flex gap-4 border-b border-border-default mb-5">
                      <button
                        onClick={() => setDetailsModal((prev) => ({ ...prev, activeTab: "audit" }))}
                        className={`flex items-center gap-2 pb-3 text-sm font-semibold transition-colors border-b-2 ${
                          detailsModal.activeTab === "audit"
                            ? "border-primary-600 text-primary-600"
                            : "border-transparent text-text-secondary hover:text-text-primary"
                        }`}
                      >
                        <Clock className="size-4" />
                        Audit Logs
                      </button>
                      <button
                        onClick={() => setDetailsModal((prev) => ({ ...prev, activeTab: "role" }))}
                        className={`flex items-center gap-2 pb-3 text-sm font-semibold transition-colors border-b-2 ${
                          detailsModal.activeTab === "role"
                            ? "border-primary-600 text-primary-600"
                            : "border-transparent text-text-secondary hover:text-text-primary"
                        }`}
                      >
                        <Repeat className="size-4" />
                        Role Changes
                      </button>
                    </div>

                    {/* Nội dung Tab Audit Logs */}
                    {detailsModal.activeTab === "audit" && (
                      <div>
                        {detailsModal.data.logs.length === 0 ? (
                          <div className="text-center py-8 bg-white rounded-xl border border-dashed border-border-default">
                            <UserIcon className="mx-auto size-8 text-neutral-300 mb-2" />
                            <p className="text-sm text-text-muted font-medium">
                              No activity history recorded.
                            </p>
                          </div>
                        ) : (
                          <div className="relative pl-3 sm:pl-4 border-l-2 border-neutral-200 space-y-6 ml-2">
                            {detailsModal.data.logs.map((log, idx) => (
                              <div key={idx} className="relative">
                                <div className="absolute -left-[21px] sm:-left-[25px] top-1.5 size-3.5 rounded-full bg-white border-[3px] border-primary-500 shadow-sm" />
                                <div className="bg-white border border-border-default rounded-lg p-4 shadow-sm ml-2 hover:border-primary-200 transition-colors">
                                  <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-2 gap-2">
                                    <h5 className="font-bold text-sm text-text-primary bg-neutral-100 px-2.5 py-1 rounded-md border border-neutral-200">
                                      {log.action}
                                    </h5>
                                    <time className="text-xs text-text-muted font-medium shrink-0 flex items-center gap-1">
                                      <Clock className="size-3" />
                                      {new Date(log.createdAt).toLocaleString()}
                                    </time>
                                  </div>
                                  <p className="text-sm text-text-secondary leading-relaxed mt-3">
                                    <span className="font-semibold text-text-primary">
                                      Reason:{" "}
                                    </span>
                                    "{log.reason}"
                                  </p>
                                  {renderChangedValues(log.changedValues)}
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    )}

                    {/* Nội dung Tab Role Changes */}
                    {detailsModal.activeTab === "role" && (
                      <div>
                        {detailsModal.data.roleLogs.length === 0 ? (
                          <div className="text-center py-8 bg-white rounded-xl border border-dashed border-border-default">
                            <Shield className="mx-auto size-8 text-neutral-300 mb-2" />
                            <p className="text-sm text-text-muted font-medium">
                              No role changes recorded.
                            </p>
                          </div>
                        ) : (
                          <div className="relative pl-3 sm:pl-4 border-l-2 border-neutral-200 space-y-6 ml-2">
                            {detailsModal.data.roleLogs.map((log, idx) => (
                              <div key={idx} className="relative">
                                <div className="absolute -left-[21px] sm:-left-[25px] top-1.5 size-3.5 rounded-full bg-white border-[3px] border-primary-500 shadow-sm" />
                                <div className="bg-white border border-border-default rounded-lg p-4 shadow-sm ml-2 hover:border-primary-200 transition-colors">
                                  <div className="flex justify-between items-center mb-3">
                                    <h5 className="font-bold text-sm text-text-primary flex items-center gap-2">
                                      <span className="bg-neutral-100 text-neutral-600 px-2 py-0.5 rounded border border-neutral-200 uppercase text-xs">
                                        {log.oldRole}
                                      </span>
                                      <ChevronRight className="size-4 text-neutral-400" />
                                      <span className="bg-primary-50 text-primary-700 px-2 py-0.5 rounded border border-primary-100 uppercase text-xs">
                                        {log.newRole}
                                      </span>
                                    </h5>
                                    <time className="text-xs text-text-muted font-medium flex items-center gap-1">
                                      <Clock className="size-3" />
                                      {new Date(log.createdAt).toLocaleString()}
                                    </time>
                                  </div>
                                  <p className="text-sm text-text-secondary leading-relaxed">
                                    <span className="font-semibold text-text-primary">
                                      Reason:{" "}
                                    </span>
                                    "{log.reason}"
                                  </p>
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              ) : null}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
