export function OrderPickupCode({ code, compact = false }: { code: string; compact?: boolean }) {
  return (
    <section
      aria-label="Order code"
      className={compact ? "min-w-0" : "rounded-md border border-primary-200 bg-primary-50 p-5"}
    >
      <p className="text-caption font-semibold uppercase tracking-wide text-text-secondary">
        Order code
      </p>
      <p
        className={`mt-1 break-all font-mono font-bold tracking-wide text-text-primary ${compact ? "text-lg" : "text-2xl sm:text-3xl"}`}
      >
        {code}
      </p>
      <p className="mt-1 text-caption text-text-secondary">Show this code to the shop at pickup.</p>
    </section>
  );
}
