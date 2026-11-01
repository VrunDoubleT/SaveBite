import type { ShopStaff } from "@/features/shop-staffs/types/shopStaff.types";
import { response0 } from "./factories/shop-staffs-shopStaffApi";
import { object } from "./db";
/** This page receives its staff card through React Router location.state in the real app. */
export function seedNavigationState() {
  const match = location.pathname.match(/^\/store-owner\/staff\/([^/]+)$/);
  if (!match || match[1] === "invitations") return;
  const current: unknown = history.state;
  const state = object(current) ? current : {};
  const userState = object(state.usr) ? state.usr : {};
  if (userState.staff) return;
  const staff: ShopStaff = { ...response0.data.items[0], id: match[1] };
  history.replaceState({ ...state, usr: { ...userState, staff } }, "", location.href);
}
