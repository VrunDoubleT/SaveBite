export function OrderPickupCode({ code, compact = false }: { code: string; compact?: boolean }) {
  return (
    <section
      aria-label="Order code"
      className={compact ? "min-w-0" : "rounded-xl border border-emerald-200 bg-emerald-50 p-5"}
    >
      <p className="text-xs font-semibold uppercase tracking-wide text-neutral-500">Order code</p>
      <p
        className={`mt-1 break-all font-mono font-bold tracking-wide text-neutral-900 ${compact ? "text-lg" : "text-2xl sm:text-3xl"}`}
      >
        {code}
      </p>
      <p className="mt-1 text-xs text-neutral-500">Show this code to the shop at pickup.</p>
    </section>
  );
}
