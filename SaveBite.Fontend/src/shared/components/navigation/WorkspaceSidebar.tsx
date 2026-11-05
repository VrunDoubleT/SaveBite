import { useEffect, useSyncExternalStore, type ReactNode } from "react";
import { LogOut, type LucideIcon } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { APP_LOGO_URL, AppLogo } from "@/shared/components/AppLogo";
import { NavItem } from "@/shared/components/navigation/NavItem";
import { Button, Drawer } from "@/shared/components/ui";
import { useAuthStore } from "@/shared/stores/authStore";
import { useUiStore } from "@/shared/stores/uiStore";

export interface WorkspaceNavigationItem {
  badge?: ReactNode;
  end?: boolean;
  icon: LucideIcon;
  label: string;
  to: string;
}
interface WorkspaceSidebarProps {
  ariaLabel: string;
  navigation: readonly WorkspaceNavigationItem[];
  collapsed?: boolean;
}
const desktopQuery = "(min-width: 1024px)";
function subscribeDesktop(callback: () => void) {
  const query = window.matchMedia(desktopQuery);
  query.addEventListener("change", callback);
  return () => query.removeEventListener("change", callback);
}
function getDesktopSnapshot() {
  return window.matchMedia(desktopQuery).matches;
}

export function WorkspaceSidebar({
  ariaLabel,
  navigation,
  collapsed = false,
}: WorkspaceSidebarProps) {
  const isOpen = useUiStore((state) => state.isSidebarOpen);
  const setOpen = useUiStore((state) => state.setSidebarOpen);
  const logout = useAuthStore((state) => state.logout);
  const isLoggingOut = useAuthStore((state) => state.isLoggingOut);
  const location = useLocation();
  const navigate = useNavigate();
  const isDesktop = useSyncExternalStore(subscribeDesktop, getDesktopSnapshot, () => false);
  const compact = isDesktop && collapsed;
  useEffect(() => setOpen(false), [location.pathname, setOpen]);
  async function handleLogout() {
    try {
      await logout();
    } finally {
      navigate("/login", { replace: true });
    }
  }
  const content = (
    <>
      <div className={`flex h-12 shrink-0 items-center ${compact ? "justify-center" : "px-2"}`}>
        {compact ? (
          <Link
            to="/"
            aria-label="SaveBite - Home"
            title="SaveBite"
            className="flex size-9 items-center justify-center overflow-hidden rounded-md"
          >
            <img src={APP_LOGO_URL} alt="" aria-hidden="true" className="size-full object-cover" />
          </Link>
        ) : (
          <AppLogo />
        )}
      </div>
      <nav className="mt-6 min-h-0 flex-1 space-y-1 overflow-y-auto py-1" aria-label={ariaLabel}>
        {navigation.map(({ icon: Icon, ...item }) => (
          <NavItem
            key={item.to}
            {...item}
            collapsed={compact}
            icon={<Icon className="size-5" aria-hidden="true" />}
          />
        ))}
      </nav>
      <Button
        variant="ghost"
        loading={isLoggingOut}
        disabled={isLoggingOut}
        onClick={() => {
          void handleLogout();
        }}
        aria-label={compact ? (isLoggingOut ? "Signing out..." : "Sign out") : undefined}
        title={compact ? "Sign out" : undefined}
        className={`mt-4 w-full text-danger-strong hover:bg-danger-soft ${compact ? "px-0" : ""}`}
      >
        {!isLoggingOut && <LogOut className="size-5" aria-hidden="true" />}
        <span className={compact ? "sr-only" : ""}>
          {isLoggingOut ? "Signing out..." : "Sign out"}
        </span>
      </Button>
    </>
  );
  return isDesktop ? (
    <aside
      id="workspace-navigation"
      className={`fixed inset-y-0 left-0 z-30 flex flex-col border-r border-border-default bg-bg-surface py-4 ${compact ? "w-20 px-2" : "w-64 px-4"}`}
    >
      {content}
    </aside>
  ) : (
    <Drawer isOpen={isOpen} onClose={() => setOpen(false)} title={ariaLabel}>
      <div id="workspace-navigation" className="flex min-h-[calc(100dvh-10rem)] flex-col">
        {content}
      </div>
    </Drawer>
  );
}
