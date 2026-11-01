import { useState } from "react";
import { UserRound, Phone } from "lucide-react";
import type { Refund } from "../types/refund.types";

// Show the customer's current profile and level, with explicit fallbacks for missing account data.
export function RefundCustomerInfo({
  refund,
  linkPhone = true,
}: {
  refund: Refund;
  linkPhone?: boolean;
}) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  return (
    <div className="flex min-w-0 items-start gap-3">
      <span className="flex size-11 shrink-0 items-center justify-center overflow-hidden rounded-full border border-neutral-200 bg-neutral-50 text-neutral-400">
        {refund.customerAvatarUrl && refund.customerAvatarUrl !== failedUrl ? (
          <img
            src={refund.customerAvatarUrl}
            alt={`${refund.customerName || "Customer"} avatar`}
            loading="lazy"
            onError={() => setFailedUrl(refund.customerAvatarUrl!)}
            className="size-full object-cover"
          />
        ) : (
          <UserRound size={21} />
        )}
      </span>
      <div className="min-w-0 space-y-1.5">
        <div className="flex flex-wrap items-center gap-2">
          <p className="break-words text-sm font-semibold text-neutral-900">
            {refund.customerName || "Customer"}
          </p>
          <span
            title="Current customer trust level"
            className="rounded-md border border-primary-200 bg-primary-50 px-2 py-0.5 text-[11px] font-semibold text-primary-800"
          >
            {refund.customerLevelRank != null
              ? `Level ${refund.customerLevelRank}${refund.customerLevelName && refund.customerLevelName.toLowerCase() !== `level ${refund.customerLevelRank}` ? ` · ${refund.customerLevelName}` : ""}`
              : refund.customerLevelName || "Level unavailable"}
          </span>
        </div>
        <p className="flex items-center gap-1.5 text-xs text-neutral-500">
          <Phone size={12} className="shrink-0" />
          {refund.customerPhone && linkPhone ? (
            <a
              href={`tel:${refund.customerPhone}`}
              className="hover:text-primary-700 hover:underline"
            >
              {refund.customerPhone}
            </a>
          ) : (
            refund.customerPhone || "Phone not provided"
          )}
        </p>
      </div>
    </div>
  );
}
