import { Outlet } from "react-router-dom";
import { CustomerHeader } from "@/shared/components/header/CustomerHeader";

export function CustomerLayout() {
  return (
    <div className="min-h-screen bg-bg-body">
      <CustomerHeader />
      <main className="mx-auto w-full max-w-320 px-4 py-4 md:p-6">
        <Outlet />
      </main>
    </div>
  );
}
