import { AppLogo } from "@/shared/components/AppLogo";
import { AuthenticatedHeaderActions } from "@/shared/components/header/AuthenticatedHeaderActions";
import { GuestHeaderActions } from "@/shared/components/header/GuestHeaderActions";
import { useAuthStore } from "@/shared/stores/authStore";
import { useMatch } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";

export function CustomerHeader() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isHome = useMatch({ path: APP_PATHS.HOME, end: true }) !== null;

  return (
    <header
      className={`sticky top-0 z-30 h-16 border-b backdrop-blur-xl ${
        isHome
          ? "border-white/30 bg-white/45"
          : "border-border-default bg-bg-surface"
      }`}
    >
      <div className="mx-auto flex h-full w-full max-w-320 items-center justify-between px-4 md:px-6">
        <AppLogo />
        {isAuthenticated ? <AuthenticatedHeaderActions /> : <GuestHeaderActions />}
      </div>
    </header>
  );
}
