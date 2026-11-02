import { Link, Outlet, useLocation } from "react-router-dom";
import { PageHeader } from "@/shared/components/ui";
import { AccountSidebar } from "@/shared/layouts/AccountSidebar";
import { APP_PATHS } from "@/app/router/paths";
import { ACCOUNT_NAV_ITEMS } from "@/shared/config/accountNavigation";

export function AccountLayout() {
  const { pathname } = useLocation();

  const current = ACCOUNT_NAV_ITEMS.find(
    (item) => pathname === item.to || pathname.startsWith(`${item.to}/`),
  );

  return (
    <div className="space-y-6 py-6 sm:py-8">
      <PageHeader
        title={current?.title ?? "Account"}
        description={current?.description ?? "Manage your account"}
        breadcrumb={
          <ol className="flex flex-wrap items-center gap-2">
            <li>
              <Link
                to={APP_PATHS.HOME}
                className="inline-flex min-h-8 items-center font-medium hover:text-primary-700"
              >
                Home
              </Link>
            </li>
            <li className="flex items-center gap-2">
              <span aria-hidden="true" className="text-text-muted">
                ›
              </span>
              <span aria-current="page" className="font-medium text-text-primary">
                {current?.title ?? "Account"}
              </span>
            </li>
          </ol>
        }
      />
      <div className="grid grid-cols-1 items-start gap-6 min-[900px]:grid-cols-[248px_minmax(0,1fr)]">
        <AccountSidebar />

        <section className="min-w-0" aria-live="polite">
          {/* Page content */}
          <Outlet />
        </section>
      </div>
    </div>
  );
}
