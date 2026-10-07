import { useEffect } from "react";
import { LogOut, type LucideIcon } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";

import { AppLogo } from "@/shared/components/AppLogo";
import { NavItem } from "@/shared/components/navigation/NavItem";
import { useAuthStore } from "@/shared/stores/authStore";
import { useUiStore } from "@/shared/stores/uiStore";

export interface WorkspaceNavigationItem {
  end?: boolean;
  icon: LucideIcon;
  label: string;
  to: string;
}

interface WorkspaceSidebarProps {
  ariaLabel: string;
  navigation: readonly WorkspaceNavigationItem[];
}

export function WorkspaceSidebar({ ariaLabel, navigation }: WorkspaceSidebarProps) {
  const isOpen = useUiStore((state) => state.isSidebarOpen);
  const setOpen = useUiStore((state) => state.setSidebarOpen);
  const logout = useAuthStore((state) => state.logout);
  const isLoggingOut = useAuthStore((state) => state.isLoggingOut);
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => setOpen(false), [location.pathname, setOpen]);
  async function handleLogout() {
    try {
      await logout();
    } finally {
      navigate("/login", {
        replace: true,
      });
    }
  }

  return (
    <>
      {isOpen && (
        <button
          type="button"
          aria-label="Close navigation"
          className="fixed inset-0 z-30 bg-neutral-900/40 lg:hidden"
          onClick={() => setOpen(false)}
        />
      )}
      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-neutral-200 bg-white px-4 py-4 transition-transform lg:translate-x-0 ${
          isOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="flex h-12 items-center px-2">
          <AppLogo />
        </div>
        <nav className="mt-8 flex-1 space-y-1" aria-label={ariaLabel}>
          {navigation.map(({ icon: Icon, ...item }) => (
            <NavItem
              key={item.to}
              {...item}
              icon={<Icon className="size-5" aria-hidden="true" />}
            />
          ))}
        </nav>
        <button
          type="button"
          disabled={isLoggingOut}
          onClick={() => {
            void handleLogout();
          }}
          className="mt-4 flex min-h-11 w-full items-center justify-center gap-2 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm font-semibold text-red-600 transition-colors hover:border-red-600 hover:bg-red-600 hover:text-white focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-red-200 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <LogOut className="size-5" aria-hidden="true" />
          {isLoggingOut ? "Signing out..." : "Sign out"}
        </button>
      </aside>
    </>
  );
}
