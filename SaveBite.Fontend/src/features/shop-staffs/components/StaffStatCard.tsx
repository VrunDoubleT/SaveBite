import type { ReactNode } from "react";

interface Props {
  label: string;
  value: number | string;
  icon: ReactNode;
  accent: string; // e.g. "bg-emerald-50 text-emerald-600"
}

export function StaffStatCard({ label, value, icon, accent }: Props) {
  return (
    <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
      <div className="flex items-center gap-3">
        <span className={`flex size-10 items-center justify-center rounded-lg ${accent}`}>
          {icon}
        </span>
        <div>
          <p className="text-xs font-medium text-neutral-500">{label}</p>
          <p className="text-xl font-bold text-neutral-900">{value}</p>
        </div>
      </div>
    </div>
  );
}
