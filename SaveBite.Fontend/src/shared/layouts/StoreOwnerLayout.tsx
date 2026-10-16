import { Store, Users, Zap } from "lucide-react";
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
    to: APP_PATHS.STORE_OWNER_FLASH_DEALS,
    label: "My Flash Deals",
    end: false,
    icon: Zap,
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
