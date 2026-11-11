import { Badge } from "@/shared/components/ui";
const tones = {
  Pending: "warning",
  Succeeded: "primary",
  Failed: "danger",
  Cancelled: "neutral",
  Expired: "neutral",
} as const;
export function PaymentStatusBadge({ status }: { status: string }) {
  return (
    <Badge tone={tones[status as keyof typeof tones] ?? "neutral"}>
      {status === "Succeeded" ? "Paid" : status}
    </Badge>
  );
}
