import { Fragment } from "react";
import { NavLink } from "react-router-dom";
import { ACCOUNT_NAV_GROUPS } from "@/features/account/config/accountNavigation";

export function AccountSidebar() {
  return (
    <nav
      aria-label="Account navigation"
      className="rounded-lg border border-border-default bg-bg-surface p-2 shadow-sm min-[900px]:sticky min-[900px]:top-20 min-[900px]:p-3"
    >
      {ACCOUNT_NAV_GROUPS.map((group, index) => (
        <Fragment key={group.label}>
          {index > 0 && (
            <hr className="mx-2 my-4 hidden border-t border-border-default min-[900px]:block" />
          )}
          <div className="mb-3 last:mb-0 min-[900px]:mb-0">
            <span className="mb-2 block px-3 text-xs font-semibold text-text-muted">
              {group.label}
            </span>
            <ul className="flex gap-2 overflow-x-auto pb-0.5 min-[900px]:flex-col min-[900px]:gap-0.5 min-[900px]:overflow-visible">
              {group.items.map(({ to, label, icon: Icon }) => (
                <li key={to} className="shrink-0 min-[900px]:shrink">
                  <NavLink
                    to={to}
                    className={({ isActive }) =>
                      `group flex w-full items-center gap-3 whitespace-nowrap rounded-md px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:shadow-focus ${
                        isActive
                          ? "bg-primary-50 text-primary-700"
                          : "text-text-secondary hover:bg-neutral-100 hover:text-text-primary"
                      }`
                    }
                  >
                    {({ isActive }) => (
                      <>
                        <Icon
                          className={`size-4.5 shrink-0 ${
                            isActive ? "text-primary-600" : "text-text-muted"
                          }`}
                          aria-hidden="true"
                        />
                        {label}
                      </>
                    )}
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        </Fragment>
      ))}
    </nav>
  );
}
