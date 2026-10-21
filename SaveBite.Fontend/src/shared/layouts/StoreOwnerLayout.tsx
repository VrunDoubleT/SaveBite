import { ShopOrderWorkspaceNotifications } from "@/features/shop-orders/components/ShopOrderWorkspaceNotifications";
import {
  ClipboardList,
  CreditCard,
  Package,
  Settings2,
  Store,
  Users,
} from "lucide-react";
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
    to: `${APP_PATHS.STORE_OWNER}/orders`,
    label: "Orders",
    end: false,
    icon: ClipboardList,
    badge: (
      <ShopOrderWorkspaceNotifications
        ordersPath={`${APP_PATHS.STORE_OWNER}/orders`}
      />
    ),
  },
  {
    to: APP_PATHS.STORE_OWNER_STAFF,
    label: "Staff Management",
    end: false,
    icon: Users,
  },
  {
    to: APP_PATHS.STORE_OWNER_PRODUCTS,
    label: "Products",
    end: false,
    icon: Package,
  },
  {
    to: APP_PATHS.STORE_OWNER_PAYMENT_SETTINGS,
    label: "Payment Settings",
    end: false,
    icon: CreditCard,
  },
  {
    to: APP_PATHS.STORE_OWNER_PROFILE,
    label: "Shop Profile",
    end: false,
    icon: Settings2,
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