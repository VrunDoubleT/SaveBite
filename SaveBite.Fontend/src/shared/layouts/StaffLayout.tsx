import { RestockWorkspaceProvider } from "@/features/restock/RestockWorkspaceProvider";
import { RestockBadge } from "@/features/restock/RestockBadge";
import { ShopOrderWorkspaceNotifications } from "@/features/shop-orders/components/ShopOrderWorkspaceNotifications";
import { ClipboardList, PackagePlus, Star, Store, Zap } from "lucide-react";
import { useParams } from "react-router-dom";

import { APP_PATHS } from "@/app/router/paths";
import { WorkspaceLayout } from "@/shared/layouts/WorkspaceLayout";

export function StaffLayout() {
  const { shopId } = useParams<{ shopId: string }>();

  const navigation = [
    {
      to: `${APP_PATHS.STAFF}/shops/${shopId}/flash-deals`,
      label: "Flash Deals",
      end: false,
      icon: Zap,
    },
    {
      to: `${APP_PATHS.STAFF}/shops/${shopId}/restock`,
      label: "Restock",
      end: false,
      icon: PackagePlus,
      badge: <RestockBadge />,
    },
    {
      to: `${APP_PATHS.STAFF}/shops/${shopId}/orders`,
      label: "Orders",
      badge: (
        <ShopOrderWorkspaceNotifications
          shopId={shopId}
          ordersPath={`${APP_PATHS.STAFF}/shops/${shopId}/orders`}
        />
      ),
      end: false,
      icon: ClipboardList,
    },
    {
      to: `${APP_PATHS.STAFF}/shops/${shopId}/reviews`,
      label: "Reviews",
      end: false,
      icon: Star,
    },
    {
      to: `${APP_PATHS.STAFF}/shops/${shopId}/info`,
      label: "Shop and Staff Info",
      icon: Store,
    },
  ] as const;

  return (
    <RestockWorkspaceProvider shopId={shopId}>
      <WorkspaceLayout
        title="Staff workspace"
        subtitle="Daily store operations"
        navigationLabel="Staff navigation"
        shopId={shopId}
        navigation={navigation}
      />
    </RestockWorkspaceProvider>
  );
}
