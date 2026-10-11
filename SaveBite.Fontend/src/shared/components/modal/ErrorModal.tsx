import { AlertCircle } from "lucide-react";
import { SharedModal } from "./BaseModal";

interface ErrorModalProps {
  /** Error message to show. The modal is open when this is a non-empty string. */
  message: string | null;
  onClose: () => void;
}

export function ErrorModal({ message, onClose }: ErrorModalProps) {
  return (
    <SharedModal isOpen={!!message} onClose={onClose} maxWidthClass="max-w-sm">
      <div className="text-center">
        <AlertCircle className="mx-auto size-10 text-danger mb-4" />
        <h3 className="text-lg font-bold text-text-primary mb-2">Error Occurred</h3>
        <p className="text-sm text-text-secondary mb-6">{message}</p>
        <button
          onClick={onClose}
          className="w-full rounded-md bg-neutral-100 text-neutral-800 px-4 py-2.5 font-semibold hover:bg-neutral-200 transition"
        >
          Dismiss
        </button>
      </div>
    </SharedModal>
  );
}
