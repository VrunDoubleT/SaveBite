import { useState, type ReactNode } from "react";
import { Outlet } from "react-router-dom";
import { WorkspaceHeader } from "@/shared/components/header/WorkspaceHeader";
import {
  WorkspaceSidebar,
  type WorkspaceNavigationItem,
} from "@/shared/components/navigation/WorkspaceSidebar";

interface WorkspaceLayoutProps {
  children?: ReactNode;
  navigation: readonly WorkspaceNavigationItem[];
  navigationLabel: string;
  subtitle: string;
  title: string;
}

export function WorkspaceLayout({
  children,
  navigation,
  navigationLabel,
  subtitle,
  title,
}: WorkspaceLayoutProps) {
  const [collapsed, setCollapsed] = useState(false);
  return (
    <div className="min-h-screen bg-bg-body">
      <WorkspaceSidebar ariaLabel={navigationLabel} navigation={navigation} collapsed={collapsed} />
      <div className={`min-h-screen transition-[padding] ${collapsed ? "lg:pl-20" : "lg:pl-64"}`}>
        <WorkspaceHeader
          title={title}
          subtitle={subtitle}
          collapsed={collapsed}
          onToggleCollapsed={() => setCollapsed((value) => !value)}
        />
        <main className="mx-auto w-full min-w-0 px-4 py-4 md:p-6">{children ?? <Outlet />}</main>
      </div>
    </div>
  );
}
