import { useState } from "react";
import { AlertTriangle, CheckCircle2 } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { AdminTrustLevel, UpdateLevelStatusPayload } from "../types/adminTrust.types";

interface LevelStatusModalProps {
  level: AdminTrustLevel | null;
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onConfirm: (levelId: string, payload: UpdateLevelStatusPayload) => Promise<string | null>;
}

// UC-155: Deactivate or Reactivate a Trust Level.
export function LevelStatusModal({ level, onClose, onConfirm }: LevelStatusModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const handleClose = () => {
    if (isSubmitting) return;
    setServerError(null);
    onClose();
  };

  if (!level) return null;

  const isDeactivating = level.isActive;

  const handleSubmit = async () => {
    setIsSubmitting(true);
    setServerError(null);
    try {
      const error = await onConfirm(level.id, { isActive: !level.isActive });
      if (error) {
        setServerError(error);
      } else {
        onClose();
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <SharedModal
      isOpen={level !== null}
      onClose={handleClose}
      title={
        isDeactivating
          ? `Deactivate ${level.name} (Rank ${level.rank})`
          : `Reactivate ${level.name} (Rank ${level.rank})`
      }
    >
      <div className="space-y-4">
        {isDeactivating ? (
          <>
            <div className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4 text-amber-900">
              <AlertTriangle className="mt-0.5 size-5 shrink-0 text-amber-600" />
              <div className="space-y-1 text-sm">
                <p className="font-bold">
                  {level.userCount} customer{level.userCount === 1 ? "" : "s"} will be reassigned to
                  another level.
                </p>
                <p className="text-xs leading-relaxed text-amber-800">
                  Each customer currently on <strong>{level.name}</strong> will be immediately
                  recalculated to the highest active level whose requirements (score, completed
                  orders, and order value) they meet, and recorded as an{" "}
                  <code>AdminAdjustment</code> in their level history.
                </p>
              </div>
            </div>

            <p className="text-xs leading-relaxed text-text-secondary">
              Customer Trust Scores, accumulated order values, and deposits on existing orders
              remain unchanged. Level ranks are not renumbered.
            </p>
          </>
        ) : (
          <div className="flex items-start gap-3 rounded-lg border border-primary-200 bg-primary-50/60 p-4 text-text-primary">
            <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-primary-600" />
            <div className="space-y-1 text-sm">
              <p className="font-semibold">
                Reactivate {level.name} (Rank {level.rank})?
              </p>
              <p className="text-xs leading-relaxed text-text-secondary">
                The system will verify that {level.name}&apos;s requirements follow the ascending
                threshold order (BR-157) with all active levels. Customers will qualify for this
                level on their next eligible order evaluation.
              </p>
            </div>
          </div>
        )}

        {serverError && (
          <p role="alert" className="rounded-md bg-danger/10 p-3 text-sm text-danger">
            {serverError}
          </p>
        )}

        <div className="flex justify-end gap-3 pt-2">
          <button
            type="button"
            disabled={isSubmitting}
            onClick={handleClose}
            className="rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary transition-colors hover:bg-neutral-50 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={isSubmitting}
            onClick={() => void handleSubmit()}
            className={`rounded-md px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors disabled:opacity-60 ${
              isDeactivating
                ? "bg-danger hover:bg-red-700"
                : "bg-primary-600 hover:bg-primary-700"
            }`}
          >
            {isSubmitting
              ? "Processing..."
              : isDeactivating
                ? "Deactivate level"
                : "Reactivate level"}
          </button>
        </div>
      </div>
    </SharedModal>
  );
}