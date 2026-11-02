export function OrderPickupCode({
  code,
  compact = false,
  showHint = true,
}: {
  code: string;
  compact?: boolean;
  showHint?: boolean;
}) {
  return (
    <section aria-label="Order code" className="min-w-0">
      <p className="text-caption font-semibold uppercase tracking-wide text-text-secondary">
        Order code
      </p>
      <p
        className={`mt-1 break-all font-mono font-bold tracking-wide text-text-primary ${compact ? "text-lg" : "text-2xl sm:text-3xl"}`}
      >
        {code}
      </p>
      {showHint && (
        <p className="mt-1 text-caption text-text-secondary">
          Show this code to the shop at pickup.
        </p>
      )}
    </section>
  );
}
