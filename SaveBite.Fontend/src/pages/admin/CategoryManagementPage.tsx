import { useEffect, useState, useCallback } from "react";
import { useSearchParams } from "react-router-dom";
import {
  Plus,
  Edit,
  Trash2,
  Power,
  Search,
  X,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  MoreHorizontal,
} from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { categoryApi } from "@/features/admin/api/categoryApi";
import type { Category } from "@/features/admin/types/category.types";
import { getApiErrorMessage } from "@/shared/api";

const schema = z.object({
  name: z.string().min(1, "Name is required").max(150),
  description: z.string().optional(),
  imageUrl: z.string().url("Must be a valid URL").optional().or(z.literal("")),
  isActive: z.boolean(),
});
type FormData = z.infer<typeof schema>;

const PAGE_SIZE = 5;

export function CategoryManagementPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page")) || 1;
  const currentSearch = searchParams.get("search") || "";

  const [categories, setCategories] = useState<Category[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  const [searchInput, setSearchInput] = useState(currentSearch);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { isActive: true },
  });

  useEffect(() => {
    const handler = setTimeout(() => {
      if (searchInput !== currentSearch) {
        setSearchParams(
          (prev) => {
            if (searchInput) prev.set("search", searchInput);
            else prev.delete("search");
            prev.set("page", "1");
            return prev;
          },
          { replace: true },
        );
      }
    }, 500);
    return () => clearTimeout(handler);
  }, [searchInput, currentSearch, setSearchParams]);

  const fetchCategories = useCallback(async () => {
    try {
      const res = await categoryApi.getCategories({
        page,
        pageSize: PAGE_SIZE,
        search: currentSearch || undefined,
      });
      setCategories(res.items);
      setTotalItems(res.total ?? 0);
      setTotalPages(res.totalPages ?? 1);
    } catch (error) {
      console.error(error);
    }
  }, [page, currentSearch]);

  useEffect(() => {
    const loadData = async () => await fetchCategories();
    void loadData();
  }, [fetchCategories]);

  const goToPage = (newPage: number) => {
    setSearchParams((prev) => {
      prev.set("page", newPage.toString());
      return prev;
    });
  };

  const getPageNumbers = () => {
    if (totalPages <= 4) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    if (page <= 2) {
      return [1, 2, 3, "...", totalPages];
    }

    if (page >= totalPages - 1) {
      return [1, "...", totalPages - 2, totalPages - 1, totalPages];
    }

    return [1, "...", page - 1, page, page + 1, "...", totalPages];
  };

  const openModal = (category?: Category) => {
    if (category) {
      setEditingId(category.id);
      reset({
        name: category.name,
        description: category.description || "",
        imageUrl: category.imageUrl || "",
        isActive: category.isActive,
      });
    } else {
      setEditingId(null);
      reset({ name: "", description: "", imageUrl: "", isActive: true });
    }
    setIsModalOpen(true);
  };

  const onSubmit = async (data: FormData) => {
    try {
      if (editingId) await categoryApi.updateCategory(editingId, data);
      else await categoryApi.createCategory(data);
      setIsModalOpen(false);
      await fetchCategories();
    } catch (error) {
      alert(getApiErrorMessage(error));
    }
  };

  const handleDelete = async () => {
    if (!deleteConfirmId) return;
    try {
      await categoryApi.deleteCategory(deleteConfirmId);
      setDeleteConfirmId(null);
      if (categories.length === 1 && page > 1) goToPage(page - 1);
      else await fetchCategories();
    } catch (error) {
      alert(getApiErrorMessage(error));
      setDeleteConfirmId(null);
    }
  };

  const handleToggleStatus = async (id: string, currentStatus: boolean) => {
    try {
      await categoryApi.toggleStatus(id, !currentStatus);
      await fetchCategories();
    } catch (error) {
      alert(getApiErrorMessage(error));
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-text-primary">Food Categories</h2>
          <p className="text-sm text-text-secondary">Manage platform food categories.</p>
        </div>
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-neutral-400" />
            <input
              type="text"
              placeholder="Search categories..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="w-full sm:w-64 rounded-md border border-border-default py-2 pl-9 pr-9 text-sm focus:border-border-focus focus:outline-none transition-colors"
            />
            {searchInput && (
              <button
                onClick={() => setSearchInput("")}
                className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-neutral-400 hover:text-neutral-700 transition-colors"
              >
                <X className="size-3.5" />
              </button>
            )}
          </div>
          <button
            onClick={() => openModal()}
            className="flex items-center justify-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-semibold text-white hover:bg-primary-700 transition shadow-sm"
          >
            <Plus className="size-4" /> Add Category
          </button>
        </div>
      </div>

      <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm">
        <table className="min-w-full divide-y divide-border-default text-left text-sm">
          <thead className="bg-neutral-50 text-text-secondary">
            <tr>
              <th className="px-6 py-4 font-semibold">Name</th>
              <th className="px-6 py-4 font-semibold">Description</th>
              <th className="px-6 py-4 font-semibold">Status</th>
              <th className="px-6 py-4 font-semibold text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-default text-text-primary">
            {categories.map((c) => (
              <tr key={c.id} className="hover:bg-neutral-50/50 transition-colors">
                <td className="px-6 py-4 font-medium">{c.name}</td>
                <td className="px-6 py-4 text-text-muted">{c.description || "—"}</td>
                <td className="px-6 py-4">
                  <span
                    className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${c.isActive ? "bg-primary-50 text-primary-700" : "bg-neutral-100 text-neutral-600"}`}
                  >
                    {c.isActive ? "Active" : "Inactive"}
                  </span>
                </td>
                <td className="px-6 py-4 text-right">
                  <div className="flex justify-end gap-1">
                    <button
                      onClick={() => handleToggleStatus(c.id, c.isActive)}
                      className="p-2 text-neutral-400 hover:text-primary-600 transition-colors rounded-md hover:bg-primary-50"
                      title={c.isActive ? "Deactivate" : "Activate"}
                    >
                      <Power className="size-4" />
                    </button>
                    <button
                      onClick={() => openModal(c)}
                      className="p-2 text-neutral-400 hover:text-info transition-colors rounded-md hover:bg-blue-50"
                      title="Edit"
                    >
                      <Edit className="size-4" />
                    </button>
                    <button
                      onClick={() => setDeleteConfirmId(c.id)}
                      className="p-2 text-neutral-400 hover:text-danger transition-colors rounded-md hover:bg-red-50"
                      title="Delete"
                    >
                      <Trash2 className="size-4" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {categories.length === 0 && (
              <tr>
                <td colSpan={4} className="p-10 text-center text-text-muted">
                  No categories found matching your criteria.
                </td>
              </tr>
            )}
          </tbody>
        </table>

        {/* Advanced Pagination UI */}
        {totalItems > 0 && (
          <div className="flex flex-col items-center justify-center border-t border-border-default bg-neutral-50/30 py-5">
            <div className="flex items-center gap-1.5">
              {/**/}
              <button
                disabled={page === 1}
                onClick={() => goToPage(1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
              >
                <ChevronsLeft className="size-4" />
              </button>
              {/**/}
              <button
                disabled={page === 1}
                onClick={() => goToPage(page - 1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
              >
                <ChevronLeft className="size-4" />
              </button>

              {/**/}
              {getPageNumbers().map((p, index) =>
                p === "..." ? (
                  <span
                    key={`ellipsis-${index}`}
                    className="flex h-8 w-8 items-center justify-center text-text-muted"
                  >
                    <MoreHorizontal className="size-4" />
                  </span>
                ) : (
                  <button
                    key={p}
                    onClick={() => goToPage(p as number)}
                    className={`flex h-8 w-8 items-center justify-center rounded-md text-sm font-semibold transition-all shadow-sm ${
                      page === p
                        ? "bg-primary-600 text-white border border-primary-600"
                        : "bg-white border border-border-default text-text-secondary hover:bg-neutral-50"
                    }`}
                  >
                    {p}
                  </button>
                ),
              )}

              {/**/}
              <button
                disabled={page >= totalPages}
                onClick={() => goToPage(page + 1)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
              >
                <ChevronRight className="size-4" />
              </button>
              {/**/}
              <button
                disabled={page >= totalPages}
                onClick={() => goToPage(totalPages)}
                className="flex h-8 w-8 items-center justify-center rounded-md border border-border-default bg-white text-text-secondary transition hover:bg-neutral-50 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm"
              >
                <ChevronsRight className="size-4" />
              </button>
            </div>
            <p className="mt-3 text-xs font-medium text-text-muted opacity-80">
              Showing <span className="text-text-secondary">{(page - 1) * PAGE_SIZE + 1}</span> to{" "}
              <span className="text-text-secondary">{Math.min(page * PAGE_SIZE, totalItems)}</span>{" "}
              of <span className="text-text-secondary">{totalItems}</span> results
            </p>
          </div>
        )}
      </div>

      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm transition-opacity">
          <div className="w-full max-w-md rounded-xl bg-bg-surface p-6 shadow-xl animate-in fade-in zoom-in-95 duration-200">
            <h3 className="text-xl font-bold text-text-primary mb-5">
              {editingId ? "Edit Category" : "Create Category"}
            </h3>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-text-primary mb-1.5">Name</label>
                <input
                  {...register("name")}
                  className="w-full rounded-md border border-border-default px-3 py-2.5 text-sm focus:border-border-focus focus:ring-1 focus:ring-border-focus focus:outline-none transition-all"
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
                  className="w-full rounded-md border border-border-default px-3 py-2.5 text-sm focus:border-border-focus focus:ring-1 focus:ring-border-focus focus:outline-none transition-all"
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
                  onClick={() => setIsModalOpen(false)}
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
          </div>
        </div>
      )}

      {deleteConfirmId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-neutral-900/40 p-4 backdrop-blur-sm transition-opacity">
          <div className="w-full max-w-sm rounded-xl bg-bg-surface p-6 shadow-xl animate-in fade-in zoom-in-95 duration-200">
            <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-red-100 mb-4">
              <Trash2 className="size-6 text-danger" />
            </div>
            <h3 className="text-lg font-bold text-center text-text-primary mb-2">
              Delete Category
            </h3>
            <p className="text-sm text-center text-text-secondary mb-6">
              Are you sure you want to delete this category? This action cannot be undone.
            </p>
            <div className="flex justify-center gap-3">
              <button
                onClick={() => setDeleteConfirmId(null)}
                className="flex-1 rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary hover:bg-neutral-50 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                className="flex-1 rounded-md bg-danger px-4 py-2.5 text-sm font-semibold text-white hover:bg-red-700 transition-colors shadow-sm"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
