import type { ReactNode } from "react";

interface Props {
  label: string;
  value: number;
  icon: ReactNode;
  accent: string; // e.g. "bg-emerald-100 text-emerald-600"
}

export function StaffStatCard({ label, value, icon, accent }: Props) {
  return (
    <div className="flex items-center gap-4 rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className={`flex h-11 w-11 items-center justify-center rounded-xl ${accent}`}>
        {icon}
      </div>
      <div>
        <p className="text-sm text-gray-500">{label}</p>
        <p className="text-2xl font-bold text-gray-900">{value}</p>
      </div>
    </div>
  );
}