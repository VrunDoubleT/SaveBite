import { useEffect } from "react";
import { CUSTOMER_ROLES } from "@/app/router/routeAccess";
import { useCartStore } from "@/features/cart/stores/cartStore";
import { subscribeCartEvents } from "@/features/cart/realtime/subscribeCartEvents";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { CustomerSuspensionBanner } from "@/shared/components/CustomerSuspensionBanner";
import { useAuthStore } from "@/shared/stores/authStore";
import { Outlet, useMatch } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { CustomerHeader } from "@/shared/components/header/CustomerHeader";

export function CustomerLayout() {
  const realtime = useRealtimeSubscriptions();
  const userId = useAuthStore((state) => state.user?.id);
  const role = useAuthStore((state) => state.user?.role);
  const cartEnabled = useAuthStore(
    (state) =>
      state.isAuthenticated &&
      !!state.user &&
      CUSTOMER_ROLES.includes(state.user.role) &&
      state.user.customerStatus === "active",
  );
  useEffect(() => {
    if (!cartEnabled || !userId) return;
    const cart = useCartStore.getState();
    cart.setCustomerActive(true);
    const unsubscribe = subscribeCartEvents(realtime);
    // Entering customer pages also refreshes the badge after time spent in a workspace.
    cart.queueRealtimeRefresh();
    return () => {
      unsubscribe();
      useCartStore.getState().setCustomerActive(false);
    };
  }, [cartEnabled, userId, role, realtime]);
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
