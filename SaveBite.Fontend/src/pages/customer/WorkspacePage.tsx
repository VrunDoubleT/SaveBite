import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

import { staffApi } from "@/features/staff/api/staffApi";
import type { AssociatedShop } from "@/features/staff/types/staff.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";

export function WorkspacePage() {
  const navigate = useNavigate();

  const [shops, setShops] = useState<AssociatedShop[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadShops = async () => {
      try {
        const result = await staffApi.getAssociatedShops();
        setShops(result);
      } catch (error) {
        toast.error(getApiErrorMessage(error) || "Failed to load workspace.");
      } finally {
        setIsLoading(false);
      }
    };

    void loadShops();
  }, []);

  if (isLoading) {
    return <p className="text-sm text-text-muted">Loading workspace...</p>;
  }

  if (shops.length === 0) {
    return (
      <div className="rounded-lg border border-border-default bg-bg-surface p-6">
        <h2 className="font-semibold text-text-primary">No stores yet</h2>

        <p className="mt-1 text-sm text-text-secondary">
          Accept a staff invitation to access a store workspace.
        </p>
      </div>
    );
  }

  return (
    <div className="grid gap-4 lg:grid-cols-2">
      {shops.map((shop) => (
        <button
          key={shop.id}
          type="button"
          onClick={() => navigate(`/staff/shops/${shop.shopId}`)}
          className="rounded-lg border border-border-default bg-bg-surface p-5 text-left shadow-sm transition hover:border-primary-300 hover:shadow-md"
        >
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="font-semibold text-text-primary">{shop.shopName}</h2>

              <p className="mt-1 text-sm text-text-secondary">
                Joined {new Date(shop.joinedAt).toLocaleDateString()}
              </p>
            </div>

            <span className="rounded-full bg-primary-50 px-3 py-1 text-xs font-medium text-primary-700">
              {shop.status}
            </span>
          </div>

          <div className="mt-5 text-sm font-medium text-primary-700">Open workspace →</div>
        </button>
      ))}
    </div>
  );
}
