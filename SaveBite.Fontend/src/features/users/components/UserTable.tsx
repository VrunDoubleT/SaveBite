import { Eye, Shield, ShoppingBag } from "lucide-react";
import type { AdminUser } from "@/features/users/types/user.types";
import { RecordId } from "@/shared/components/RecordId";
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
    <div className="overflow-x-auto">
      <table className="w-full min-w-[1120px] table-fixed divide-y divide-border-default text-left text-sm">
        <colgroup>
          <col className="w-[15%]" />
          <col className="w-[28%]" />
          <col className="w-[12%]" />
          <col className="w-[15%]" />
          <col className="w-[16%]" />
          <col className="w-[14%]" />
        </colgroup>
        <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
          <tr>
            <th scope="col" className="px-5 py-4">
              ID
            </th>
            <th scope="col" className="px-5 py-4">
              User profile
            </th>
            <th scope="col" className="px-5 py-4">
              Role
            </th>
            <th scope="col" className="px-5 py-4">
              Account status
            </th>
            <th scope="col" className="px-5 py-4">
              Customer status
            </th>
            <th scope="col" className="px-5 py-4 text-right">
              Actions
            </th>
          </tr>
        </thead>

        <tbody className="divide-y divide-border-default text-text-primary">
          {users.map((u) => {
            const isProtected = u.id === currentUserId || u.role === "Admin";

            return (
              <tr key={u.id} className="align-middle transition-colors hover:bg-neutral-50/50">
                <td className="max-w-40 px-5 py-4">
                  <RecordId id={u.id} />
                </td>
                <td className="min-w-0 px-5 py-4">
                  <div className="flex min-w-0 items-center gap-3">
                    <div className="relative flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full border border-neutral-200 bg-neutral-100 text-xs font-bold text-neutral-600">
                      {u.avatarUrl ? (
                        <img
                          src={u.avatarUrl}
                          alt={u.fullName}
                          className="size-full object-cover"
                        />
                      ) : (
                        <span>{(u.fullName || "U").charAt(0).toUpperCase()}</span>
                      )}
                    </div>
                    <div className="min-w-0">
                      <p className="truncate font-semibold text-text-primary" title={u.fullName}>
                        {u.fullName}
                      </p>
                      <p className="mt-0.5 truncate text-xs text-text-muted" title={u.email}>
                        {u.email}
                      </p>
                    </div>
                  </div>
                </td>

                <td className="px-5 py-4">
                  <span className="inline-flex whitespace-nowrap rounded-md border border-neutral-200 bg-neutral-100 px-2.5 py-1.5 text-xs font-bold uppercase tracking-wide text-neutral-700">
                    {u.role === "User" ? "Customer" : u.role}
                  </span>
                </td>

                <td className="whitespace-nowrap px-5 py-4">
                  <UserStatusBadge status={u.status} />
                </td>

                <td className="whitespace-nowrap px-5 py-4">
                  <UserStatusBadge status={u.customerStatus} />
                </td>

                <td className="px-5 py-4">
                  <div className="flex items-center justify-end gap-1">
                    <button
                      onClick={() => onViewDetails(u.id)}
                      title="View Details & Logs"
                      aria-label="View Details & Logs"
                      className="rounded p-1.5 text-neutral-500 transition hover:bg-neutral-50 hover:text-neutral-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
                    >
                      <Eye className="size-4" aria-hidden="true" />
                    </button>

                    {!isProtected ? (
                      <>
                        <button
                          onClick={() => onAction(u, false)}
                          title={`${u.status === "Active" ? "Suspend" : "Reactivate"} System Account`}
                          aria-label={`${u.status === "Active" ? "Suspend" : "Reactivate"} System Account`}
                          className={`rounded p-1.5 text-neutral-500 transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700 ${
                            u.status === "Active"
                              ? "hover:bg-amber-50 hover:text-amber-600"
                              : "hover:bg-primary-50 hover:text-primary-600"
                          }`}
                        >
                          <Shield className="size-4" aria-hidden="true" />
                        </button>

                        <button
                          onClick={() => onAction(u, true)}
                          title={`${u.customerStatus === "Active" ? "Suspend" : "Activate"} Customer Account`}
                          aria-label={`${u.customerStatus === "Active" ? "Suspend" : "Activate"} Customer Account`}
                          className={`rounded p-1.5 text-neutral-500 transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700 ${
                            u.customerStatus === "Active"
                              ? "hover:bg-red-50 hover:text-danger"
                              : "hover:bg-primary-50 hover:text-primary-600"
                          }`}
                        >
                          <ShoppingBag className="size-4" aria-hidden="true" />
                        </button>
                      </>
                    ) : (
                      <span className="px-2 text-xs italic text-text-muted">Protected</span>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}

          {users.length === 0 && (
            <tr>
              <td colSpan={6} className="p-10 text-center text-text-muted">
                No users found matching your search.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
