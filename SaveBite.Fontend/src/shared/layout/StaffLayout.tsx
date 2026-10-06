import {
  ClipboardList,
  Store,
} from "lucide-react";
import { useParams } from "react-router-dom";

import { APP_PATHS } from "@/app/router/paths";
import { WorkspaceLayout } from "@/shared/layout/WorkspaceLayout";

export function StaffLayout() {
  const { shopId } = useParams<{ shopId: string }>();

  const navigation = [
    {
      to: `${APP_PATHS.STAFF}/shops/${shopId}`,
      label: "Overview",
      end: true,
      icon: ClipboardList,
    },
    {
      to: `${APP_PATHS.STAFF}/shops/${shopId}/info`,
      label: "Shop and Staff Info",
      icon: Store,
    },
  ] as const;

  return (
    <WorkspaceLayout
      title="Staff workspace"
      subtitle="Daily store operations"
      navigationLabel="Staff navigation"
      navigation={navigation}
    />
  );
}