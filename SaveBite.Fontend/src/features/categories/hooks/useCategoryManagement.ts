import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { categoryApi } from "@/features/categories/api/adminCategoryApi";
import type { Category } from "@/features/categories/types/adminCategory.types";
import type { CategoryFormData } from "@/features/categories/schemas/category.schema";
import { getApiErrorMessage } from "@/shared/api";

export const CATEGORY_PAGE_SIZE = 5;

export type SaveCategoryResult = "success" | "duplicate" | "error";

/**
 * Handles data fetching, search (debounced), pagination (via URL params)
 * and all category actions for the admin category page.
 */
export function useCategoryManagement() {
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page")) || 1;
  const currentSearch = searchParams.get("search") || "";

  const [categories, setCategories] = useState<Category[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [searchInput, setSearchInput] = useState(currentSearch);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Debounce the search input into the URL
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
        pageSize: CATEGORY_PAGE_SIZE,
        search: currentSearch || undefined,
      });
      setCategories(res.items);
      setTotalItems(res.total ?? 0);
      setTotalPages(res.totalPages ?? 1);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
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

  const saveCategory = async (
    data: CategoryFormData,
    editingId: string | null,
  ): Promise<SaveCategoryResult> => {
    try {
      if (editingId) await categoryApi.updateCategory(editingId, data);
      else await categoryApi.createCategory(data);
      await fetchCategories();
      return "success";
    } catch (error) {
      const message = getApiErrorMessage(error);
      const lower = message.toLowerCase();
      if (lower.includes("exist") || lower.includes("duplicate")) return "duplicate";
      setErrorMessage(message);
      return "error";
    }
  };

  const deleteCategory = async (id: string) => {
    try {
      await categoryApi.deleteCategory(id);
      if (categories.length === 1 && page > 1) goToPage(page - 1);
      else await fetchCategories();
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    }
  };

  const toggleStatus = async (id: string, currentStatus: boolean) => {
    try {
      await categoryApi.toggleStatus(id, !currentStatus);
      await fetchCategories();
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    }
  };

  return {
    categories,
    totalItems,
    totalPages,
    page,
    searchInput,
    setSearchInput,
    errorMessage,
    clearError: () => setErrorMessage(null),
    goToPage,
    saveCategory,
    deleteCategory,
    toggleStatus,
  };
}
