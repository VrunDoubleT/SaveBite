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
          <div
            key={key}
            className="flex min-w-0 flex-wrap justify-between items-start gap-2 text-xs"
          >
            <span className="text-text-secondary capitalize">
              {key.replace(/([A-Z])/g, " $1").trim()}
            </span>
            {typeof values[key] === "object" && values[key] !== null ? (
              <pre className="w-full min-w-0 whitespace-pre-wrap [overflow-wrap:anywhere] rounded border border-neutral-100 bg-white p-2 text-text-primary">
                {JSON.stringify(values[key], null, 2)}
              </pre>
            ) : (
              <span className="min-w-0 max-w-full whitespace-pre-wrap [overflow-wrap:anywhere] rounded border border-neutral-100 bg-white px-2 py-0.5 font-semibold text-text-primary">
                {String(values[key])}
              </span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
