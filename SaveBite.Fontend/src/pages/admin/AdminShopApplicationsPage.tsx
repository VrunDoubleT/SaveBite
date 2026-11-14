import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";

import { getApiErrorMessage } from "@/shared/api/httpClient";
import { Pagination } from "@/shared/components/pagination/Pagination";
import { usePagination } from "@/shared/components/pagination/usePagination";
import { adminShopApplicationApi } from "@/features/shop-applications/api/adminShopApplicationApi";
import { AdminShopApplicationDetail } from "@/features/shop-applications/components/AdminShopApplicationDetail";
import { AdminShopApplicationsList } from "@/features/shop-applications/components/AdminShopApplicationsList";
import { ADMIN_SHOP_APPLICATION_FILTERS } from "@/features/shop-applications/schemas/adminShopApplication.schema";
import type {
  ShopApplication,
  ShopApplicationAdminFilter,
} from "@/features/shop-applications/types/adminShopApplication.types";

const PAGE_SIZE = 10;

export function AdminShopApplicationsPage() {
  const [items, setItems] = useState<ShopApplication[]>([]);
  const [selected, setSelected] = useState<ShopApplication | null>(null);
  const [searchParams, setSearchParams] = useSearchParams();
  const statusParam = searchParams.get("status");
  const pageParam = Number(searchParams.get("page") ?? 1);
  const filter: ShopApplicationAdminFilter = ADMIN_SHOP_APPLICATION_FILTERS.includes(
    statusParam as ShopApplicationAdminFilter,
  )
    ? (statusParam as ShopApplicationAdminFilter)
    : "All";
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

  useEffect(() => {
    const handleUpdate = () => {
      void load();
    };
    window.addEventListener("shop-application:updated", handleUpdate);
    return () => window.removeEventListener("shop-application:updated", handleUpdate);
  }, [load]);

  const sortedItems = useMemo(() => {
    const filtered = items.filter((item) => filter === "All" || item.status === filter);

    return filtered.sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );
  }, [items, filter]);

  const {
    currentPage,
    setCurrentPage,
    totalPages,
    totalItems,
    pageSize,
    paginatedItems,
  } = usePagination({
    items: sortedItems,
    initialPageSize: PAGE_SIZE,
    initialPage: Number.isInteger(pageParam) && pageParam > 0 ? pageParam : 1,
  });

  const counts = useMemo(() => {
    const result: Record<string, number> = { All: items.length };

    ADMIN_SHOP_APPLICATION_FILTERS.slice(1).forEach((status) => {
      result[status] = items.filter((item) => item.status === status).length;
    });

    return result;
  }, [items]);

  const handleFilterChange = (value: string) => {
    if (!ADMIN_SHOP_APPLICATION_FILTERS.includes(value as ShopApplicationAdminFilter)) return;

    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);
        if (value === "All") next.delete("status");
        else next.set("status", value);
        next.delete("page");
        return next;
      },
      { preventScrollReset: true },
    );
    setCurrentPage(1);
  };

  useEffect(() => {
    const requestedPage = Number.isInteger(pageParam) && pageParam > 0
      ? Math.min(pageParam, totalPages)
      : 1;
    if (requestedPage !== currentPage) setCurrentPage(requestedPage);
  }, [pageParam, totalPages, currentPage, setCurrentPage]);

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);
        next.set("page", String(page));
        return next;
      },
      { preventScrollReset: true },
    );
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

  const review = async (decision: "Approved" | "Rejected" | "NeedsRevision") => {
    if (!selected) return;

    if (decision !== "Approved" && !note.trim()) {
      setError("Please provide a reason for rejection or requested changes.");
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
      window.dispatchEvent(new CustomEvent("shop-application:updated"));
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
            onPageChange={handlePageChange}
            itemLabel="applications"
            showTotalItems={true}
            showFirstLast={true}
            className="w-full"
          />
        ) : null
      }
    />
  );
}
