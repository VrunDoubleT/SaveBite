interface ListFilterProps<T extends string> {
  label: string;
  /** Text of the "no filter" option, e.g. "All events". */
  allLabel: string;
  value: T | null;
  options: ReadonlyArray<{ value: T; label: string }>;
  onChange: (value: T | null) => void;
}

/** Filter dropdown shared by the paged lists so they look and behave the same. */
export function ListFilter<T extends string>({
  label,
  allLabel,
  value,
  options,
  onChange,
}: ListFilterProps<T>) {
  return (
    <label className="flex items-center gap-2 text-sm text-text-secondary">
      {label}
      <select
        value={value ?? ""}
        onChange={(event) =>
          onChange(options.find((option) => option.value === event.target.value)?.value ?? null)
        }
        className="rounded-md border border-border-default bg-white px-3 py-2 text-sm text-text-primary focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus"
      >
        <option value="">{allLabel}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}
