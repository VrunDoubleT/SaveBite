import { ShoppingBag } from "lucide-react";
import {
  DonutBreakdown,
  HeroPanel,
  Panel,
  StackedBarTrend,
  type ChartGranularity,
} from "@/shared/components/analytics";
import {
  formatNumber,
  formatPercent,
  formatVnd,
  formatVndCompact,
} from "@/features/store-analytics/utils/formatAnalytics";
import type { PlatformRevenueAndOrderStatistics } from "../types/platformAnalytics.types";

interface PlatformRevenueOrderSectionProps {
  revenueAndOrders: PlatformRevenueAndOrderStatistics;
  granularity: ChartGranularity;
  scopeLabel: string;
}

const CHART_TITLES: Record<ChartGranularity, string> = {
  hour: "Collected per hour",
  day: "Collected per day",
  month: "Collected per month",
};

export function PlatformRevenueOrderSection({
  revenueAndOrders: rev,
  granularity,
  scopeLabel,
}: PlatformRevenueOrderSectionProps) {
  return (
    <div className="space-y-6">
      <HeroPanel
        title={`Revenue, ${scopeLabel}`}
        description="Calculated like each shop's own dashboard: money collected for completed orders, minus refunds sent back, split into online payments and cash at the shop."
        headlineLabel="Net order revenue"
        headline={formatVnd(rev.netPlatformOrderRevenue)}
        formula={[
          { label: "Online payments", value: formatVnd(rev.collectedOnlineAmount) },
          { label: "Cash at the shop", value: formatVnd(rev.collectedCashAmount) },
          { label: "Refunds sent", value: formatVnd(rev.totalRefundedAmount), op: "minus" },
        ]}
        stats={[
          { label: "Orders placed", value: formatNumber(rev.totalOrdersInPeriod) },
          {
            label: "Completion rate",
            value: formatPercent(rev.orderCompletionRatePercent),
            hint: "Of orders placed",
          },
          {
            label: "Average order",
            value: formatVnd(rev.averageCompletedOrderValue),
            hint: `${formatNumber(rev.completedOrdersInPeriod)} completed`,
          },
          {
            label: "Merchandise value",
            value: formatVnd(rev.grossMerchandiseValue),
            hint: "Excludes failed orders",
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
          data={rev.trend.map((pt) => ({
            date: pt.date,
            online: pt.paidOnline,
            cash: pt.paidCash,
            detail: `${pt.completedOrders} order${pt.completedOrders === 1 ? "" : "s"}, ${formatVnd(pt.refundedAmount)} refunded`,
          }))}
        />
      </HeroPanel>

      <Panel
        icon={ShoppingBag}
        title={`Orders, ${scopeLabel}`}
        description="Orders placed in this time, grouped by their current status."
      >
        <DonutBreakdown
          title="Orders by status"
          centerLabel="orders"
          colorByStatus
          items={rev.orderStatusBreakdown.map((st) => ({
            key: st.status,
            label: st.status,
            value: st.count,
          }))}
          emptyText="No orders were placed in this time."
        />
      </Panel>
    </div>
  );
}
