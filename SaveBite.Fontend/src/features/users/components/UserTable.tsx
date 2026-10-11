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
          <th className="px-6 py-4 font-semibold">Customer Status</th>
          <th className="px-6 py-4 font-semibold text-right">Actions</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-border-default text-text-primary">
        {users.map((u) => {
          const isProtected = u.id === currentUserId || u.role === "Admin";

          return (
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
                <UserStatusBadge status={u.status} />
              </td>
              <td className="px-6 py-4">
                <UserStatusBadge status={u.customerStatus} />
              </td>
              <td className="px-6 py-4 text-right">
                <div className="flex justify-end gap-2">
                  <button
                    onClick={() => onViewDetails(u.id)}
                    title="View Details & Logs"
                    className="p-1.5 text-neutral-500 hover:text-primary-600 hover:bg-primary-50 rounded transition"
                  >
                    <Eye className="size-4.5" />
                  </button>
                  {!isProtected ? (
                    <>
                      <button
                        onClick={() => onAction(u, false)}
                        title={`${u.status === "Active" ? "Suspend" : "Reactivate"} System Account`}
                        className={`p-1.5 rounded transition ${u.status === "Active" ? "text-neutral-500 hover:text-danger hover:bg-red-50" : "text-danger hover:text-primary-600 hover:bg-primary-50"}`}
                      >
                        <Shield className="size-4.5" />
                      </button>
                      <button
                        onClick={() => onAction(u, true)}
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
