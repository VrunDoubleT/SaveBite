import { useId } from "react";
import { Select } from "./Controls";

export type FilterOption = { value: string; label: string };
/** Approved account filter format: white surface, left label, right native dropdown. */
export function FilterBar({
  filters,
  inline = false,
}: {
  inline?: boolean;
  filters: {
    label: string;
    value: string;
    options: readonly FilterOption[];
    onChange: (value: string) => void;
  }[];
}) {
  const id = useId();
  return (
    <div className="@container rounded-lg border border-border-default bg-bg-surface shadow-sm">
      <div
        className={inline
          ? "grid grid-cols-1 @min-[40rem]:grid-cols-2"
          : "divide-y divide-border-default"}
      >
        {filters.map((filter, index) => (
          <div key={filter.label} className={inline
            ? "grid min-w-0 grid-cols-[6rem_minmax(0,1fr)] items-center gap-3 p-3"
            : "flex min-w-0 flex-wrap items-center gap-3 p-3"}>
            <label htmlFor={`${id}-${index}`} className="text-body font-medium text-text-secondary">
              {filter.label}
            </label>
            <Select
              id={`${id}-${index}`}
              value={filter.value}
              onChange={(event) => filter.onChange(event.target.value)}
              className={inline
                ? "min-h-11 min-w-0 !w-full"
                : "ml-auto min-h-11 !w-auto min-w-0 max-w-full sm:min-w-52"}
            >
              {filter.options.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </Select>
          </div>
        ))}
      </div>
    </div>
  );
}
