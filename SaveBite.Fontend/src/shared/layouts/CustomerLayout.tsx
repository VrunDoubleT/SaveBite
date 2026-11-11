import { CustomerSuspensionBanner } from "@/shared/components/CustomerSuspensionBanner";
import { useAuthStore } from "@/shared/stores/authStore";
import { Outlet, useMatch } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { CustomerHeader } from "@/shared/components/header/CustomerHeader";

export function CustomerLayout() {
  const customerSuspended = useAuthStore(
    (state) => state.isAuthenticated && state.user?.customerStatus === "suspended",
  );
  const isHome = useMatch({ path: APP_PATHS.HOME, end: true }) !== null;
  return (
    <div className="min-h-screen bg-bg-body">
      <CustomerHeader />
      <main
        className={isHome ? "w-full min-w-0" : "mx-auto w-full max-w-320 px-4 pb-4 md:px-6 md:pb-6"}
      >
        {!isHome && customerSuspended && (
          <div className="mt-4">
            <CustomerSuspensionBanner />
          </div>
        )}
        <Outlet />
      </main>
    </div>
  );
}
