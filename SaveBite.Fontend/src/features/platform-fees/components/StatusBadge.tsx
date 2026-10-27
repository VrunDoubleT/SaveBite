import type { PlatformFeeStatementStatus } from "../types/platformFee.types";
import { normalizeStatementStatus } from "../utils/platformFeeUtils";

interface StatusBadgeProps {
   status: PlatformFeeStatementStatus | string | number;
  size?: "sm" | "md";
}

export function StatusBadge({ status, size = "md" }: StatusBadgeProps) {
  const sizeClasses = size === "sm" ? "px-2 py-0.5 text-xs" : "px-3 py-1 text-xs font-semibold";
  const normalized = normalizeStatementStatus(status);
 switch (normalized) {
    case "Paid":
      return (
        <span
          className={`inline-flex items-center gap-1.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200/80 shadow-xs ${sizeClasses}`}
        >
          <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
            Paid
        </span>
      );
    case "Issued":
      return (
        <span
          className={`inline-flex items-center gap-1.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200/80 shadow-xs ${sizeClasses}`}
        >
          <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
          Unpaid
        </span>
      );
    case "Overdue":
      return (
        <span
          className={`inline-flex items-center gap-1.5 rounded-full bg-rose-50 text-rose-700 border border-rose-200/80 shadow-xs ${sizeClasses}`}
        >
          <span className="h-1.5 w-1.5 rounded-full bg-rose-500 animate-ping" />
          Overdue
        </span>
      );
    case "Draft":
      return (
        <span
          className={`inline-flex items-center gap-1.5 rounded-full bg-slate-50 text-slate-600 border border-slate-200 shadow-xs ${sizeClasses}`}
        >
          <span className="h-1.5 w-1.5 rounded-full bg-slate-400" />
          Draft
        </span>
      );
    default:
      return (
        <span className={`inline-flex items-center rounded-full bg-slate-100 text-slate-700 ${sizeClasses}`}>
          {status}
        </span>
      );
  }
}
