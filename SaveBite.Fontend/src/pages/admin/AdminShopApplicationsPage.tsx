import { useCallback, useEffect, useMemo, useState } from "react";

import { getApiErrorMessage } from "@/shared/api/httpClient";
import { adminShopApplicationApi } from "@/features/admin/shop-applications/api/adminShopApplicationApi";
import { AdminShopApplicationDetail } from "@/features/admin/shop-applications/components/AdminShopApplicationDetail";
import { AdminShopApplicationsList } from "@/features/admin/shop-applications/components/AdminShopApplicationsList";
import { ADMIN_SHOP_APPLICATION_FILTERS } from "@/features/admin/shop-applications/schemas/adminShopApplication.schema";
import type {
  ShopApplication,
  ShopApplicationAdminFilter,
} from "@/features/admin/shop-applications/types/adminShopApplication.types";

export function AdminShopApplicationsPage() {
  const [items, setItems] = useState<ShopApplication[]>([]);
  const [selected, setSelected] = useState<ShopApplication | null>(null);
  const [filter, setFilter] = useState<ShopApplicationAdminFilter>("All");
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
      const applications = await adminShopApplicationApi.getAll();
      setItems(applications);
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const visibleItems = useMemo(
    () => items.filter((item) => filter === "All" || item.status === filter),
    [items, filter],
  );

  const counts = useMemo(() => {
    const result: Record<string, number> = { All: items.length };

    ADMIN_SHOP_APPLICATION_FILTERS.slice(1).forEach((status) => {
      result[status] = items.filter((item) => item.status === status).length;
    });

    return result;
  }, [items]);

  const openDetail = async (item: ShopApplication) => {
    setError("");
    setSuccess("");
    setNote("");
    setOpeningId(item.id);

    try {
      const application = await adminShopApplicationApi.getById(item.id);
      setSelected(application);
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
      visibleItems={visibleItems}
      loading={loading}
      filter={filter}
      setFilter={(value) => setFilter(value as ShopApplicationAdminFilter)}
      counts={counts}
      error={error}
      load={() => void load()}
      openDetail={(item) => void openDetail(item)}
      openingId={openingId}
    />
  );
}
