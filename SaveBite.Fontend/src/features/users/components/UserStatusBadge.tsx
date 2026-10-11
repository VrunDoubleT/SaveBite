interface UserStatusBadgeProps {
  status: string;
  label?: string;
}

export function UserStatusBadge({ status, label }: UserStatusBadgeProps) {
  const isActive = status === "Active";

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold tracking-wide ${isActive ? "bg-primary-50 text-primary-700 border border-primary-100" : "bg-red-50 text-danger border border-red-100"}`}
    >
      <span className={`size-1.5 rounded-full ${isActive ? "bg-primary-500" : "bg-danger"}`}></span>
      {label ? `${label}: ` : ""}
      {status}
    </span>
  );
}
