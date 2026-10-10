import { Outlet } from "react-router-dom";
import { WorkspaceHeader } from "@/shared/components/header/WorkspaceHeader";
import {
  WorkspaceSidebar,
  type WorkspaceNavigationItem,
} from "@/shared/components/navigation/WorkspaceSidebar";

interface WorkspaceLayoutProps {
  navigation: readonly WorkspaceNavigationItem[];
  navigationLabel: string;
  subtitle: string;
  title: string;
}

export function WorkspaceLayout({
  navigation,
  navigationLabel,
  subtitle,
  title,
}: WorkspaceLayoutProps) {
  return (
    <div className="min-h-screen bg-bg-body">
      <WorkspaceSidebar ariaLabel={navigationLabel} navigation={navigation} />
      <div className="min-h-screen lg:pl-64">
        <WorkspaceHeader title={title} subtitle={subtitle} />
        <main className="mx-auto w-full px-4 py-8 md:px-8 md:py-12">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
