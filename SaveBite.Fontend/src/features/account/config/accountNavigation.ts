import {
  Briefcase,
  ClipboardList,
  MessageSquareText,
  ShieldCheck,
  Store,
  User,
  UserPlus,
  type LucideIcon,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import type { AuthRole } from "@/features/auth/types/auth.types";

export interface AccountNavItem {
  to: string;
  label: string;
  title: string;
  description: string;
  icon: LucideIcon;
  roles?: readonly AuthRole[];
}

export interface AccountNavGroup {
  label: string;
  items: readonly AccountNavItem[];
}

export const ACCOUNT_NAV_GROUPS: readonly AccountNavGroup[] = [
  {
    label: "Account",
    items: [
      {
        to: APP_PATHS.ACCOUNT_PROFILE,
        label: "Profile",
        title: "Profile",
        description: "Manage your personal information",
        icon: User,
      },
      {
        to: APP_PATHS.ACCOUNT_ORDERS,
        label: "Orders",
        title: "Orders",
        description: "Track and review your orders",
        icon: ClipboardList,
      },
      {
        to: APP_PATHS.ACCOUNT_REVIEWS,
        label: "Reviews",
        title: "Reviews",
        description: "View the reviews you have submitted to stores",
        icon: MessageSquareText,
      },
      {
        to: APP_PATHS.ACCOUNT_TRUST_SCORES,
        label: "Trust Score & Level",
        title: "Trust Score & Level",
        description: "View your trust score and membership level",
        icon: ShieldCheck,
      },
    ],
  },
  {
    label: "Business",
    items: [
      {
        to: APP_PATHS.ACCOUNT_SHOP_REGISTRATION,
        label: "Register Store",
        title: "Register Store",
        description: "Become a SaveBite store partner",
        icon: Store,
      },
      {
        to: APP_PATHS.ACCOUNT_WORKSPACE,
        label: "Workspace",
        title: "Workspace",
        description: "Access the stores you are working with",
        icon: Briefcase,
          roles: ["staff", "storeOwner"],
      },
      {
        to: APP_PATHS.ACCOUNT_STAFF_INVITATIONS,
        label: "Staff Invitations",
        title: "Staff Invitations",
        description: "Manage invitations to join stores",
        icon: UserPlus,
      },
    ],
  },
];

export const ACCOUNT_NAV_ITEMS: readonly AccountNavItem[] = ACCOUNT_NAV_GROUPS.flatMap(
  (group) => group.items,
);
