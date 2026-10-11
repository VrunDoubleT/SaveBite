import { Navigate, Outlet, useLocation } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { HOME_PATH_BY_ROLE } from "@/app/router/rolePaths";
import { isAuthRole, useAuthStore, type AuthRole } from "@/shared/stores/authStore";
import { SessionCheckingScreen } from "@/shared/components/SessionCheckingScreen";

interface ProtectedRouteProps {
  allowedRoles: readonly AuthRole[];
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isInitializing = useAuthStore((state) => state.isInitializing);
  const user = useAuthStore((state) => state.user);
  const location = useLocation();

  if (isInitializing) {
    return <SessionCheckingScreen />;
  }

  // A token without user information is not enough to authorize a route.
  if (!isAuthenticated || !user || !isAuthRole(user.role)) {
    return <Navigate to={APP_PATHS.LOGIN} replace state={{ from: location }} />;
  }

  if (!allowedRoles.includes(user.role)) {
    return <Navigate to={HOME_PATH_BY_ROLE[user.role]} replace />;
  }

  return <Outlet />;
}
