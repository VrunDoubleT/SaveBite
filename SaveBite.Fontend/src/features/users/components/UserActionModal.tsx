import { useState, type FormEvent } from "react";
import { AlertCircle, CheckCircle } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { AdminUser } from "@/features/users/types/user.types";

export interface UserActionTarget {
  user: AdminUser;
  isCustomerProfile: boolean;
  /** true = the action will suspend, false = the action will reactivate */
  isSuspended: boolean;
}

interface UserActionModalProps {
  /** null = modal closed */
  target: UserActionTarget | null;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

export function UserActionModal({ target, onClose, onConfirm }: UserActionModalProps) {
  return (
    <SharedModal isOpen={!!target} onClose={onClose} maxWidthClass="max-w-md">
      {/* The form is only mounted while the modal is open, so `reason` resets on every open */}
      {target && <UserActionForm target={target} onClose={onClose} onConfirm={onConfirm} />}
    </SharedModal>
  );
}

interface UserActionFormProps {
  target: UserActionTarget;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

function UserActionForm({ target, onClose, onConfirm }: UserActionFormProps) {
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { user, isCustomerProfile, isSuspended } = target;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) return;

    setIsSubmitting(true);
    try {
      await onConfirm(reason.trim());
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <div className="text-center">
        <div
          className={`mx-auto flex size-12 items-center justify-center rounded-full mb-4 ${isSuspended ? "bg-red-100" : "bg-primary-100"}`}
        >
          {isSuspended ? (
            <AlertCircle className="size-6 text-danger" />
          ) : (
            <CheckCircle className="size-6 text-primary-600" />
          )}
        </div>
        <h3 className="text-xl font-bold text-center text-text-primary mb-1">
          {isSuspended ? "Suspend" : "Reactivate"}{" "}
          {isCustomerProfile ? "Customer Profile" : "Account"}
        </h3>
        <p className="mb-6 text-sm text-center text-text-secondary">
          Target user: <span className="font-semibold text-text-primary">{user.email}</span>
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4 text-left">
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
            onClick={onClose}
            className="rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary hover:bg-neutral-50 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || !reason.trim()}
            className={`rounded-md px-5 py-2.5 text-sm font-semibold text-white transition-colors disabled:opacity-60 shadow-sm ${isSuspended ? "bg-danger hover:bg-red-700" : "bg-primary-600 hover:bg-primary-700"}`}
          >
            {isSubmitting ? "Processing..." : "Confirm Action"}
          </button>
        </div>
      </form>
    </>
  );
}
