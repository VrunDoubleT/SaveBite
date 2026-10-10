import type { ReactNode } from "react";
import { AlertCircle, CheckCircle2 } from "lucide-react";
import { getStatusMeta } from "../schemas/adminShopApplication.schema";

export function StatusBadge({ status }: { status: string }) {
  const { label, icon: Icon, badge } = getStatusMeta(status);

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${badge}`}
    >
      <Icon className="size-3.5" aria-hidden="true" />

      {label}
    </span>
  );
}

export function InfoRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-2.5 sm:flex-row sm:items-baseline sm:justify-between sm:gap-6">
      <dt className="shrink-0 text-xs text-slate-500">{label}</dt>

      <dd className="min-w-0 break-words text-sm font-medium text-slate-900 sm:text-right">
        {children}
      </dd>
    </div>
  );
}

export function Panel({
  title,
  aside,
  children,
}: {
  title: string;
  aside?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white">
      <div className="flex items-center justify-between gap-3 border-b border-slate-100 px-5 py-3.5">
        <h2 className="text-base font-semibold text-slate-900">{title}</h2>

        {aside}
      </div>

      <div className="px-5 py-4">{children}</div>
    </section>
  );
}

export function Notice({ tone, children }: { tone: "error" | "success"; children: ReactNode }) {
  const isError = tone === "error";

  const Icon = isError ? AlertCircle : CheckCircle2;

  return (
    <p
      role={isError ? "alert" : "status"}

      className={`flex items-start gap-2.5 rounded-lg px-3.5 py-3 text-sm ring-1 ring-inset ${
        isError
          ? "bg-red-50 text-red-800 ring-red-200"
          : "bg-emerald-50 text-emerald-800 ring-emerald-200"
      }`}
    >
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden="true" />

      <span>{children}</span>
    </p>
  );
}
