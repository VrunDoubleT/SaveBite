import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";

interface NavItemProps {
  collapsed?: boolean;
  badge?: ReactNode;
  end?: boolean;
  icon: ReactNode;
  label: string;
  to: string;
}

export function NavItem({ badge, end, icon, label, to, collapsed = false }: NavItemProps) {
  return (
    <NavLink
      to={to}
      title={collapsed ? label : undefined}
      aria-label={collapsed ? label : undefined}
      end={end}
      className={({ isActive }) =>
        `relative border border-transparent has-[[data-restock-attention]]:border-orange-300 has-[[data-restock-attention]]:bg-orange-50 has-[[data-restock-attention]]:text-orange-800 flex min-h-11 items-center gap-3 rounded-md ${collapsed ? "justify-center px-2" : "px-3"} py-2 text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-400/30 ${
          isActive
            ? "bg-primary-50 text-primary-700"
            : "text-neutral-600 hover:bg-neutral-50 hover:text-neutral-900"
        }`
      }
    >
      <span className="flex size-5 shrink-0 items-center justify-center" aria-hidden="true">
        {icon}
      </span>
      <span className={collapsed ? "sr-only" : ""}>{label}</span>
      {badge && (
        <span
          className={
            collapsed ? "absolute -right-1 -top-1 inline-flex" : "ml-auto inline-flex shrink-0"
          }
        >
          {badge}
        </span>
      )}
    </NavLink>
  );
}
