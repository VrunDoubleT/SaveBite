import { Trash2 } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";

interface DeleteCategoryModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export function DeleteCategoryModal({ isOpen, onClose, onConfirm }: DeleteCategoryModalProps) {
  return (
    <SharedModal isOpen={isOpen} onClose={onClose} maxWidthClass="max-w-sm">
      <div className="text-center">
        <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-red-100 mb-4 shadow-sm">
          <Trash2 className="size-6 text-danger" />
        </div>
        <h3 className="text-lg font-bold text-center text-text-primary mb-2">Delete Category</h3>
        <p className="text-sm text-center text-text-secondary mb-6">
          Are you sure you want to delete this category? This action cannot be undone.
        </p>
        <div className="flex justify-center gap-3">
          <button
            onClick={onClose}
            className="flex-1 rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary hover:bg-neutral-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            className="flex-1 rounded-md bg-danger px-4 py-2.5 text-sm font-semibold text-white hover:bg-red-700 transition-colors shadow-sm"
          >
            Delete
          </button>
        </div>
      </div>
    </SharedModal>
  );
}
