import { useAuthStore } from "@/shared/stores/authStore";
import { useUiStore } from "@/shared/stores/uiStore";

interface WorkspaceHeaderProps {
  subtitle: string;
  title: string;
}

export function WorkspaceHeader({ subtitle, title }: WorkspaceHeaderProps) {
  const toggleSidebar = useUiStore((state) => state.toggleSidebar);
  const user = useAuthStore((state) => state.user);

  return (
    <header className="sticky top-0 z-20 h-16 border-b border-neutral-200 bg-white">
      <div className="flex h-full items-center justify-between px-4 md:px-8">
        <div className="flex items-center gap-3">
          <button
            type="button"
            className="flex size-10 items-center justify-center rounded-md border border-neutral-200 text-neutral-600 transition-colors hover:bg-neutral-50 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-400/30 lg:hidden"
            aria-label="Open navigation"
            onClick={toggleSidebar}
          >
            <svg viewBox="0 0 24 24" className="size-5 fill-none stroke-current stroke-2">
              <path d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <div>
            <p className="text-sm font-semibold text-neutral-900">{title}</p>
            <p className="hidden text-xs font-medium text-neutral-500 sm:block">{subtitle}</p>
          </div>
        </div>

        {user && (
          <div className="flex items-center gap-3">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-semibold text-neutral-800">{user.fullName}</p>
              <p className="text-xs font-medium text-neutral-500">{user.email}</p>
            </div>
            <span className="flex size-9 items-center justify-center rounded-full bg-primary-50 text-sm font-bold text-primary-700">
              {user.fullName.charAt(0).toUpperCase()}
            </span>
          </div>
        )}
      </div>
    </header>
  );
}
