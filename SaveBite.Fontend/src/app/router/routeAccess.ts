import type { AuthRole } from "@/features/auth/types/auth.types";

export const CUSTOMER_ROLES: readonly AuthRole[] = ["user", "staff", "storeOwner"];
export const STAFF_ROLES: readonly AuthRole[] = ["staff", "storeOwner"];
export const STORE_OWNER_ROLES: readonly AuthRole[] = ["storeOwner"];
export const ADMIN_ROLES: readonly AuthRole[] = ["admin"];
