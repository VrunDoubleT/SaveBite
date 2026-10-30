import {
  ClipboardCheck,
  LayoutDashboard,
  Package,
  ShieldCheck,
  ShoppingBag,
  Store,
  Users,
  Tags,
  MessageSquare,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { WorkspaceLayout } from "@/shared/layouts/WorkspaceLayout";

const navigation = [
  {
    to: APP_PATHS.ADMIN,
    label: "Overview",
    end: true,
    icon: LayoutDashboard,
  },
  {
    to: APP_PATHS.ADMIN_USERS,
    label: "Users",
    icon: Users,
  },
  {
    to: APP_PATHS.ADMIN_CATEGORIES,
    label: "Categories",
    icon: Tags,
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
    to: APP_PATHS.ADMIN_TRUST,
    label: "Trust Score",
    icon: ShieldCheck,
  },
  {
    to: APP_PATHS.ADMIN_SHOP_APPLICATIONS,
    label: "Store Applications",
    icon: ClipboardCheck,
  },
  {
    to: APP_PATHS.ADMIN_FEEDBACK,
    label: "Feedback",
    icon: MessageSquare,
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
