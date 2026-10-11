interface ChangedValuesProps {
  values: Record<string, unknown> | null;
}

export function ChangedValues({ values }: ChangedValuesProps) {
  if (!values) return null;

  const keys = Object.keys(values).filter((k) => k.toLowerCase() !== "target");
  if (keys.length === 0) return null;

  return (
    <div className="mt-3 rounded-md bg-neutral-50 p-2.5 border border-neutral-100">
      <p className="text-[11px] font-bold text-neutral-400 uppercase tracking-wider mb-1.5">
        Changes
      </p>
      <div className="space-y-1.5">
        {keys.map((key) => (
          <div key={key} className="flex justify-between items-center text-xs">
            <span className="text-text-secondary capitalize">
              {key.replace(/([A-Z])/g, " $1").trim()}
            </span>
            <span className="font-semibold text-text-primary px-2 py-0.5 bg-white rounded shadow-sm border border-neutral-100">
              {String(values[key])}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
