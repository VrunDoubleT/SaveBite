import { ClipboardCheck, LayoutDashboard, Package, ShoppingBag, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { WorkspaceLayout } from "@/shared/layout/WorkspaceLayout";

const navigation = [
  {
    to: APP_PATHS.ADMIN,
    label: "Overview",
    end: true,
    icon: LayoutDashboard,
  },
  {
    to: APP_PATHS.ADMIN_PRODUCTS,
    label: "Products",
    icon: Package,
  },
  {
    to: APP_PATHS.ADMIN_ORDERS,
    label: "Orders",
    icon: ShoppingBag,
  },
  {
    to: APP_PATHS.ADMIN_STORES,
    label: "Stores",
    icon: Store,
  },
  { 
    to: APP_PATHS.ADMIN_SHOP_APPLICATIONS, 
    label: "Store Applications", 
    icon: ClipboardCheck, 
  },
] as const;

export function AdminLayout() {
  return (
    <WorkspaceLayout
      title="Admin workspace"
      subtitle="Smart food management"
      navigationLabel="Admin navigation"
      navigation={navigation}
    />
  );
}
