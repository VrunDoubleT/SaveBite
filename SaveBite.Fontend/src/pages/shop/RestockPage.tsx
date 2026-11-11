import { useState } from "react";
import { RestockTaskList } from "@/features/restock/RestockTaskList";
import { useRestockWorkspace } from "@/features/restock/restockContext";
export function RestockPage() {
  const [pendingOnly, setPendingOnly] = useState(true);
  const { pendingCount } = useRestockWorkspace();
  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-neutral-900">Restock decisions</h1>
        <p className="mt-2 max-w-3xl text-sm text-neutral-500">
          Review cancelled and no-show orders and choose quantities to return to sale. Decisions
          must be recorded within five minutes; remaining quantities become unsellable.
        </p>
      </header>
      <div role="group" aria-label="Restock filter" className="flex flex-wrap gap-2">
        <button
          type="button"
          aria-pressed={pendingOnly}
          onClick={() => setPendingOnly(true)}
          className={`min-h-11 rounded-lg border px-4 text-sm font-semibold ${pendingOnly || (pendingCount ?? 0) > 0 ? "border-orange-300 bg-orange-50 text-orange-800" : "border-neutral-200 bg-white"}`}
        >
          Needs attention {pendingCount !== null && `(${pendingCount})`}
        </button>
        <button
          type="button"
          aria-pressed={!pendingOnly}
          onClick={() => setPendingOnly(false)}
          className={`min-h-11 rounded-lg border px-4 text-sm font-semibold ${!pendingOnly ? "border-emerald-300 bg-emerald-50 text-emerald-800" : "border-neutral-200 bg-white"}`}
        >
          All decisions
        </button>
      </div>
      <RestockTaskList key={String(pendingOnly)} pendingOnly={pendingOnly} />
    </div>
  );
}
