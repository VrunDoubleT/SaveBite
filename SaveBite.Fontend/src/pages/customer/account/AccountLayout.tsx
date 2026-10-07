import { Outlet, useLocation } from "react-router-dom";
import { AccountSidebar } from "@/features/account/components/AccountSidebar";
import { ACCOUNT_NAV_ITEMS } from "@/features/account/config/accountNavigation";

export function AccountLayout() {
  const { pathname } = useLocation();

  const current = ACCOUNT_NAV_ITEMS.find(
    (item) => pathname === item.to || pathname.startsWith(`${item.to}/`),
  );

  return (
    <div>
      <h1 className="text-2xl font-semibold text-text-primary">{current?.title ?? "Account"}</h1>

      <p className="mb-6 mt-1 text-sm text-text-secondary">
        {current?.description ?? "Manage your account"}
      </p>

      <div className="grid items-start gap-6 min-[900px]:grid-cols-[248px_1fr]">
        <AccountSidebar />

        <section className="min-w-0" aria-live="polite">
          <Outlet />
        </section>
      </div>
    </div>
  );
}
