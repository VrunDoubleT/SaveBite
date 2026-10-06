import { useEffect, useState, useCallback } from "react";
import { useSearchParams } from "react-router-dom";
import {
  Ban,
  CheckCircle,
  Search,
  X,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  MoreHorizontal,
  AlertCircle,
} from "lucide-react";
import { userApi } from "@/features/admin/api/userApi";
import type { AdminUser } from "@/features/admin/types/user.types";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

const PAGE_SIZE = 5;

export function UserManagementPage() {
  const currentUser = useAuthStore((state) => state.user);

  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page")) || 1;
  const currentSearch = searchParams.get("search") || "";

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  const [searchInput, setSearchInput] = useState(currentSearch);

  const [actionModal, setActionModal] = useState<{
    isOpen: boolean;
    type: "suspend" | "reactivate";
    user: AdminUser | null;
  }>({ isOpen: false, type: "suspend", user: null });
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

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
      console.error(error);
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

  const handleAction = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!actionModal.user || !reason.trim()) return;
    setIsSubmitting(true);
    try {
      if (actionModal.type === "suspend") {
        await userApi.suspendAccount(actionModal.user.id, reason);
      } else {
        await userApi.reactivateAccount(actionModal.user.id, reason);
      }
      setActionModal({ isOpen: false, type: "suspend", user: null });
      setReason("");
      await fetchUsers();
    } catch (error) {
      alert(getApiErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-text-primary">User Management</h2>
          <p className="text-sm text-text-secondary">View and manage platform accounts.</p>
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

      <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm">
        <table className="min-w-full divide-y divide-border-default text-left text-sm">
          <thead className="bg-neutral-50 text-text-secondary">
            <tr>
              <th className="px-6 py-4 font-semibold">User Profile</th>
              <th className="px-6 py-4 font-semibold">Role</th>
              <th className="px-6 py-4 font-semibold">Status</th>
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
                  <span className="rounded-md bg-neutral-100 border border-neutral-200 px-2 py-1.5 text-xs font-semibold text-neutral-700 uppercase tracking-wider">
                    {u.role}
                  </span>
                </td>
                <td className="px-6 py-4">
                  <span
                    className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${u.status === "Active" ? "bg-primary-50 text-primary-700" : "bg-red-50 text-danger"}`}
                  >
                    <span
                      className={`size-1.5 rounded-full ${u.status === "Active" ? "bg-primary-500" : "bg-danger"}`}
                    ></span>
                    {u.status}
                  </span>
                </td>
                <td className="px-6 py-4 text-right">
                  {u.id !== currentUser?.id && u.role !== "Admin" ? (
                    <button
                      onClick={() =>
                        setActionModal({
                          isOpen: true,
                          type: u.status === "Active" ? "suspend" : "reactivate",
                          user: u,
                        })
                      }
                      className={`inline-flex items-center justify-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-semibold transition shadow-sm ${u.status === "Active" ? "border border-red-200 bg-white text-danger hover:bg-red-50" : "border border-primary-200 bg-white text-primary-700 hover:bg-primary-50"}`}
                    >
                      {u.status === "Active" ? (
                        <>
                          <Ban className="size-3.5" /> Suspend
                        </>
                      ) : (
                        <>
                          <CheckCircle className="size-3.5" /> Reactivate
                        </>
                      )}
                    </button>
                  ) : (
                    <span className="text-xs text-text-muted italic px-2">Protected</span>
                  )}
                </td>
              </tr>
            ))}
            {users.length === 0 && (
              <tr>
                <td colSpan={4} className="p-10 text-center text-text-muted">
                  No users found matching your search.
                </td>
              </tr>
            )}
          </tbody>
        </table>

        {totalItems > 0 && (
          <div className="flex flex-col items-center justify-center border-t border-border-default bg-neutral-50/30 py-5">
            <div className="flex items-center gap-1.5">
              <button
                disabled={page === 1}
                onClick={() => goToPage(1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
              >
                <ChevronsLeft className="size-4" />
              </button>
              <button
                disabled={page === 1}
                onClick={() => goToPage(page - 1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
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
                    className={`flex h-8 w-8 items-center justify-center rounded-md text-sm font-semibold transition-all shadow-sm ${
                      page === p
                        ? "bg-primary-600 text-white border border-primary-600"
                        : "bg-white border border-border-default text-text-secondary hover:bg-neutral-50"
                    }`}
                  >
                    {p}
                  </button>
                ),
              )}

              <button
                disabled={page >= totalPages}
                onClick={() => goToPage(page + 1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
              >
                <ChevronRight className="size-4" />
              </button>
              <button
                disabled={page >= totalPages}
                onClick={() => goToPage(totalPages)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
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

      {actionModal.isOpen && actionModal.user && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm transition-opacity">
          <div className="w-full max-w-md rounded-xl bg-bg-surface p-6 shadow-xl animate-in fade-in zoom-in-95 duration-200">
            <div
              className={`mx-auto flex size-12 items-center justify-center rounded-full mb-4 ${actionModal.type === "suspend" ? "bg-red-100" : "bg-primary-100"}`}
            >
              {actionModal.type === "suspend" ? (
                <AlertCircle className="size-6 text-danger" />
              ) : (
                <CheckCircle className="size-6 text-primary-600" />
              )}
            </div>
            <h3 className="text-xl font-bold text-center text-text-primary mb-1">
              {actionModal.type === "suspend" ? "Suspend Account" : "Reactivate Account"}
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
                  onClick={() => setActionModal({ isOpen: false, type: "suspend", user: null })}
                  className="rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary hover:bg-neutral-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting || !reason.trim()}
                  className={`rounded-md px-5 py-2.5 text-sm font-semibold text-white transition-colors disabled:opacity-60 shadow-sm ${actionModal.type === "suspend" ? "bg-danger hover:bg-red-700" : "bg-primary-600 hover:bg-primary-700"}`}
                >
                  {isSubmitting ? "Processing..." : "Confirm Action"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
