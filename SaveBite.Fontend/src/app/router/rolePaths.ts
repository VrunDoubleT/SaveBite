import { APP_PATHS } from "@/app/router/paths";
import type { AuthRole } from "@/features/auth/types/auth.types";

export const HOME_PATH_BY_ROLE: Record<AuthRole, string> = {
  user: APP_PATHS.HOME,
  staff: APP_PATHS.HOME,
  storeOwner: APP_PATHS.HOME,
  admin: APP_PATHS.ADMIN,
};
