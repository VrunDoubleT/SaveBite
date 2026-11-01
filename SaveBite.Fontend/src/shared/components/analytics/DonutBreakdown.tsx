import { Cell, Pie, PieChart } from "recharts";
import { CATEGORY_COLORS, TONE_HEX } from "./palette";
import { toneForStatus, type Tone } from "./tones";

export interface DonutItem {
  key: string;
  label: string;
  /** Numeric size of the slice (count or amount). */
  value: number;
  /** Formatted figure shown in the legend. Defaults to the plain number. */
  display?: string;
  /** Quiet text under the label, for example "4 payments". */
  hint?: string;
  tone?: Tone;
}

interface DonutBreakdownProps {
  title: string;
  items: DonutItem[];
  /** Small label under the total in the centre of the ring. */
  centerLabel: string;
  /** Total shown in the centre. Defaults to the sum of values. */
  centerValue?: string;
  /** Colour slices by the meaning of their label (order, payment and refund statuses). */
  colorByStatus?: boolean;
  emptyText?: string;
}

const SIZE = 168;

function sliceColor(item: DonutItem, index: number, colorByStatus: boolean): string {
  if (item.tone) return TONE_HEX[item.tone];
  if (colorByStatus) return TONE_HEX[toneForStatus(item.label)];
  return CATEGORY_COLORS[index % CATEGORY_COLORS.length];
}

export function DonutBreakdown({
  title,
  items,
  centerLabel,
  centerValue,
  colorByStatus = false,
  emptyText = "Nothing to show for this period.",
}: DonutBreakdownProps) {
  const total = items.reduce((sum, item) => sum + item.value, 0);
  const visible = items.filter((item) => item.value > 0);

  return (
    // @container: the ring and the legend sit side by side only when this block is wide enough,
    // otherwise the legend goes under the ring instead of being squeezed out of the panel.
    <div className="@container min-w-0">
      <h3 className="mb-4 text-sm font-semibold text-neutral-900">{title}</h3>
      <div className="flex flex-col items-center gap-5 @md:flex-row @md:items-center">
        <div className="relative shrink-0" style={{ width: SIZE, height: SIZE }}>
          <PieChart width={SIZE} height={SIZE}>
            <Pie
              data={total > 0 ? visible : [{ key: "empty", label: "Empty", value: 1 }]}
              dataKey="value"
              nameKey="label"
              innerRadius={54}
              outerRadius={78}
              paddingAngle={total > 0 && visible.length > 1 ? (visible.length > 5 ? 1 : 3) : 0}
              cornerRadius={visible.length > 5 ? 2 : 5}
              stroke="none"
              isAnimationActive={false}
            >
              {total > 0 ? (
                visible.map((item) => (
                  <Cell key={item.key} fill={sliceColor(item, items.indexOf(item), colorByStatus)} />
                ))
              ) : (
                <Cell fill="#eef2f0" />
              )}
            </Pie>
          </PieChart>
          <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center">
            <span className="text-xl font-semibold tabular-nums tracking-tight text-neutral-900">
              {centerValue ?? total.toLocaleString("vi-VN")}
            </span>
            <span className="text-xs text-neutral-500">{centerLabel}</span>
          </div>
        </div>

        {total === 0 ? (
          <p className="text-sm text-neutral-400">{emptyText}</p>
        ) : (
          <ul className="w-full min-w-0 flex-1 space-y-2.5">
            {items.map((item, index) => {
              const share = total > 0 ? (item.value * 100) / total : 0;
              return (
                <li
                  key={item.key}
                  className={`flex items-center gap-3 text-sm ${item.value === 0 ? "opacity-50" : ""}`}
                >
                  <span
                    className="size-3 shrink-0 rounded-sm"
                    style={{ backgroundColor: sliceColor(item, index, colorByStatus) }}
                    aria-hidden="true"
                  />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium text-neutral-800" title={item.label}>
                      {item.label}
                    </p>
                    {item.hint && <p className="truncate text-xs text-neutral-400">{item.hint}</p>}
                  </div>
                  <span className="shrink-0 font-semibold tabular-nums text-neutral-900">
                    {item.display ?? item.value.toLocaleString("vi-VN")}
                  </span>
                  <span className="w-14 shrink-0 text-right text-xs tabular-nums text-neutral-500">
                    {share.toLocaleString("vi-VN", { maximumFractionDigits: 1 })}%
                  </span>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </div>
  );
}
