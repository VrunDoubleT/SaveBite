import { AlertTriangle, X } from "lucide-react";
import type { ShopStaff } from "../types/shopStaff.types";

interface RemoveStaffModalProps {
  staff: ShopStaff;
  loading?: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export function RemoveStaffModal({
  staff,
  loading = false,
  onClose,
  onConfirm,
}: RemoveStaffModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-xl">
        {/* Header */}
        <div className="flex items-start justify-between border-b border-gray-100 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-red-50 text-red-600">
              <AlertTriangle size={20} />
            </div>

            <div>
              <h2 className="text-lg font-semibold text-gray-900">
                Remove staff member
              </h2>

              <p className="mt-0.5 text-sm text-gray-500">
                This action will remove the staff member from your shop.
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-lg p-1.5 text-gray-400 transition hover:bg-gray-100 hover:text-gray-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <X size={20} />
          </button>
        </div>

        {/* Content */}
        <div className="px-6 py-5">
          <div className="rounded-xl bg-gray-50 p-4">
            <p className="font-medium text-gray-900">
              {staff.userName}
            </p>

            <p className="mt-1 text-sm text-gray-500">
              {staff.userEmail}
            </p>
          </div>

          <p className="mt-4 text-sm leading-6 text-gray-600">
            Are you sure you want to remove{" "}
            <span className="font-semibold text-gray-900">
              {staff.userName}
            </span>{" "}
            from your shop?
          </p>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3 border-t border-gray-100 bg-gray-50 px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Cancel
          </button>

          <button
            type="button"
            onClick={onConfirm}
            disabled={loading}
            className="inline-flex items-center justify-center rounded-xl bg-red-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {loading ? "Removing..." : "Remove staff"}
          </button>
        </div>
      </div>
    </div>
  );
}