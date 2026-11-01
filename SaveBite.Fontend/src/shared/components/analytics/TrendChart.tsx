import { useEffect, useId, useState } from "react";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatBucketLabel, formatBucketTitle, type ChartGranularity } from "./chartFormat";
import { CHART } from "./palette";

export interface TrendDatum {
  /** yyyy-MM-dd */
  date: string;
  value: number;
  /** Extra line shown in the tooltip, for example "3 orders". */
  detail?: string;
}

interface TrendChartProps {
  data: TrendDatum[];
  /** Full figure used in the tooltip. */
  formatValue: (value: number) => string;
  /** Short figure used on the vertical axis. */
  formatAxis?: (value: number) => string;
  /** Dark variant for use on the hero panel. */
  onDark?: boolean;
  height?: number;
  granularity?: ChartGranularity;
}

function usePrefersReducedMotion(): boolean {
  const [reduced, setReduced] = useState(false);
  useEffect(() => {
    const query = window.matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => setReduced(query.matches);
    update();
    query.addEventListener("change", update);
    return () => query.removeEventListener("change", update);
  }, []);
  return reduced;
}

interface TipProps {
  active?: boolean;
  payload?: ReadonlyArray<{ payload?: unknown }>;
  formatValue: (value: number) => string;
  granularity: ChartGranularity;
}

function ChartTip({ active, payload, formatValue, granularity }: TipProps) {
  const datum = payload?.[0]?.payload as TrendDatum | undefined;
  if (!active || !datum) return null;
  return (
    <div className="rounded-md border border-neutral-200 bg-white px-3 py-2 shadow-lg">
      <p className="text-xs text-neutral-500">{formatBucketTitle(datum.date, granularity)}</p>
      <p className="text-sm font-semibold tabular-nums text-neutral-900">{formatValue(datum.value)}</p>
      {datum.detail && <p className="text-xs text-neutral-500">{datum.detail}</p>}
    </div>
  );
}

function EmptyChart({ onDark, height }: { onDark?: boolean; height: number }) {
  return (
    <div
      className={`flex items-center justify-center rounded-md border border-dashed text-sm ${
        onDark ? "border-white/20 text-white/60" : "border-neutral-200 text-neutral-400"
      }`}
      style={{ height }}
    >
      Nothing to chart in this period yet.
    </div>
  );
}

const MARGIN = { top: 8, right: 8, bottom: 0, left: 0 };

/** Smooth area chart: used for money over time. */
export function AreaTrend({
  data,
  formatValue,
  formatAxis,
  onDark = false,
  height = 240,
  granularity = "day",
}: TrendChartProps) {
  const gradientId = `area-${useId().replace(/[^a-zA-Z0-9]/g, "")}`;
  const reduced = usePrefersReducedMotion();
  const hasData = data.some((d) => d.value > 0);
  if (!hasData) return <EmptyChart onDark={onDark} height={height} />;

  const stroke = onDark ? CHART.lineOnDark : CHART.line;
  const tick = { fontSize: 12, fill: onDark ? "rgba(255,255,255,0.65)" : CHART.tick };
  const grid = onDark ? "rgba(255,255,255,0.12)" : CHART.grid;

  return (
    <div role="img" aria-label="Trend chart" style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={MARGIN}>
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={stroke} stopOpacity={onDark ? 0.45 : 0.3} />
              <stop offset="100%" stopColor={stroke} stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid vertical={false} stroke={grid} strokeDasharray="3 4" />
          <XAxis
            dataKey="date"
            tickFormatter={(key: string) => formatBucketLabel(key, granularity)}
            tick={tick}
            tickLine={false}
            axisLine={false}
            minTickGap={28}
          />
          <YAxis
            tickFormatter={formatAxis ?? formatValue}
            tick={tick}
            tickLine={false}
            axisLine={false}
            width={52}
          />
          <Tooltip
            cursor={{ stroke, strokeOpacity: 0.4 }}
            content={({ active, payload }) => (
              <ChartTip active={active} payload={payload} formatValue={formatValue} granularity={granularity} />
            )}
          />
          <Area
            type="monotone"
            dataKey="value"
            stroke={stroke}
            strokeWidth={2.5}
            fill={`url(#${gradientId})`}
            dot={false}
            activeDot={{ r: 5, strokeWidth: 2, stroke: onDark ? "#0f5737" : "#ffffff", fill: stroke }}
            isAnimationActive={!reduced}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}

/** Column chart: used for counts per day. */
export function BarTrend({ data, formatValue, formatAxis, height = 220, granularity = "day" }: TrendChartProps) {
  const reduced = usePrefersReducedMotion();
  const hasData = data.some((d) => d.value > 0);
  if (!hasData) return <EmptyChart height={height} />;

  return (
    <div role="img" aria-label="Trend chart" style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} margin={MARGIN}>
          <CartesianGrid vertical={false} stroke={CHART.grid} strokeDasharray="3 4" />
          <XAxis
            dataKey="date"
            tickFormatter={(key: string) => formatBucketLabel(key, granularity)}
            tick={{ fontSize: 12, fill: CHART.tick }}
            tickLine={false}
            axisLine={false}
            minTickGap={28}
          />
          <YAxis
            tickFormatter={formatAxis ?? formatValue}
            tick={{ fontSize: 12, fill: CHART.tick }}
            tickLine={false}
            axisLine={false}
            allowDecimals={false}
            width={40}
          />
          <Tooltip
            cursor={{ fill: "rgba(34,168,102,0.08)" }}
            content={({ active, payload }) => (
              <ChartTip active={active} payload={payload} formatValue={formatValue} granularity={granularity} />
            )}
          />
          <Bar
            dataKey="value"
            fill="#22a866"
            radius={[4, 4, 0, 0]}
            maxBarSize={28}
            isAnimationActive={!reduced}
          />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

export interface StackedDatum {
  date: string;
  online: number;
  cash: number;
  detail?: string;
}

interface StackedTrendProps {
  data: StackedDatum[];
  formatValue: (value: number) => string;
  formatAxis?: (value: number) => string;
  onDark?: boolean;
  height?: number;
  granularity?: ChartGranularity;
}

const STACK_COLORS = {
  light: { online: "#22a866", cash: "#ff9f1c" },
  dark: { online: "#b7f0d1", cash: "#ffb94d" },
};

/** Money in per hour/day/month, stacked into online payments and cash at the shop. */
export function StackedBarTrend({
  data,
  formatValue,
  formatAxis,
  onDark = false,
  height = 250,
  granularity = "day",
}: StackedTrendProps) {
  const reduced = usePrefersReducedMotion();
  const colors = onDark ? STACK_COLORS.dark : STACK_COLORS.light;
  const hasData = data.some((d) => d.online + d.cash > 0);
  const tick = { fontSize: 12, fill: onDark ? "rgba(255,255,255,0.65)" : CHART.tick };
  const grid = onDark ? "rgba(255,255,255,0.12)" : CHART.grid;

  return (
    <div>
      <ul
        className={`mb-3 flex flex-wrap gap-x-4 gap-y-1 text-sm ${onDark ? "text-primary-100" : "text-neutral-600"}`}
        aria-label="Legend"
      >
        <li className="flex items-center gap-2">
          <span className="size-3 rounded-sm" style={{ backgroundColor: colors.online }} aria-hidden="true" />
          Online payments
        </li>
        <li className="flex items-center gap-2">
          <span className="size-3 rounded-sm" style={{ backgroundColor: colors.cash }} aria-hidden="true" />
          Cash at the shop
        </li>
      </ul>

      {!hasData ? (
        <EmptyChart onDark={onDark} height={height} />
      ) : (
        <div role="img" aria-label="Money collected, online and cash" style={{ height }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data} margin={MARGIN}>
              <CartesianGrid vertical={false} stroke={grid} strokeDasharray="3 4" />
              <XAxis
                dataKey="date"
                tickFormatter={(key: string) => formatBucketLabel(key, granularity)}
                tick={tick}
                tickLine={false}
                axisLine={false}
                minTickGap={24}
              />
              <YAxis
                tickFormatter={formatAxis ?? formatValue}
                tick={tick}
                tickLine={false}
                axisLine={false}
                width={52}
              />
              <Tooltip
                cursor={{ fill: onDark ? "rgba(255,255,255,0.08)" : "rgba(34,168,102,0.08)" }}
                content={({ active, payload }) => {
                  const datum = payload?.[0]?.payload as StackedDatum | undefined;
                  if (!active || !datum) return null;
                  return (
                    <div className="min-w-44 rounded-md border border-neutral-200 bg-white px-3 py-2 shadow-lg">
                      <p className="text-xs text-neutral-500">{formatBucketTitle(datum.date, granularity)}</p>
                      <div className="mt-1 space-y-0.5 text-sm">
                        <p className="flex items-center justify-between gap-4">
                          <span className="flex items-center gap-1.5 text-neutral-600">
                            <span className="size-2.5 rounded-sm" style={{ backgroundColor: STACK_COLORS.light.online }} />
                            Online
                          </span>
                          <span className="font-medium tabular-nums text-neutral-900">{formatValue(datum.online)}</span>
                        </p>
                        <p className="flex items-center justify-between gap-4">
                          <span className="flex items-center gap-1.5 text-neutral-600">
                            <span className="size-2.5 rounded-sm" style={{ backgroundColor: STACK_COLORS.light.cash }} />
                            Cash
                          </span>
                          <span className="font-medium tabular-nums text-neutral-900">{formatValue(datum.cash)}</span>
                        </p>
                        <p className="flex items-center justify-between gap-4 border-t border-neutral-100 pt-1">
                          <span className="text-neutral-600">Total</span>
                          <span className="font-semibold tabular-nums text-neutral-900">
                            {formatValue(datum.online + datum.cash)}
                          </span>
                        </p>
                      </div>
                      {datum.detail && <p className="mt-1 text-xs text-neutral-500">{datum.detail}</p>}
                    </div>
                  );
                }}
              />
              <Bar dataKey="online" stackId="money" fill={colors.online} maxBarSize={30} isAnimationActive={!reduced} />
              <Bar
                dataKey="cash"
                stackId="money"
                fill={colors.cash}
                radius={[4, 4, 0, 0]}
                maxBarSize={30}
                isAnimationActive={!reduced}
              />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
