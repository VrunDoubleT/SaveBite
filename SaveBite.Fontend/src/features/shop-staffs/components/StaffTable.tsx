import { Eye, Pencil, Trash2, Users } from "lucide-react";

import type { ShopStaff } from "../types/shopStaff.types";
import { StaffStatusBadge } from "./StaffStatusBadge";

function formatDate(date: string) {
  return new Date(date).toLocaleDateString("vi-VN");
}

// Take up to 2 initials from the user's name for the avatar
function getInitials(name: string) {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
}

interface Props {
  staffs: ShopStaff[];
  onView?: (staff: ShopStaff) => void;
  onEdit?: (staff: ShopStaff) => void;
  onRemove?: (staff: ShopStaff) => void;
}

const HEAD_CELL = "px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500";

const ICON_BUTTON =
  "inline-flex h-8 w-8 items-center justify-center rounded-lg text-gray-500 transition hover:bg-gray-100 hover:text-gray-900";

export function StaffTable({ staffs, onView, onEdit, onRemove }: Props) {
  if (staffs.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-gray-300 bg-white py-16 text-center">
        <Users className="mb-3 h-10 w-10 text-gray-300" />
        <p className="font-medium text-gray-900">No staff yet</p>
        <p className="mt-1 text-sm text-gray-500">Invite your first staff member to get started.</p>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px]">
          <thead className="border-b border-gray-200 bg-gray-50">
            <tr>
              <th className={HEAD_CELL}>Staff</th>
              <th className={HEAD_CELL}>Role</th>
              <th className={HEAD_CELL}>Status</th>
              <th className={HEAD_CELL}>Joined date</th>
              <th className={`${HEAD_CELL} text-right`}>Actions</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-gray-100">
            {staffs.map((staff) => (
              <tr key={staff.id} className="transition hover:bg-gray-50/70">
                {/* Staff info */}
                <td className="px-6 py-4">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-sm font-semibold text-emerald-700">
                      {getInitials(staff.displayName)}
                    </div>
                    <div className="min-w-0">
                      <p className="truncate font-medium text-gray-900">{staff.displayName}</p>

                      {staff.staffNickname && (
                        <p className="truncate text-xs text-gray-400">{staff.userName}</p>
                      )}

                      <p className="truncate text-sm text-gray-500">{staff.userEmail}</p>
                    </div>
                  </div>
                </td>

                {/* Role */}
                <td className="px-6 py-4">
                  <span className="rounded-md bg-gray-100 px-2 py-1 text-xs font-medium text-gray-700">
                    Staff
                  </span>
                </td>

                {/* Status */}
                <td className="px-6 py-4">
                  <StaffStatusBadge status={staff.status} />
                </td>

                {/* Joined date */}
                <td className="px-6 py-4 text-sm text-gray-600">{formatDate(staff.joinedAt)}</td>

                {/* Actions */}
                <td className="px-6 py-4">
                  <div className="flex items-center justify-end gap-1">
                    <button
                      type="button"
                      title="View"
                      className={ICON_BUTTON}
                      onClick={() => onView?.(staff)}
                    >
                      <Eye size={16} />
                    </button>
                    <button
                      type="button"
                      title="Edit"
                      className={ICON_BUTTON}
                      onClick={() => onEdit?.(staff)}
                    >
                      <Pencil size={16} />
                    </button>
                    <button
                      type="button"
                      title="Remove"
                      className={`${ICON_BUTTON} hover:bg-red-50 hover:text-red-600`}
                      onClick={() => onRemove?.(staff)}
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
