export type Tone = "neutral" | "primary" | "info" | "warning" | "danger";

// Colour carries meaning: green is money in or healthy, orange is money going back or needs attention, red is a
// failure, blue is informational. All class names come from the app theme.
export const TONE: Record<Tone, { text: string; soft: string; icon: string; dot: string; bar: string }> = {
  neutral: {
    text: "text-neutral-900",
    soft: "bg-neutral-100 text-neutral-700",
    icon: "bg-neutral-100 text-neutral-600",
    dot: "bg-neutral-400",
    bar: "bg-neutral-400",
  },
  primary: {
    text: "text-primary-700",
    soft: "bg-primary-50 text-primary-700",
    icon: "bg-primary-100 text-primary-700",
    dot: "bg-primary-500",
    bar: "bg-primary-500",
  },
  info: {
    text: "text-info",
    soft: "bg-info/10 text-info",
    icon: "bg-info/10 text-info",
    dot: "bg-info",
    bar: "bg-info",
  },
  warning: {
    text: "text-accent-700",
    soft: "bg-accent-50 text-accent-700",
    icon: "bg-accent-100 text-accent-700",
    dot: "bg-accent-500",
    bar: "bg-accent-500",
  },
  danger: {
    text: "text-danger",
    soft: "bg-danger/10 text-danger",
    icon: "bg-danger/10 text-danger",
    dot: "bg-danger",
    bar: "bg-danger",
  },
};

const STATUS_TONES: Record<string, Tone> = {
  completed: "primary",
  succeeded: "primary",
  paid: "primary",
  active: "primary",
  approved: "primary",
  transferred: "primary",
  onsale: "primary",
  pending: "warning",
  awaitingpayment: "warning",
  awaitingcustomerdetails: "warning",
  awaitingtransfer: "warning",
  pendingverification: "warning",
  paused: "warning",
  issued: "info",
  confirmed: "info",
  preparing: "info",
  readyforpickup: "info",
  soldout: "info",
  closed: "info",
  cancelled: "danger",
  rejected: "danger",
  failed: "danger",
  noshow: "danger",
  overdue: "danger",
  suspended: "danger",
};

export function toneForStatus(status: string): Tone {
  return STATUS_TONES[status.replace(/[\s_-]/g, "").toLowerCase()] ?? "neutral";
}
