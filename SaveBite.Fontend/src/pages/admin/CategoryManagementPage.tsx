import { useEffect, useState } from "react";
import { Plus, Tags, CheckCircle2, EyeOff } from "lucide-react";
import type { Category } from "@/features/categories/types/adminCategory.types";
import {
  CATEGORY_PAGE_SIZE,
  useCategoryManagement,
} from "@/features/categories/hooks/useCategoryManagement";
import { categoryApi } from "@/features/categories/api/adminCategoryApi";
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

  // Summary counts state
  const [counts, setCounts] = useState<{ total: number; active: number; inactive: number }>({
    total: 0,
    active: 0,
    inactive: 0,
  });
  const [loadingStats, setLoadingStats] = useState(true);
  const [statsReloadKey, setStatsReloadKey] = useState(0);

  useEffect(() => {
    let cancelled = false;

    Promise.all([
      categoryApi.getCategories({ page: 1, pageSize: 1 }),
      categoryApi.getCategories({ page: 1, pageSize: 1, isActive: true }),
      categoryApi.getCategories({ page: 1, pageSize: 1, isActive: false }),
    ])
      .then(([totalRes, activeRes, inactiveRes]) => {
        if (!cancelled) {
          setCounts({
            total: totalRes?.total ?? 0,
            active: activeRes?.total ?? 0,
            inactive: inactiveRes?.total ?? 0,
          });
          setLoadingStats(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setCounts((prev) => ({ ...prev, total: totalItems }));
          setLoadingStats(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [totalItems, statsReloadKey]);

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

  const handleSaveCategory = async (data: Parameters<typeof saveCategory>[0], id: string | null) => {
    const res = await saveCategory(data, id);
    if (res === "success") {
      setLoadingStats(true);
      setStatsReloadKey((k) => k + 1);
    }
    return res;
  };

  const handleToggleStatus = async (id: string, currentStatus: boolean) => {
    await toggleStatus(id, currentStatus);
    setLoadingStats(true);
    setStatsReloadKey((k) => k + 1);
  };

  const handleConfirmDelete = async () => {
    if (!deleteConfirmId) return;
    const id = deleteConfirmId;
    setDeleteConfirmId(null);
    await deleteCategory(id);
    setLoadingStats(true);
    setStatsReloadKey((k) => k + 1);
  };

  return (
    <div className="space-y-6 relative">
      {/* Header & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold tracking-tight text-neutral-900">Food Categories</h2>
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

      {/* 3 Overview Stat Cards with icons (no hover, non-clickable) */}
      <div
        className="grid grid-cols-1 gap-3 sm:grid-cols-3 sm:gap-4"
        role="region"
        aria-label="Category summary metrics"
      >
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <Tags className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">All Categories</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.total}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <CheckCircle2 className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Active</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.active}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
              <EyeOff className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Inactive</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.inactive}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Table + Pagination */}
      <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm">
        <CategoryTable
          categories={categories}
          onToggleStatus={handleToggleStatus}
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
        onSave={handleSaveCategory}
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