import { Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { WorkspaceLayout } from "@/shared/layout/WorkspaceLayout";

const navigation = [
  {
    to: APP_PATHS.STORE_OWNER,
    label: "Overview",
    end: true,
    icon: Store,
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
