import { AssociatedShopImage } from "@/features/staff-shops/components/AssociatedShopImage";
import { useEffect, useState } from "react";
import { ArrowRight, LogOut, Store } from "lucide-react";
import { useNavigate } from "react-router-dom";
import {
  Badge,
  Button,
  Card,
  EmptyState,
  ErrorState,
  Modal,
  Skeleton,
} from "@/shared/components/ui";
import { staffApi } from "@/features/staff-shops/api/staffApi";
import type { AssociatedShop } from "@/features/staff-shops/types/staff.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import { AccountPagination as Pagination, usePagination } from "@/shared/components/pagination";

export function WorkspacePage() {
  const navigate = useNavigate();
  const [shops, setShops] = useState<AssociatedShop[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  const [leavingShopId, setLeavingShopId] = useState<string | null>(null);
  const [shopToLeave, setShopToLeave] = useState<AssociatedShop | null>(null);
  const [leaveError, setLeaveError] = useState("");
  const {
    currentPage,
    setCurrentPage,
    totalPages,
    paginatedItems: paginatedShops,
    totalItems,
  } = usePagination({ items: shops, initialPageSize: 5 });
  useEffect(() => {
    let active = true;
    void staffApi
      .getAssociatedShops()
      .then((result) => {
        if (active) {
          setShops(result);
          setError("");
        }
      })
      .catch((e: unknown) => {
        if (active) setError(getApiErrorMessage(e));
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });
    return () => {
      active = false;
    };
  }, [attempt]);
  async function handleConfirmLeaveShop() {
    if (!shopToLeave || leavingShopId) return;
    try {
      setLeavingShopId(shopToLeave.shopId);
      setLeaveError("");
      await staffApi.leaveShop(shopToLeave.shopId);
      setShops((current) => current.filter((shop) => shop.shopId !== shopToLeave.shopId));
      toast.success(`You have left ${shopToLeave.shopName} successfully.`);
      setShopToLeave(null);
    } catch (e) {
      setLeaveError(getApiErrorMessage(e));
    } finally {
      setLeavingShopId(null);
    }
  }
  return (
    <div className="min-w-0 space-y-4">
      {error && (
        <ErrorState
          title="Couldn't load workspaces"
          description={error}
          onRetry={() => {
            setError("");
            setIsLoading(true);
            setAttempt((n) => n + 1);
          }}
        />
      )}
      {isLoading ? (
        <div role="status" aria-label="Loading workspaces" className="space-y-3">
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
        </div>
      ) : !error && !shops.length ? (
        <EmptyState
          icon={Store}
          title="No stores yet"
          description="Accept a staff invitation to access a store workspace."
        />
      ) : (
        paginatedShops.map((shop) => (
          <Card
            padding="compact"
            key={shop.id}
            className="flex flex-wrap items-center justify-between gap-3"
          >
            <div className="flex min-w-0 items-center gap-3">
              <AssociatedShopImage shop={shop} />
              <div className="min-w-0">
                <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {shop.shopName}
                </h2>
                <div className="mt-1 flex flex-wrap items-center gap-2">
                  <Badge tone={shop.status.toLowerCase() === "active" ? "primary" : "neutral"}>
                    {shop.status}
                  </Badge>
                  <p className="text-caption text-text-secondary">
                    Joined{" "}
                    {new Date(shop.joinedAt).toLocaleDateString("en-US", {
                      year: "numeric",
                      month: "short",
                      day: "numeric",
                    })}
                  </p>
                </div>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-2 sm:ml-auto">
              <Button
                variant="primary"
                onClick={() => navigate(`/staff/shops/${shop.shopId}`)}
                disabled={leavingShopId === shop.shopId}
                className="min-h-11"
              >
                Open workspace
                <ArrowRight size={16} aria-hidden="true" />
              </Button>
              <Button
                variant="ghost"
                onClick={() => {
                  setLeaveError("");
                  setShopToLeave(shop);
                }}
                disabled={leavingShopId === shop.shopId}
                className="min-h-11 !text-danger-strong"
              >
                <LogOut size={16} aria-hidden="true" />
                Leave shop
              </Button>
            </div>
          </Card>
        ))
      )}
      {totalItems > 0 && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          totalItems={totalItems}
          pageSize={5}
          itemLabel="stores"
          onPageChange={setCurrentPage}
        />
      )}
      <Modal
        isOpen={!!shopToLeave}
        onClose={() => {
          if (!leavingShopId) setShopToLeave(null);
        }}
        closeOnClickOutside={!leavingShopId}
        title="Leave shop"
      >
        <p className="text-body text-text-secondary">
          Leave <strong className="text-text-primary">{shopToLeave?.shopName}</strong>? You will
          immediately lose staff access. To join again, the shop owner must invite you again.
        </p>
        {leaveError && (
          <p
            role="alert"
            className="mt-3 rounded-md bg-danger-soft p-3 text-body text-danger-strong"
          >
            {leaveError}
          </p>
        )}
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button
            variant="secondary"
            disabled={!!leavingShopId}
            onClick={() => setShopToLeave(null)}
            className="min-h-11"
          >
            Cancel
          </Button>
          <Button
            variant="danger"
            loading={!!leavingShopId}
            onClick={() => void handleConfirmLeaveShop()}
            className="min-h-11"
          >
            Leave shop
          </Button>
        </div>
      </Modal>
    </div>
  );
}
