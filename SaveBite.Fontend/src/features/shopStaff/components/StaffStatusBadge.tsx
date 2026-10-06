interface Props {
  status: string;
}

// Map each known status to its color style
const STATUS_STYLES: Record<string, string> = {
  Active: "bg-emerald-50 text-emerald-700 ring-emerald-600/20",
  Pending: "bg-amber-50 text-amber-700 ring-amber-600/20",
  Inactive: "bg-gray-100 text-gray-600 ring-gray-500/20",
};

const DEFAULT_STYLE = "bg-red-50 text-red-700 ring-red-600/20";

export function StaffStatusBadge({ status }: Props) {
  const style = STATUS_STYLES[status] ?? DEFAULT_STYLE;

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset ${style}`}
    >
      <span className="h-1.5 w-1.5 rounded-full bg-current" />
      {status}
    </span>
  );
}