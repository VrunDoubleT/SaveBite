import { Eye, Pencil, Trash2, Users } from "lucide-react";

import type { ShopStaff } from "../types/shopStaff.types";
import { StaffStatusBadge } from "./StaffStatusBadge";
import { formatStaffName } from "@/features/shop-staffs/utils/formatStaffName";

function formatDate(date: string) {
  return new Date(date).toLocaleDateString("en-US", { month: "short", day: "2-digit", year: "numeric" });
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

const HEAD_CELL = "px-6 py-3 text-left text-xs font-semibold uppercase tracking-wide text-neutral-500";

const ICON_BUTTON =
  "inline-flex h-8 w-8 items-center justify-center rounded-lg text-neutral-500 transition hover:bg-neutral-100 hover:text-neutral-900";

export function StaffTable({ staffs, onView, onEdit, onRemove }: Props) {
  if (staffs.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-neutral-200 bg-white py-16 text-center shadow-2xs">
        <Users className="mb-3 h-10 w-10 text-neutral-300" />
        <p className="font-medium text-neutral-900">No staff yet</p>
        <p className="mt-1 text-sm text-neutral-500">Invite your first staff member to get started.</p>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-2xs">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px]">
          <thead className="border-b border-neutral-200 bg-neutral-50/75">
            <tr>
              <th className={HEAD_CELL}>Staff</th>
              <th className={HEAD_CELL}>Role</th>
              <th className={HEAD_CELL}>Status</th>
              <th className={HEAD_CELL}>Joined date</th>
              <th className={`${HEAD_CELL} text-right`}>Actions</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-neutral-100">
            {staffs.map((staff) => (
              <tr key={staff.id} className="transition hover:bg-neutral-50/70">
                {/* Staff info */}
                <td className="px-6 py-4">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-sm font-semibold text-emerald-700">
                      {getInitials(staff.displayName)}
                    </div>
                    <div className="min-w-0">
                      <p className="truncate font-medium text-neutral-900">
                        {formatStaffName(staff.displayName, staff.staffNickname)}
                      </p>

                      <p className="truncate text-sm text-neutral-500">{staff.userEmail}</p>
                    </div>
                  </div>
                </td>

                {/* Role */}
                <td className="px-6 py-4">
                  <span className="rounded-md bg-neutral-100 px-2 py-1 text-xs font-medium text-neutral-700">
                    Staff
                  </span>
                </td>

                {/* Status */}
                <td className="px-6 py-4">
                  <StaffStatusBadge status={staff.status} />
                </td>

                {/* Joined date */}
                <td className="px-6 py-4 text-sm text-neutral-600">{formatDate(staff.joinedAt)}</td>

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
