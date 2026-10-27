import { Panel, ComparisonTable } from "@/shared/components/analytics";
import { Scale } from "lucide-react";
import type { AnalyticsPeriodInfo, StoreAnalyticsSnapshot } from "../types/storeAnalytics.types";
import { describePeriod, formatNumber, formatVnd } from "../utils/formatAnalytics";

interface StoreSummaryComparisonProps {
  period: AnalyticsPeriodInfo;
  selected: StoreAnalyticsSnapshot;
  overall: StoreAnalyticsSnapshot;
}

export function StoreSummaryComparison({ period, selected, overall }: StoreSummaryComparisonProps) {
  const money = (pick: (s: StoreAnalyticsSnapshot) => number) => [formatVnd(pick(selected)), formatVnd(pick(overall))];
  const count = (pick: (s: StoreAnalyticsSnapshot) => number) => [formatNumber(pick(selected)), formatNumber(pick(overall))];

  const rows = [
    { label: "Sales revenue", hint: "Collected minus refunds", tone: "primary" as const, v: money((s) => s.revenueOverview.salesRevenue) },
    { label: "Collected online", v: money((s) => s.revenueOverview.collectedOnlineAmount) },
    { label: "Collected in cash", hint: "Taken at the shop", v: money((s) => s.revenueOverview.collectedCashAmount) },
    { label: "Refunds sent", tone: "warning" as const, v: money((s) => s.revenueOverview.refundedAmount) },
    { label: "Platform fee", v: money((s) => s.revenueOverview.netPlatformFeeAmount) },
    { label: "Completed orders", v: count((s) => s.revenueOverview.completedOrdersCount) },
    { label: "Orders placed", v: count((s) => s.orderStatistics.totalOrders) },
    { label: "Deals created", v: count((s) => s.flashDealStatistics.createdDealsCount) },
    { label: "Deals cancelled early", v: count((s) => s.flashDealStatistics.cancelledEarlyCount) },
    { label: "Units sold through deals", v: count((s) => s.flashDealStatistics.completedOrderItemsQuantitySold) },
  ];

  return (
    <Panel
      icon={Scale}
      title="At a glance"
      description="The time you picked, next to everything your shop has ever recorded."
    >
      <div className="grid gap-6 lg:grid-cols-2">
        {[rows.slice(0, Math.ceil(rows.length / 2)), rows.slice(Math.ceil(rows.length / 2))].map((group, index) => (
          <ComparisonTable
            key={index}
            selectedLabel={describePeriod(period)}
            rows={group.map(({ v, ...row }) => ({ ...row, selected: v[0], overall: v[1] }))}
          />
        ))}
      </div>
    </Panel>
  );
}
