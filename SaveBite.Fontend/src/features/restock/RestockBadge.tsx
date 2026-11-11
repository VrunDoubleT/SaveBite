import { useRestockWorkspace } from "./restockContext";
export function RestockBadge() {
  const { pendingCount } = useRestockWorkspace();
  if (!pendingCount) return null;
  return (
    <span
      data-restock-attention="true"
      aria-label={`${pendingCount} orders need restock decisions`}
      title={`${pendingCount} orders need restock decisions`}
      className="inline-flex min-w-5 items-center justify-center rounded-full bg-orange-600 px-1.5 py-0.5 text-xs font-bold text-white"
    >
      {pendingCount}
    </span>
  );
}
