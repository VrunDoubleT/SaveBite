import { useCallback, useEffect, useMemo, useState } from "react";

import { getApiErrorMessage } from "@/shared/api/httpClient";
import { Pagination } from "@/shared/components/pagination/Pagination";
import { usePagination } from "@/shared/components/pagination/usePagination";
import { adminShopApplicationApi } from "@/features/shop-applications/api/adminShopApplicationApi";
import { AdminShopApplicationDetail } from "@/features/shop-applications/components/AdminShopApplicationDetail";
import {
  AdminShopApplicationsList,
  type SortOrder,
} from "@/features/shop-applications/components/AdminShopApplicationsList";
import { ADMIN_SHOP_APPLICATION_FILTERS } from "@/features/shop-applications/schemas/adminShopApplication.schema";
import type {
  ShopApplication,
  ShopApplicationAdminFilter,
} from "@/features/shop-applications/types/adminShopApplication.types";

const PAGE_SIZE = 5;

export function AdminShopApplicationsPage() {
  const [items, setItems] = useState<ShopApplication[]>([]);
  const [selected, setSelected] = useState<ShopApplication | null>(null);
  const [filter, setFilter] = useState<ShopApplicationAdminFilter>("All");
  const [sort, setSort] = useState<SortOrder>("newest");
  const [note, setNote] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [openingId, setOpeningId] = useState<ShopApplication["id"] | null>(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      setItems(await adminShopApplicationApi.getAll());
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const sortedItems = useMemo(() => {
    const filtered = items.filter((item) => filter === "All" || item.status === filter);

    return filtered.sort((a, b) => {
      const diff = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      return sort === "newest" ? -diff : diff;
    });
  }, [items, filter, sort]);

  const {
    currentPage,
    setCurrentPage,
    totalPages,
    totalItems,
    pageSize,
    paginatedItems,
    resetPage,
  } = usePagination({ items: sortedItems, initialPageSize: PAGE_SIZE });

  const counts = useMemo(() => {
    const result: Record<string, number> = { All: items.length };

    ADMIN_SHOP_APPLICATION_FILTERS.slice(1).forEach((status) => {
      result[status] = items.filter((item) => item.status === status).length;
    });

    return result;
  }, [items]);

  const handleFilterChange = (value: string) => {
    setFilter(value as ShopApplicationAdminFilter);
    resetPage();
  };

  const handleSortChange = (value: SortOrder) => {
    setSort(value);
    resetPage();
  };

  const openDetail = async (item: ShopApplication) => {
    setError("");
    setSuccess("");
    setNote("");
    setOpeningId(item.id);

    try {
      setSelected(await adminShopApplicationApi.getById(item.id));
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      setOpeningId(null);
    }
  };

  const review = async (decision: "Approved" | "Rejected") => {
    if (!selected) return;

    if (decision === "Rejected" && !note.trim()) {
      setError("Please provide a reason before rejecting this application.");
      return;
    }

    setSaving(true);
    setError("");
    setSuccess("");

    try {
      const application = await adminShopApplicationApi.review(
        selected.id,
        decision,
        note.trim() || null,
      );

      setSelected(application);
      setItems((current) =>
        current.map((item) => (item.id === application.id ? application : item)),
      );
      setSuccess(`Application ${decision.toLowerCase()} successfully.`);
      setNote("");
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      setSaving(false);
    }
  };

  if (selected) {
    return (
      <AdminShopApplicationDetail
        app={selected}
        error={error}
        success={success}
        note={note}
        saving={saving}
        onBack={() => {
          setSelected(null);
          setError("");
          setSuccess("");
        }}
        onNoteChange={setNote}
        onReview={(decision) => void review(decision)}
      />
    );
  }

  return (
    <AdminShopApplicationsList
      visibleItems={paginatedItems}
      loading={loading}
      filter={filter}
      setFilter={handleFilterChange}
      sort={sort}
      onSortChange={handleSortChange}
      counts={counts}
      error={error}
      load={() => void load()}
      openDetail={(item) => void openDetail(item)}
      openingId={openingId}
      pagination={
        !loading && totalItems > 0 ? (
          <Pagination
            currentPage={currentPage}
            totalPages={totalPages}
            totalItems={totalItems}
            pageSize={pageSize}
            onPageChange={setCurrentPage}
            itemLabel="applications"
            showFirstLast
          />
        ) : null
      }
    />
  );
}
