import { AppLogo } from "@/shared/components/AppLogo";
import { AuthenticatedHeaderActions } from "@/shared/components/header/AuthenticatedHeaderActions";
import { GuestHeaderActions } from "@/shared/components/header/GuestHeaderActions";
import { useAuthStore } from "@/shared/stores/authStore";

export function CustomerHeader() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <header className="sticky top-0 z-20 h-16 border-b border-border-default bg-bg-surface">
      <div className="mx-auto flex h-full w-full max-w-300 items-center justify-between px-4 md:px-8">
        <AppLogo />
        {isAuthenticated ? <AuthenticatedHeaderActions /> : <GuestHeaderActions />}
      </div>
    </header>
  );
}
