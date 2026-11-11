import { Link, useParams } from "react-router-dom";
import { RestockTaskList } from "./RestockTaskList";
export function FlashDealStockHistory({ flashDealId }: { flashDealId: string }) {
  const { shopId } = useParams();
  return (
    <section className="space-y-4 rounded-xl border border-neutral-200 bg-white p-4 sm:p-5">
      <div>
        <h2 className="text-base font-bold text-neutral-900">Stock history for this flash deal</h2>
        <p className="mt-2 text-sm text-neutral-500">
          View quantities returned to sale, stopped from sale and awaiting a decision from cancelled
          or no-show orders. Only items belonging to this flash deal are shown.
        </p>
      </div>
      <Link
        to={shopId ? `/staff/shops/${shopId}/restock` : "/store-owner/restock"}
        className="inline-flex min-h-11 items-center text-sm font-semibold text-emerald-700 hover:underline"
      >
        Open Restock to process pending quantities →
      </Link>
      <RestockTaskList key={flashDealId} pendingOnly={false} flashDealId={flashDealId} readOnly />
    </section>
  );
}
