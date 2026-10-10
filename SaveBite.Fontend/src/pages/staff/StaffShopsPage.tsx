import { useEffect, useState } from "react";
import { CalendarDays, ChevronRight, Store } from "lucide-react";
import { useNavigate } from "react-router-dom";

import { staffApi } from "@/features/staff-shops/api/staffApi";
import type { AssociatedShop } from "@/features/staff-shops/types/staff.types";
import { APP_PATHS } from "@/app/router/paths";

function formatDate(date: string) {
  return new Date(date).toLocaleDateString("vi-VN");
}

function getStatusStyle(status: AssociatedShop["status"]) {
  switch (status) {
    case "Active":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";

    case "Suspended":
      return "border-amber-200 bg-amber-50 text-amber-700";

    case "Removed":
      return "border-red-200 bg-red-50 text-red-700";

    default:
      return "border-gray-200 bg-gray-50 text-gray-600";
  }
}

function getStatusLabel(status: AssociatedShop["status"]) {
  switch (status) {
    case "Active":
      return "Active";

    case "Suspended":
      return "Suspended";

    case "Removed":
      return "Removed";

    default:
      return status;
  }
}

export function StaffShopsPage() {
  const [shops, setShops] = useState<AssociatedShop[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const navigate = useNavigate();

  async function loadShops() {
    try {
      setLoading(true);
      setError("");

      const response = await staffApi.getAssociatedShops();
      setShops(response);
    } catch (error) {
      console.error(error);
      setError("Unable to load store information.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadShops();
  }, []);

  function handleViewShop(shop: AssociatedShop) {
    navigate(`${APP_PATHS.STAFF}/shops/${shop.shopId}`);
  }

  if (loading) {
    return (
      <div className="space-y-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">My Shops</h1>

          <p className="mt-1 text-sm text-gray-500">Store information you are working at</p>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-6">
          <div className="h-24 animate-pulse rounded-xl bg-gray-100" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">My Shops</h1>

          <p className="mt-1 text-sm text-gray-500">Store information you are working at</p>
        </div>

        <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700">
          {error}
        </div>
      </div>
    );
  }

  if (shops.length === 0) {
    return (
      <div className="space-y-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">My Shops</h1>

          <p className="mt-1 text-sm text-gray-500">Store information you are working at</p>
        </div>

        <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-gray-300 bg-white py-16 text-center">
          <Store className="mb-3 h-10 w-10 text-gray-300" />

          <p className="font-medium text-gray-900">No associated shops</p>

          <p className="mt-1 text-sm text-gray-500">You are not associated with any shops.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">My Shops</h1>

        <p className="mt-1 text-sm text-gray-500">Store information you are working at</p>
      </div>

      {/* Shop list */}
      <div className="space-y-4">
        {shops.map((shop) => (
          <button
            key={shop.id}
            type="button"
            onClick={() => handleViewShop(shop)}
            className="group w-full rounded-2xl border border-gray-200 bg-white p-6 text-left shadow-sm transition hover:border-emerald-200 hover:shadow-md"
          >
            <div className="flex items-center justify-between gap-4">
              <div className="flex min-w-0 items-center gap-4">
                {/* Icon */}
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                  <Store size={24} />
                </div>

                {/* Shop info */}
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="truncate text-lg font-semibold text-gray-900">
                      {shop.shopName}
                    </h2>

                    <span
                      className={`rounded-full border px-2.5 py-1 text-xs font-medium ${getStatusStyle(
                        shop.status,
                      )}`}
                    >
                      {getStatusLabel(shop.status)}
                    </span>
                  </div>

                  <div className="mt-2 flex items-center gap-2 text-sm text-gray-500">
                    <CalendarDays size={15} />

                    <span>Joined from {formatDate(shop.joinedAt)}</span>
                  </div>
                </div>
              </div>

              <ChevronRight
                size={20}
                className="shrink-0 text-gray-400 transition group-hover:translate-x-1 group-hover:text-emerald-600"
              />
            </div>
          </button>
        ))}
      </div>
    </div>
  );
}
