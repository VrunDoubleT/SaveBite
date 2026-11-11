import { Menu, PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { IconButton } from "@/shared/components/ui";
import { UserAvatar } from "@/shared/components/avatar/UserAvatar";
import { useAuthStore } from "@/shared/stores/authStore";
import { useUiStore } from "@/shared/stores/uiStore";
import { NotificationBell } from "@/features/notifications/components/NotificationBell";

interface WorkspaceHeaderProps {
  collapsed?: boolean;
  onToggleCollapsed?: () => void;
  subtitle: string;
  title: string;
}

export function WorkspaceHeader({
  subtitle,
  title,
  collapsed,
  onToggleCollapsed,
}: WorkspaceHeaderProps) {
  const toggleSidebar = useUiStore((state) => state.toggleSidebar);
  const isSidebarOpen = useUiStore((state) => state.isSidebarOpen);
  const user = useAuthStore((state) => state.user);

  return (
    <header className="sticky top-0 z-20 h-16 border-b border-border-default bg-bg-surface">
      <div className="flex h-full items-center justify-between px-4 md:px-6">
        <div className="flex items-center gap-3">
          <IconButton
            className="lg:hidden"
            aria-label="Open navigation"
            aria-controls="workspace-navigation"
            aria-expanded={isSidebarOpen}
            onClick={toggleSidebar}
          >
            <Menu className="size-5" aria-hidden="true" />
          </IconButton>
          <IconButton
            className="hidden lg:inline-flex"
            aria-label={collapsed ? "Expand navigation" : "Collapse navigation"}
            aria-controls="workspace-navigation"
            aria-expanded={!collapsed}
            onClick={onToggleCollapsed}
          >
            {collapsed ? (
              <PanelLeftOpen className="size-5" aria-hidden="true" />
            ) : (
              <PanelLeftClose className="size-5" aria-hidden="true" />
            )}
          </IconButton>
          <div>
            <p className="text-sm font-semibold text-neutral-900">{title}</p>
            <p className="hidden text-xs font-medium text-neutral-500 sm:block">{subtitle}</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {user && (
            <>
              <NotificationBell />
              <div className="flex items-center gap-3">
                               <div className="hidden text-right sm:block">
                  <p className="text-sm font-semibold text-neutral-800">{user.fullName}</p>
                  <p className="text-xs font-medium text-neutral-500">{user.email}</p>
                </div>
                <UserAvatar
                  key={user.avatarUrl}
                  fullName={user.fullName}
                  avatarUrl={user.avatarUrl}
                />
              </div>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
