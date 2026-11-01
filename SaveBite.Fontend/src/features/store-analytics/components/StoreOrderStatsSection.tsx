import { Ban, CheckCircle2, Clock, ShoppingBag } from "lucide-react";
import { DonutBreakdown, KpiGrid, Panel } from "@/shared/components/analytics";
import type { StoreOrderStatistics } from "../types/storeAnalytics.types";
import { formatNumber, formatPercent } from "../utils/formatAnalytics";

interface StoreOrderStatsSectionProps {
  orders: StoreOrderStatistics;
  scopeLabel: string;
}

export function StoreOrderStatsSection({ orders, scopeLabel }: StoreOrderStatsSectionProps) {
  return (
    <Panel
      icon={ShoppingBag}
      title={`Orders, ${scopeLabel}`}
      description="Orders placed in this time, grouped by their current status."
    >
      <KpiGrid
        columns={2}
        items={[
          { icon: ShoppingBag, label: "Orders placed", value: formatNumber(orders.totalOrders) },
          {
            icon: CheckCircle2,
            tone: "primary",
            label: "Completed",
            value: formatPercent(orders.completionRatePercent),
            hint: `${formatNumber(orders.completedOrders)} orders`,
          },
          {
            icon: Ban,
            tone: orders.cancelledOrRejectedOrders > 0 ? "danger" : "neutral",
            label: "Cancelled or rejected",
            value: formatPercent(orders.cancellationRatePercent),
            hint: `${formatNumber(orders.cancelledOrRejectedOrders)} orders`,
          },
          {
            icon: Clock,
            tone: orders.noShowOrders > 0 ? "warning" : "neutral",
            label: "No-shows",
            value: formatNumber(orders.noShowOrders),
            hint: `${formatNumber(orders.lateArrivalOrders)} late arrivals`,
          },
        ]}
      />

      <DonutBreakdown
        title="Orders by status"
        centerLabel="orders"
        colorByStatus
        items={orders.statusBreakdown.map((item) => ({
          key: item.status,
          label: item.status,
          value: item.count,
        }))}
        emptyText="No orders were placed in this time."
      />
    </Panel>
  );
}
