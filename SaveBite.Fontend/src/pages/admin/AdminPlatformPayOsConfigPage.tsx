import { AdminPlatformPayOsConfig } from "@/features/platform-fees/components/AdminPlatformPayOsConfig";

export function AdminPlatformPayOsConfigPage() {
  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <header>
        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">Payment settings</p>
        <h1 className="mt-1 text-2xl font-bold text-slate-900">PayOS Settings</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
          Configure the payment account used to collect platform fees.
        </p>
      </header>
      <AdminPlatformPayOsConfig />
    </div>
  );
}
