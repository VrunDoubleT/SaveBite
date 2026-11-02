import { formatCartMoney } from "@/features/cart/utils/formatCartMoney";

export function AmountSummary({
  amounts,
}: {
  amounts: { label: string; amount: number; tone?: "primary" | "warning" }[];
}) {
  return (
    <dl className={`grid gap-2 ${amounts.length === 3 ? "grid-cols-3" : "grid-cols-2"}`}>
      {amounts.map(({ label, amount, tone }) => (
        <div
          key={label}
          className={`min-w-0 rounded-md p-3 ${tone === "primary" ? "bg-primary-50 text-primary-700" : tone === "warning" ? "bg-accent-50 text-accent-700" : "bg-neutral-50 text-text-primary"}`}
        >
          <dt className="text-caption font-medium">{label}</dt>
          <dd className="mt-1 break-words text-body font-bold tabular-nums sm:text-lg">
            {formatCartMoney(amount)}
          </dd>
        </div>
      ))}
    </dl>
  );
}
