import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";

interface NavItemProps {
  end?: boolean;
  icon: ReactNode;
  label: string;
  to: string;
}

export function NavItem({ end, icon, label, to }: NavItemProps) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        `flex min-h-11 items-center gap-3 rounded-md px-3 py-2 text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-400/30 ${
          isActive
            ? "bg-primary-50 text-primary-700"
            : "text-neutral-600 hover:bg-neutral-50 hover:text-neutral-900"
        }`
      }
    >
      <span className="flex size-5 shrink-0 items-center justify-center" aria-hidden="true">
        {icon}
      </span>
      <span>{label}</span>
    </NavLink>
  );
}
