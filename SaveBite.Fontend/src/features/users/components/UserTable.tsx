import { Eye, Shield, ShoppingBag } from "lucide-react";
import type { AdminUser } from "@/features/users/types/user.types";
import { UserStatusBadge } from "./UserStatusBadge";

interface UserTableProps {
  users: AdminUser[];
  currentUserId?: string;
  onViewDetails: (id: string) => void;
  /** isCustomerProfile = false -> system account, true -> customer profile */
  onAction: (user: AdminUser, isCustomerProfile: boolean) => void;
}

export function UserTable({ users, currentUserId, onViewDetails, onAction }: UserTableProps) {
  return (
    <table className="min-w-full divide-y divide-border-default text-left text-sm">
      <thead className="bg-neutral-50 text-text-secondary">
        <tr>
          <th className="px-6 py-4 font-semibold">User Profile</th>
          <th className="px-6 py-4 font-semibold">Role</th>
          <th className="px-6 py-4 font-semibold">Account Status</th>
          <th className="px-6 py-4 font-semibold">Chức năng Customer</th>
          <th className="px-6 py-4 font-semibold text-right">Actions</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-border-default text-text-primary">
        {users.map((u) => {
          const isProtected = u.id === currentUserId || u.role === "Admin";

          return (
            <tr key={u.id} className="hover:bg-neutral-50/50 transition-colors">
              <td className="px-6 py-4">
                <div className="flex items-center gap-3">
                  <div className="relative flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full border border-neutral-200 bg-neutral-100 text-xs font-bold text-neutral-600">
                    {u.avatarUrl ? (
                      <img src={u.avatarUrl} alt={u.fullName} className="size-full object-cover" />
                    ) : (
                      <span>{(u.fullName || "U").charAt(0).toUpperCase()}</span>
                    )}
                  </div>
                  <div className="min-w-0">
                    <p className="font-semibold text-text-primary truncate">{u.fullName}</p>
                    <p className="text-xs text-text-muted mt-0.5 truncate">{u.email}</p>
                  </div>
                </div>
              </td>
              <td className="px-6 py-4">
                <span className="rounded-md bg-neutral-100 border border-neutral-200 px-2 py-1.5 text-[11px] font-bold text-neutral-700 uppercase tracking-wider">
                  {u.role}
                </span>
              </td>
              <td className="px-6 py-4">
                <UserStatusBadge status={u.status} />
              </td>
              <td className="px-6 py-4">
                <UserStatusBadge status={u.customerStatus} />
              </td>
              <td className="px-6 py-4 text-right">
                <div className="flex items-center justify-end gap-2">
                  <button
                    onClick={() => onViewDetails(u.id)}
                    title="View Details & Logs"
                    className="inline-flex items-center gap-1.5 rounded-md border border-neutral-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs hover:bg-neutral-50 transition"
                  >
                    <Eye className="size-3.5" />
                    <span>Details</span>
                  </button>
                  {!isProtected ? (
                    <>
                      <button
                        onClick={() => onAction(u, false)}
                        title={`${u.status === "Active" ? "Suspend" : "Reactivate"} System Account`}
                        className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs font-semibold shadow-2xs transition ${
                          u.status === "Active"
                            ? "border-amber-200 bg-amber-50 text-amber-800 hover:bg-amber-100"
                            : "border-emerald-200 bg-emerald-50 text-emerald-800 hover:bg-emerald-100"
                        }`}
                      >
                        <Shield className="size-3.5" />
                        <span>{u.status === "Active" ? "Suspend Account" : "Reactivate"}</span>
                      </button>
                      <button
                        onClick={() => onAction(u, true)}
                        title={`${u.customerStatus === "Active" ? "Cấm" : "Mở"} chức năng Customer`}
                        className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs font-semibold shadow-2xs transition ${
                          u.customerStatus === "Active"
                            ? "border-red-200 bg-red-50 text-red-800 hover:bg-red-100"
                            : "border-blue-200 bg-blue-50 text-blue-800 hover:bg-blue-100"
                        }`}
                      >
                        <ShoppingBag className="size-3.5" />
                        <span>{u.customerStatus === "Active" ? "Cấm chức năng Customer" : "Mở chức năng Customer"}</span>
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
          );
        })}
        {users.length === 0 && (
          <tr>
            <td colSpan={5} className="p-10 text-center text-text-muted">
              No users found matching your search.
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}
