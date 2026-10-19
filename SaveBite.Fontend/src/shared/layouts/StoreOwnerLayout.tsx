import { ShopOrderWorkspaceNotifications } from "@/features/shop-orders/components/ShopOrderWorkspaceNotifications";
import { Store, Users, ClipboardList } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { WorkspaceLayout } from "@/shared/layouts/WorkspaceLayout";

const navigation = [
  {
    to: APP_PATHS.STORE_OWNER,
    label: "Overview",
    end: true,
    icon: Store,
  },
  {
    to: `${APP_PATHS.STORE_OWNER}/orders`, label: "Orders", end: false, icon: ClipboardList,
    badge: <ShopOrderWorkspaceNotifications ordersPath={`${APP_PATHS.STORE_OWNER}/orders`} />,
  },

  {
    to: `${APP_PATHS.STORE_OWNER}/staff`,
    label: "Staff Management",
    end: false,
    icon: Users,
  },
] as const;

export function StoreOwnerLayout() {
  return (
    <WorkspaceLayout
      title="Store owner workspace"
      subtitle="Store management"
      navigationLabel="Store owner navigation"
      navigation={navigation}
    />
  );
}
