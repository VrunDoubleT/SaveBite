import { Scale } from "lucide-react";
import { ComparisonTable, Panel } from "@/shared/components/analytics";
import {
  describePeriod,
  formatNumber,
  formatVnd,
} from "@/features/store-analytics/utils/formatAnalytics";
import type { AnalyticsPeriodInfo } from "@/features/store-analytics/types/storeAnalytics.types";
import type { PlatformAnalyticsSnapshot } from "../types/platformAnalytics.types";

interface PlatformSummaryComparisonProps {
  period: AnalyticsPeriodInfo;
  selected: PlatformAnalyticsSnapshot;
  overall: PlatformAnalyticsSnapshot;
}

export function PlatformSummaryComparison({ period, selected, overall }: PlatformSummaryComparisonProps) {
  const money = (pick: (s: PlatformAnalyticsSnapshot) => number) => [formatVnd(pick(selected)), formatVnd(pick(overall))];
  const count = (pick: (s: PlatformAnalyticsSnapshot) => number) => [formatNumber(pick(selected)), formatNumber(pick(overall))];

  const rows = [
    { label: "Net order revenue", hint: "Collected minus refunds", tone: "primary" as const, v: money((s) => s.revenueAndOrders.netPlatformOrderRevenue) },
    { label: "Collected online", v: money((s) => s.revenueAndOrders.collectedOnlineAmount) },
    { label: "Collected in cash", hint: "Taken at the shops", v: money((s) => s.revenueAndOrders.collectedCashAmount) },
    { label: "Refunds sent", tone: "warning" as const, v: money((s) => s.revenueAndOrders.totalRefundedAmount) },
    { label: "Platform fees earned", hint: "After refund adjustments", v: money((s) => s.platformFees.netAccruedFeeAmount) },
    { label: "Orders placed", v: count((s) => s.revenueAndOrders.totalOrdersInPeriod) },
    { label: "New users", v: count((s) => s.users.newUsersInPeriod) },
    { label: "New shops", v: count((s) => s.shops.newShopsInPeriod) },
    { label: "Deals created", v: count((s) => s.flashDeals.createdDealsCount) },
    { label: "Deals cancelled early", v: count((s) => s.flashDeals.cancelledEarlyCount) },
  ];

  return (
    <Panel
      icon={Scale}
      title="At a glance"
      description="The time you picked, next to everything the platform has ever recorded."
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
