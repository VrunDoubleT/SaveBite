import {
  FlashDealStatusEnum,
  type ShopFlashDealStatusHistory as StatusHistoryEntry,
} from "@/features/flash-deals/types/flashDeal.types";

interface FlashDealStatusHistoryProps {
  history: StatusHistoryEntry[];
}

const statusLabels: Record<number, string> = {
  [FlashDealStatusEnum.Pending]: "Pending",
  [FlashDealStatusEnum.OnSale]: "On sale",
  [FlashDealStatusEnum.Hidden]: "Hidden",
  [FlashDealStatusEnum.SoldOut]: "Sold out",
  [FlashDealStatusEnum.Closed]: "Closed",
  [FlashDealStatusEnum.Cancelled]: "Cancelled",
};

function formatStatus(status: number | null | undefined) {
  return status == null ? "Created" : (statusLabels[status] ?? "Unknown");
}

function formatDateTime(date: string) {
  return new Date(date).toLocaleString();
}

export function FlashDealStatusHistory({ history }: FlashDealStatusHistoryProps) {
  return (
    <section className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
      <h3 className="text-sm font-bold text-neutral-900">Status history</h3>
      <p className="mt-1 text-xs text-neutral-500">
        Review who changed this deal and why.
      </p>
      {history.length === 0 ? (
        <p className="mt-4 text-sm text-neutral-500">No status changes recorded.</p>
      ) : (
        <ol className="mt-4 divide-y divide-neutral-100">
          {history.map((entry, index) => (
            <li
              key={`${entry.createdAt}-${index}`}
              className="flex flex-col gap-1 py-3 first:pt-0 last:pb-0 sm:flex-row sm:items-start sm:justify-between sm:gap-4"
            >
              <div className="min-w-0">
                <p className="text-sm font-semibold text-neutral-900">
                  {formatStatus(entry.fromStatus)} <span aria-hidden="true">→</span>{" "}
                  {formatStatus(entry.toStatus)}
                </p>
                <p className="mt-1 text-xs text-neutral-600">
                  Changed by {entry.changedByType === "Shop" ? "the shop" : "the system"}
                </p>
                {entry.note && (
                  <p className="mt-1 whitespace-pre-wrap text-xs text-neutral-600">{entry.note}</p>
                )}
              </div>
              <time
                dateTime={entry.createdAt}
                className="shrink-0 text-xs text-neutral-500"
              >
                {formatDateTime(entry.createdAt)}
              </time>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
