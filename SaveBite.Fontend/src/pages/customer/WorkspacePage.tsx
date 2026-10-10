import { useEffect, useState } from "react";
import { LogOut, X } from "lucide-react";
import { useNavigate } from "react-router-dom";

import { staffApi } from "@/features/staff-shops/api/staffApi";
import type { AssociatedShop } from "@/features/staff-shops/types/staff.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";

export function WorkspacePage() {
  const navigate = useNavigate();

  const [shops, setShops] = useState<AssociatedShop[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const [leavingShopId, setLeavingShopId] = useState<string | null>(null);

  const [shopToLeave, setShopToLeave] =
    useState<AssociatedShop | null>(null);

  useEffect(() => {
    const loadShops = async () => {
      try {
        const result = await staffApi.getAssociatedShops();
        setShops(result);
      } catch (error) {
        toast.error(
          getApiErrorMessage(error) ||
            "Failed to load workspace.",
        );
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

      setShops((currentShops) =>
        currentShops.filter(
          (shop) =>
            shop.shopId !== shopToLeave.shopId,
        ),
      );

      toast.success(
        `You have left ${shopToLeave.shopName} successfully.`,
      );

      setShopToLeave(null);
    } catch (error) {
      toast.error(
        getApiErrorMessage(error) ||
          "Failed to leave shop.",
      );
    } finally {
      setLeavingShopId(null);
    }
  }

  if (isLoading) {
    return (
      <p className="text-sm text-text-muted">
        Loading workspace...
      </p>
    );
  }

  return (
    <>
      {shops.length === 0 ? (
        <div className="rounded-xl border border-border-default bg-bg-surface p-6">
          <h2 className="font-semibold text-text-primary">
            No stores yet
          </h2>

          <p className="mt-1 text-sm text-text-secondary">
            Accept a staff invitation to access a store workspace.
          </p>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          {shops.map((shop) => {
            const isLeaving =
              leavingShopId === shop.shopId;

            return (
              <div
                key={shop.id}
                className="rounded-xl border border-border-default bg-bg-surface p-5 shadow-sm transition hover:border-primary-300 hover:shadow-md"
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <h2 className="font-semibold text-text-primary">
                      {shop.shopName}
                    </h2>

                    <p className="mt-1 text-sm text-text-secondary">
                      Joined{" "}
                      {new Date(
                        shop.joinedAt,
                      ).toLocaleDateString()}
                    </p>
                  </div>

                  <span className="rounded-full bg-primary-50 px-3 py-1 text-xs font-medium text-primary-700">
                    {shop.status}
                  </span>
                </div>

                <div className="mt-5 flex items-center justify-between border-t border-border-default pt-4">
                  <button
                    type="button"
                    onClick={() =>
                      navigate(
                        `/staff/shops/${shop.shopId}`,
                      )
                    }
                    disabled={isLeaving}
                    className="text-sm font-medium text-primary-700 transition hover:text-primary-800 disabled:opacity-50"
                  >
                    Open workspace →
                  </button>

                  <button
                    type="button"
                    onClick={() =>
                      setShopToLeave(shop)
                    }
                    disabled={isLeaving}
                    className="inline-flex items-center gap-2 rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-600 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <LogOut size={16} />

                    {isLeaving
                      ? "Leaving..."
                      : "Leave shop"}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* LEAVE SHOP CONFIRM MODAL */}
      {shopToLeave && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4">
          <div className="w-full max-w-md rounded-2xl bg-white shadow-2xl">
            {/* Header */}
            <div className="flex items-center justify-between border-b border-gray-100 px-6 py-4">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">
                  Leave shop
                </h2>

                <p className="mt-1 text-sm text-gray-500">
                  This action will remove your staff access.
                </p>
              </div>

              <button
                type="button"
                onClick={() =>
                  setShopToLeave(null)
                }
                disabled={
                  leavingShopId === shopToLeave.shopId
                }
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
                  <span className="font-semibold">
                    {shopToLeave.shopName}
                  </span>
                  ?
                </p>

                <p className="mt-2 text-sm leading-6 text-red-700">
                  You will immediately lose access to this
                  shop. If you want to join again, the shop
                  owner must invite you again.
                </p>
              </div>
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-3 border-t border-gray-100 px-6 py-4">
              <button
                type="button"
                onClick={() =>
                  setShopToLeave(null)
                }
                disabled={
                  leavingShopId === shopToLeave.shopId
                }
                className="rounded-xl border border-gray-200 px-4 py-2.5 text-sm font-medium text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Cancel
              </button>

              <button
                type="button"
                onClick={() =>
                  void handleConfirmLeaveShop()
                }
                disabled={
                  leavingShopId === shopToLeave.shopId
                }
                className="inline-flex items-center gap-2 rounded-xl bg-red-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <LogOut size={16} />

                {leavingShopId === shopToLeave.shopId
                  ? "Leaving..."
                  : "Leave shop"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}