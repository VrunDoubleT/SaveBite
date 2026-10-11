import { useState } from "react";
import { Plus } from "lucide-react";
import type { Category } from "@/features/categories/types/adminCategory.types";
import {
  CATEGORY_PAGE_SIZE,
  useCategoryManagement,
} from "@/features/categories/hooks/useCategoryManagement";
import {
  CategoryTable,
  CategoryFormModal,
  DeleteCategoryModal,
} from "@/features/categories/components";
import { Pagination } from "@/shared/components/pagination";
import { ErrorModal } from "@/shared/components/modal/ErrorModal";
import { SearchInput } from "@/shared/components/modal/SearchInput";

export function CategoryManagementPage() {
  const {
    categories,
    totalItems,
    totalPages,
    page,
    searchInput,
    setSearchInput,
    errorMessage,
    clearError,
    goToPage,
    saveCategory,
    deleteCategory,
    toggleStatus,
  } = useCategoryManagement();

  // UI state for the popups
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  const openCreateModal = () => {
    setEditingCategory(null);
    setIsFormOpen(true);
  };

  const openEditModal = (category: Category) => {
    setEditingCategory(category);
    setIsFormOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!deleteConfirmId) return;
    const id = deleteConfirmId;
    setDeleteConfirmId(null);
    await deleteCategory(id);
  };

  return (
    <div className="space-y-6 relative">
      {/* Header & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-text-primary">Food Categories</h2>
          <p className="text-sm text-text-secondary">Manage platform food categories.</p>
        </div>
        <div className="flex flex-col sm:flex-row gap-3">
          <SearchInput
            value={searchInput}
            onChange={setSearchInput}
            placeholder="Search categories..."
          />
          <button
            onClick={openCreateModal}
            className="flex items-center justify-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-semibold text-white hover:bg-primary-700 transition shadow-sm"
          >
            <Plus className="size-4" /> Add Category
          </button>
        </div>
      </div>

      {/* Table + Pagination */}
      <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm">
        <CategoryTable
          categories={categories}
          onToggleStatus={toggleStatus}
          onEdit={openEditModal}
          onDelete={setDeleteConfirmId}
        />

        {totalItems > 0 && (
          <div className="flex w-full items-center justify-around border-t border-border-default bg-neutral-50/30 px-6 py-5">
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              onPageChange={goToPage}
              totalItems={totalItems}
              pageSize={CATEGORY_PAGE_SIZE}
              itemLabel="categories"
              showTotalItems={true}
              showFirstLast={true}
              className="w-full"
            />
          </div>
        )}
      </div>

      {/* Popups */}
      <CategoryFormModal
        isOpen={isFormOpen}
        category={editingCategory}
        onClose={() => setIsFormOpen(false)}
        onSave={saveCategory}
      />

      <DeleteCategoryModal
        isOpen={!!deleteConfirmId}
        onClose={() => setDeleteConfirmId(null)}
        onConfirm={handleConfirmDelete}
      />

      {/* Keep last: all modals share z-50, so the later one in the DOM sits on top */}
      <ErrorModal message={errorMessage} onClose={clearError} />
    </div>
  );
}
