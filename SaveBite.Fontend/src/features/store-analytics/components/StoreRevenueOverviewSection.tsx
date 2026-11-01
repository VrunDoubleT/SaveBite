import { HeroPanel, StackedBarTrend, type ChartGranularity } from "@/shared/components/analytics";
import type { StoreRevenueOverview } from "../types/storeAnalytics.types";
import { formatNumber, formatVnd, formatVndCompact } from "../utils/formatAnalytics";

interface StoreRevenueOverviewSectionProps {
  revenue: StoreRevenueOverview;
  /** How the chart is bucketed: hour for a single day, day for a month or range, month for a year or all time. */
  granularity: ChartGranularity;
  /** Plain-words name of what is shown, for example "today" or "all time". */
  scopeLabel: string;
}

const CHART_TITLES: Record<ChartGranularity, string> = {
  hour: "Collected per hour",
  day: "Collected per day",
  month: "Collected per month",
};

export function StoreRevenueOverviewSection({ revenue, granularity, scopeLabel }: StoreRevenueOverviewSectionProps) {
  return (
    <HeroPanel
      title={`Revenue, ${scopeLabel}`}
      description="Money collected for orders completed in this time, minus refunds already sent back. Online payments and cash taken at the shop are shown separately."
      headlineLabel="Sales revenue"
      headline={formatVnd(revenue.salesRevenue)}
      formula={[
        { label: "Online payments", value: formatVnd(revenue.collectedOnlineAmount) },
        { label: "Cash at the shop", value: formatVnd(revenue.collectedCashAmount) },
        { label: "Refunds sent", value: formatVnd(revenue.refundedAmount), op: "minus" },
      ]}
      stats={[
        {
          label: "Platform fee",
          value: `− ${formatVnd(revenue.netPlatformFeeAmount)}`,
          hint: "Charged by SaveBite",
        },
        {
          label: "After platform fee",
          value: formatVnd(revenue.netShopRevenue),
          hint: "What you keep",
        },
        {
          label: "Completed orders",
          value: formatNumber(revenue.completedOrdersCount),
          hint: `Order value ${formatVnd(revenue.grossCompletedOrderValue)}`,
        },
        {
          label: "Average order",
          value: formatVnd(revenue.averageCompletedOrderValue),
          hint: "Collected per order",
        },
      ]}
    >
      <p className="mb-2 text-sm font-medium text-primary-100">{CHART_TITLES[granularity]}</p>
      <StackedBarTrend
        onDark
        height={230}
        granularity={granularity}
        formatValue={formatVnd}
        formatAxis={formatVndCompact}
        data={revenue.trend.map((pt) => ({
          date: pt.date,
          online: pt.paidOnline,
          cash: pt.paidCash,
          detail: `${pt.completedOrders} order${pt.completedOrders === 1 ? "" : "s"}, ${formatVnd(pt.refundedAmount)} refunded`,
        }))}
      />
    </HeroPanel>
  );
}
