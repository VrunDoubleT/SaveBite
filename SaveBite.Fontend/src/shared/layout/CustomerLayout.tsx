import { Outlet } from "react-router-dom";
import { CustomerHeader } from "@/shared/components/header/CustomerHeader";

export function CustomerLayout() {
  return (
    <div className="min-h-screen bg-bg-body">
      <CustomerHeader />
      <main className="mx-auto w-full max-w-300 px-4 py-8 md:px-8 md:py-12">
        <Outlet />
      </main>
    </div>
  );
}
