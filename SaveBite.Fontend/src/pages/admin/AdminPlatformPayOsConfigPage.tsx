import { AdminPlatformPayOsConfig } from "@/features/platform-fees/components/AdminPlatformPayOsConfig";

export function AdminPlatformPayOsConfigPage() {
  return (
    <div className="space-y-6">
      <header>
        <h1 className="mt-1 text-2xl font-bold text-slate-900">PayOS Settings</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
          Configure the payment account used to collect platform fees.
        </p>
      </header>
      <AdminPlatformPayOsConfig />
    </div>
  );
}
