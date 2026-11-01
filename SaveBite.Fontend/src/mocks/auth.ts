import type { AuthRole, CurrentUserResponse, TokenPair } from "@/features/auth/types/auth.types";
import { id, object, payload, bodyFor, patch, type Json } from "./db";
export function role(): AuthRole {
  const value = sessionStorage.getItem("savebite-mock-role");
  return value === "admin" || value === "staff" || value === "storeOwner" ? value : "user";
}
export function tokens(): TokenPair {
  const exp = Math.floor(Date.now() / 1000) + 86400;
  return {
    accessToken: `${btoa('{"alg":"none"}')}.${btoa(JSON.stringify({ sub: id(0), role: role(), exp }))}.mock`,
    refreshToken: "mock-refresh-token",
    accessTokenExpiresAt: new Date(exp * 1000).toISOString(),
    refreshTokenExpiresAt: new Date((exp + 86400) * 1000).toISOString(),
  };
}
export function currentUser(): CurrentUserResponse {
  const data = payload(bodyFor("auth-authApi_getMe_2"));
  const base: CurrentUserResponse = {
    id: id(0),
    fullName: "Nguyễn Minh Anh",
    email: "minhanh@example.test",
    phone: "0901234567",
    avatarUrl: null,
    role: role(),
    customerStatus: "active",
    defaultAddress: {
      id: id(0),
      label: "Nhà riêng",
      addressLine: "12 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
      ward: "Bến Nghé",
      district: "Quận 1",
      city: "TP. Hồ Chí Minh",
      latitude: 10.7769,
      longitude: 106.7009,
      isDefault: true,
    },
  };
  if (object(data)) {
    for (const key of ["fullName", "email", "phone", "avatarUrl"] as const) {
      const value = data[key];
      if (typeof value === "string") base[key] = value;
    }
  }
  return base;
}
/** Isolate preview credentials from the real browser session without changing auth services/stores. */
export function installMockSession() {
  const get = Storage.prototype.getItem,
    set = Storage.prototype.setItem,
    remove = Storage.prototype.removeItem;
  const scoped = (key: string) =>
    key === "savebite-access-token" || key === "savebite-refresh-token";
  Storage.prototype.getItem = function (key: string) {
    return this === localStorage && scoped(key)
      ? get.call(sessionStorage, `mock:${key}`)
      : get.call(this, key);
  };
  Storage.prototype.setItem = function (key: string, value: string) {
    if (this === localStorage && scoped(key)) set.call(sessionStorage, `mock:${key}`, value);
    else set.call(this, key, value);
  };
  Storage.prototype.removeItem = function (key: string) {
    if (this === localStorage && scoped(key)) remove.call(sessionStorage, `mock:${key}`);
    else remove.call(this, key);
  };
  const selected = new URLSearchParams(location.search).get("role");
  if (
    selected === "admin" ||
    selected === "staff" ||
    selected === "storeOwner" ||
    selected === "user"
  )
    sessionStorage.setItem("savebite-mock-role", selected);
  if (selected === "guest") {
    localStorage.removeItem("savebite-access-token");
    localStorage.removeItem("savebite-refresh-token");
    sessionStorage.setItem("savebite-mock-guest", "true");
  } else if (selected || !sessionStorage.getItem("savebite-mock-guest")) {
    const pair = tokens();
    localStorage.setItem("savebite-access-token", pair.accessToken);
    localStorage.setItem("savebite-refresh-token", pair.refreshToken);
  }
}
export function userMutation(input: Json) {
  if (object(input)) {
    const body = bodyFor("auth-authApi_getMe_2");
    const value = payload(body);
    patch(value, input);
    return body;
  }
  return null;
}
