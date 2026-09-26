import { ClipboardList } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { WorkspaceLayout } from "@/shared/layout/WorkspaceLayout";

const navigation = [
  {
    to: APP_PATHS.STAFF,
    label: "Overview",
    end: true,
    icon: ClipboardList,
  },
] as const;

export function StaffLayout() {
  return (
    <WorkspaceLayout
      title="Staff workspace"
      subtitle="Daily store operations"
      navigationLabel="Staff navigation"
      navigation={navigation}
    />
  );
}
