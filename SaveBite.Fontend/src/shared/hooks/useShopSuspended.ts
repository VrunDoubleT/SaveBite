import { useParams } from "react-router-dom";
import { useAuthStore } from "@/shared/stores/authStore";

export function useShopSuspended(shopId?: string): boolean {
  const routeParams = useParams<{ shopId?: string }>();
  const shop = useAuthStore((state) => state.user?.shop);
  const targetShopId = shopId ?? routeParams.shopId;

  return shop?.status === "Suspended" && (!targetShopId || targetShopId === shop.shopId);
}
