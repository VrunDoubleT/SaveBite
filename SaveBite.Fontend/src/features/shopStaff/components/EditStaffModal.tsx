import { useEffect, useState } from "react";
import { X } from "lucide-react";

import type { ShopStaff, UpdateStaffInfoInput } from "../types/shopStaff.types";

interface Props {
  staff: ShopStaff;
  loading?: boolean;
  onClose: () => void;
  onSubmit: (data: UpdateStaffInfoInput) => Promise<void>;
}

export function EditStaffModal({ staff, loading = false, onClose, onSubmit }: Props) {
  const [nickname, setNickname] = useState(staff.staffNickname ?? "");

  const [status, setStatus] = useState<"Active" | "Suspended">(
    staff.status === "Suspended" ? "Suspended" : "Active",
  );

  useEffect(() => {
    setNickname(staff.staffNickname ?? "");

    setStatus(staff.status === "Suspended" ? "Suspended" : "Active");
  }, [staff]);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    await onSubmit({
      staffNickname: nickname.trim() || null,
      status,
    });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md rounded-2xl bg-white shadow-xl">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-gray-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Update Staff</h2>

            <p className="mt-1 text-sm text-gray-500">Update information for {staff.userName}.</p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-lg p-2 text-gray-400 transition hover:bg-gray-100 hover:text-gray-700 disabled:cursor-not-allowed"
          >
            <X size={18} />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-5 px-6 py-5">
          {/* Staff */}
          <div className="rounded-xl bg-gray-50 p-4">
            <p className="text-sm font-medium text-gray-900">{staff.userName}</p>

            <p className="mt-1 text-sm text-gray-500">{staff.userEmail}</p>
          </div>

          {/* Nickname */}
          <div>
            <label
              htmlFor="staff-nickname"
              className="mb-1.5 block text-sm font-medium text-gray-700"
            >
              Staff nickname
            </label>

            <input
              id="staff-nickname"
              value={nickname}
              onChange={(event) => setNickname(event.target.value)}
              placeholder="Enter nickname"
              disabled={loading}
              className="w-full rounded-xl border border-gray-200 bg-white px-3.5 py-2.5 text-sm text-gray-900 outline-none transition placeholder:text-gray-400 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-50"
            />
          </div>

          {/* Status */}
          <div>
            <label
              htmlFor="staff-status"
              className="mb-1.5 block text-sm font-medium text-gray-700"
            >
              Status
            </label>

            <select
              id="staff-status"
              value={status}
              onChange={(event) => setStatus(event.target.value as "Active" | "Suspended")}
              disabled={loading}
              className="w-full rounded-xl border border-gray-200 bg-white px-3.5 py-2.5 text-sm text-gray-900 outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-50"
            >
              <option value="Active">Active</option>

              <option value="Suspended">Suspended</option>
            </select>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3 border-t border-gray-100 pt-5">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="rounded-xl border border-gray-200 px-4 py-2.5 text-sm font-medium text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={loading}
              className="rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? "Saving..." : "Save changes"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
