import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { Category } from "@/features/categories/types/adminCategory.types";
import {
  categorySchema,
  type CategoryFormData,
} from "@/features/categories/schemas/category.schema";
import type { SaveCategoryResult } from "@/features/categories/hooks/useCategoryManagement";

interface CategoryFormModalProps {
  isOpen: boolean;
  /** null = create mode, Category = edit mode */
  category: Category | null;
  onClose: () => void;
  onSave: (data: CategoryFormData, editingId: string | null) => Promise<SaveCategoryResult>;
}

const EMPTY_VALUES: CategoryFormData = {
  name: "",
  description: "",
  imageUrl: "",
  isActive: true,
};

export function CategoryFormModal({ isOpen, category, onClose, onSave }: CategoryFormModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CategoryFormData>({
    resolver: zodResolver(categorySchema),
    defaultValues: EMPTY_VALUES,
  });

  // Fill the form each time the modal opens (create or edit)
  useEffect(() => {
    if (!isOpen) return;
    reset(
      category
        ? {
            name: category.name,
            description: category.description || "",
            imageUrl: category.imageUrl || "",
            isActive: category.isActive,
          }
        : EMPTY_VALUES,
    );
  }, [isOpen, category, reset]);

  const onSubmit = async (data: CategoryFormData) => {
    const result = await onSave(data, category?.id ?? null);
    if (result === "success") {
      onClose();
    } else if (result === "duplicate") {
      setError("name", { type: "manual", message: "This name entry already exists." });
    }
  };

  return (
    <SharedModal
      isOpen={isOpen}
      onClose={onClose}
      title={category ? "Edit Category" : "Create Category"}
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-sm font-semibold text-text-primary mb-1.5">Name</label>
          <input
            {...register("name")}
            className={`w-full rounded-md border px-3 py-2.5 text-sm focus:ring-1 focus:outline-none transition-all ${
              errors.name
                ? "border-danger focus:border-danger focus:ring-danger"
                : "border-border-default focus:border-border-focus focus:ring-border-focus"
            }`}
            placeholder="Enter category name"
          />
          {errors.name && (
            <p className="mt-1.5 text-xs font-medium text-danger">{errors.name.message}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-semibold text-text-primary mb-1.5">
            Description
          </label>
          <textarea
            {...register("description")}
            className="w-full rounded-md border border-border-default px-3 py-2.5 text-sm focus:border-border-focus focus:ring-1 focus:ring-border-focus focus:outline-none transition-all resize-none"
            rows={3}
            placeholder="Brief description (optional)"
          />
        </div>

        <div className="flex items-center gap-3 pt-2">
          <input
            type="checkbox"
            {...register("isActive")}
            id="isActive"
            className="size-4.5 rounded border-border-default text-primary-600 focus:ring-primary-500 cursor-pointer"
          />
          <label
            htmlFor="isActive"
            className="text-sm font-semibold text-text-primary cursor-pointer select-none"
          >
            Active on store
          </label>
        </div>

        <div className="mt-8 flex justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary hover:bg-neutral-50 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-md bg-primary-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-primary-700 transition-colors disabled:opacity-60 shadow-sm"
          >
            {isSubmitting ? "Saving..." : "Save Category"}
          </button>
        </div>
      </form>
    </SharedModal>
  );
}
