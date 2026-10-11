interface CategoryStatusBadgeProps {
  isActive: boolean;
}

export function CategoryStatusBadge({ isActive }: CategoryStatusBadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold tracking-wide ${isActive ? "bg-primary-50 text-primary-700 border border-primary-100" : "bg-neutral-50 text-neutral-600 border border-neutral-200"}`}
    >
      <span
        className={`size-1.5 rounded-full ${isActive ? "bg-primary-500" : "bg-neutral-400"}`}
      ></span>
      {isActive ? "Active" : "Inactive"}
    </span>
  );
}
