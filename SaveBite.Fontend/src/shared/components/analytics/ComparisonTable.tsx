import { TONE, type Tone } from "./tones";

export interface ComparisonRow {
  label: string;
  selected: string;
  overall: string;
  tone?: Tone;
  /** Quiet note under the label. */
  hint?: string;
}

interface ComparisonTableProps {
  selectedLabel: string;
  overallLabel?: string;
  rows: ComparisonRow[];
}

/** Key figures side by side: the selected period (the one the owner is looking at) next to the all-time total. */
export function ComparisonTable({ selectedLabel, overallLabel = "All time", rows }: ComparisonTableProps) {
  return (
    <div className="overflow-hidden rounded-md border border-neutral-200">
      <table className="w-full text-left text-sm">
        <thead className="bg-neutral-50 text-xs">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium text-neutral-500">
              Figure
            </th>
            <th scope="col" className="px-4 py-3 text-right font-semibold text-primary-700">
              {selectedLabel}
            </th>
            <th scope="col" className="px-4 py-3 text-right font-medium text-neutral-500">
              {overallLabel}
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-neutral-100">
          {rows.map((row) => (
            <tr key={row.label} className="hover:bg-neutral-50">
              <th scope="row" className="px-4 py-3 font-normal text-neutral-700">
                {row.label}
                {row.hint && <span className="block text-xs text-neutral-400">{row.hint}</span>}
              </th>
              <td
                className={`px-4 py-3 text-right text-base font-semibold tabular-nums ${TONE[row.tone ?? "neutral"].text}`}
              >
                {row.selected}
              </td>
              <td className="px-4 py-3 text-right tabular-nums text-neutral-500">{row.overall}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
