import { Navigate, Outlet } from "react-router-dom";
import { CUSTOMER_ROLES } from "@/app/router/routeAccess";
import { HOME_PATH_BY_ROLE } from "@/app/router/rolePaths";
import { isAuthRole, useAuthStore } from "@/shared/stores/authStore";
import { SessionCheckingScreen } from "@/shared/components/SessionCheckingScreen";

export function CustomerRoute() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isInitializing = useAuthStore((state) => state.isInitializing);
  const user = useAuthStore((state) => state.user);

  if (isInitializing) {
    return <SessionCheckingScreen />;
  }

  // Customer pages are shared by customers, staff, and store owners. Admin has
  // a separate workspace and must not render the customer layout.
  if (isAuthenticated && user && isAuthRole(user.role) && !CUSTOMER_ROLES.includes(user.role)) {
    return <Navigate to={HOME_PATH_BY_ROLE[user.role]} replace />;
  }

  return <Outlet />;
}
