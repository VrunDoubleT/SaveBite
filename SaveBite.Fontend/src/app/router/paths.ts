export const APP_PATHS = {
  HOME: "/",
  LOGIN: "/login",
  REGISTER: "/register",
  REGISTER_VERIFY: "/register/verify",
  FORGOT_PASSWORD: "/forgot-password",
  RESET_PASSWORD_VERIFY: "/forgot-password/verify",
  CUSTOMER_LEGACY: "/customer",
  CART: "/cart",
  ACCOUNT: "/account",
  ADMIN: "/admin",
  ADMIN_PRODUCTS: "/admin/products",
  ADMIN_ORDERS: "/admin/orders",
  ADMIN_STORES: "/admin/stores",
  STAFF: "/staff",
  STORE_OWNER: "/store-owner",
  NOT_FOUND: "*",
} as const;

export type AppPath = (typeof APP_PATHS)[keyof typeof APP_PATHS];
