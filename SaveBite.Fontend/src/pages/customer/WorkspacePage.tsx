import { useEffect, useState } from "react";
import { LogOut, Store, X } from "lucide-react";
import { useNavigate } from "react-router-dom";

import { staffApi } from "@/features/staff-shops/api/staffApi";
import type { AssociatedShop } from "@/features/staff-shops/types/staff.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import { Pagination, usePagination } from "@/shared/components/pagination";

export function WorkspacePage() {
  const navigate = useNavigate();

  const [shops, setShops] = useState<AssociatedShop[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const [leavingShopId, setLeavingShopId] = useState<string | null>(null);
  const [shopToLeave, setShopToLeave] = useState<AssociatedShop | null>(null);

  const {
    currentPage,
    setCurrentPage,
    totalPages,
    paginatedItems: paginatedShops,
    totalItems,
  } = usePagination({
    items: shops,
    initialPageSize: 5,
  });

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

  async function handleConfirmLeaveShop() {
    if (!shopToLeave) return;

    try {
      setLeavingShopId(shopToLeave.shopId);

      await staffApi.leaveShop(shopToLeave.shopId);

      setShops((currentShops) => currentShops.filter((shop) => shop.shopId !== shopToLeave.shopId));

      toast.success(`You have left ${shopToLeave.shopName} successfully.`);

      setShopToLeave(null);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to leave shop.");
    } finally {
      setLeavingShopId(null);
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[1, 2].map((item) => (
          <div
            key={item}
            className="h-24 animate-pulse rounded-2xl border border-gray-100 bg-white p-5 shadow-sm"
          />
        ))}
      </div>
    );
  }

  return (
    <>
      {shops.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-gray-300 bg-white py-16 text-center">
          <Store className="mx-auto mb-3 h-10 w-10 text-gray-300" />
          <h2 className="font-semibold text-gray-900">No stores yet</h2>

          <p className="mt-1 text-sm text-gray-500">
            Accept a staff invitation to access a store workspace.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {paginatedShops.map((shop) => {
            const isLeaving = leavingShopId === shop.shopId;

            return (
              <div
                key={shop.id}
                className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm transition hover:border-emerald-300 hover:shadow-md"
              >
                <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
                  {/* Store info */}
                  <div className="flex items-center gap-4">
                    <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                      <Store size={24} />
                    </div>

                    <div>
                      <div className="flex flex-wrap items-center gap-2.5">
                        <h2 className="text-base font-semibold text-gray-900">{shop.shopName}</h2>

                        <span
                          className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 ring-inset ${
                            shop.status.toLowerCase() === "active"
                              ? "bg-emerald-50 text-emerald-700 ring-emerald-600/20"
                              : "bg-gray-100 text-gray-600 ring-gray-500/10"
                          }`}
                        >
                          {shop.status}
                        </span>
                      </div>

                      <p className="mt-1 text-sm text-gray-500">
                        Joined{" "}
                        {new Date(shop.joinedAt).toLocaleDateString("en-US", {
                          year: "numeric",
                          month: "short",
                          day: "numeric",
                        })}
                      </p>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-3 pt-2 sm:pt-0">
                    <button
                      type="button"
                      onClick={() => navigate(`/staff/shops/${shop.shopId}`)}
                      disabled={isLeaving}
                      className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-emerald-700 disabled:opacity-50"
                    >
                      Open workspace →
                    </button>

                    <button
                      type="button"
                      onClick={() => setShopToLeave(shop)}
                      disabled={isLeaving}
                      className="inline-flex items-center gap-2 rounded-xl border border-red-200 bg-white px-3.5 py-2.5 text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      <LogOut size={16} />

                      {isLeaving ? "Leaving..." : "Leave shop"}
                    </button>
                  </div>
                </div>
              </div>
            );
          })}

          {totalPages > 1 && (
            <div className="pt-2">
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                totalItems={totalItems}
                pageSize={5}
                itemLabel="stores"
                onPageChange={setCurrentPage}
              />
            </div>
          )}
        </div>
      )}

      {/* LEAVE SHOP CONFIRM MODAL */}
      {shopToLeave && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4">
          <div className="w-full max-w-md rounded-2xl bg-white shadow-2xl">
            {/* Header */}
            <div className="flex items-center justify-between border-b border-gray-100 px-6 py-4">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">Leave shop</h2>

                <p className="mt-1 text-sm text-gray-500">
                  This action will remove your staff access.
                </p>
              </div>

              <button
                type="button"
                onClick={() => setShopToLeave(null)}
                disabled={leavingShopId === shopToLeave.shopId}
                className="rounded-lg p-2 text-gray-400 transition hover:bg-gray-100 hover:text-gray-700"
              >
                <X size={18} />
              </button>
            </div>

            {/* Content */}
            <div className="px-6 py-5">
              <div className="rounded-xl bg-red-50 p-4">
                <p className="text-sm font-medium text-red-800">
                  Are you sure you want to leave{" "}
                  <span className="font-semibold">{shopToLeave.shopName}</span>?
                </p>

                <p className="mt-2 text-sm leading-6 text-red-700">
                  You will immediately lose access to this shop. If you want to join again, the shop
                  owner must invite you again.
                </p>
              </div>
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-3 border-t border-gray-100 px-6 py-4">
              <button
                type="button"
                onClick={() => setShopToLeave(null)}
                disabled={leavingShopId === shopToLeave.shopId}
                className="rounded-xl border border-gray-200 px-4 py-2.5 text-sm font-medium text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Cancel
              </button>

              <button
                type="button"
                onClick={() => void handleConfirmLeaveShop()}
                disabled={leavingShopId === shopToLeave.shopId}
                className="inline-flex items-center gap-2 rounded-xl bg-red-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <LogOut size={16} />

                {leavingShopId === shopToLeave.shopId ? "Leaving..." : "Leave shop"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
