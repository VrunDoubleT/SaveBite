import {
  EyeOff,
  Flame,
  Gift,
  Hourglass,
  PackageX,
  ShoppingBasket,
  Tag,
  TimerOff,
  Wallet,
  Zap,
} from "lucide-react";
import { DonutBreakdown, KpiGrid, Panel } from "@/shared/components/analytics";
import {
  formatNumber,
  formatPercent,
  formatVnd,
} from "@/features/store-analytics/utils/formatAnalytics";
import type { PlatformFlashDealStatistics } from "../types/platformAnalytics.types";

interface PlatformFlashDealStatsSectionProps {
  deals: PlatformFlashDealStatistics;
  scopeLabel: string;
}

export function PlatformFlashDealStatsSection({ deals, scopeLabel }: PlatformFlashDealStatsSectionProps) {
  const stillRunning = Math.max(
    0,
    deals.createdDealsCount - deals.endedOnScheduleCount - deals.cancelledEarlyCount,
  );

  return (
    <Panel
      icon={Zap}
      title={`Flash Deals, ${scopeLabel}`}
      description="Deals are counted by the status they have. Cancelled early and ended on schedule are the two final states. Units and sales come from completed orders."
    >
      <KpiGrid
        columns={4}
        items={[
          {
            icon: Tag,
            label: "Deals created",
            value: formatNumber(deals.createdDealsCount),
            hint: `${formatNumber(deals.soldOutDealsCount)} sold out`,
          },
          { icon: Flame, tone: "primary", label: "On sale now", value: formatNumber(deals.onSaleNowCount) },
          {
            icon: EyeOff,
            tone: deals.hiddenNowCount > 0 ? "warning" : "neutral",
            label: "Hidden now",
            value: formatNumber(deals.hiddenNowCount),
          },
          { icon: Hourglass, label: "Waiting to start", value: formatNumber(deals.pendingNowCount) },
          {
            icon: TimerOff,
            tone: "info",
            label: "Ended on schedule",
            value: formatNumber(deals.endedOnScheduleCount),
            hint: "Sale window ran out",
          },
          {
            icon: PackageX,
            tone: deals.cancelledEarlyCount > 0 ? "danger" : "neutral",
            label: "Cancelled early",
            value: formatNumber(deals.cancelledEarlyCount),
            hint: "Stopped before the window ended",
          },
          {
            icon: ShoppingBasket,
            label: "Units sold",
            value: formatNumber(deals.totalSoldUnitsFromCompletedOrders),
            hint: `${formatNumber(deals.ordersWithDealsCount)} orders, ${formatPercent(deals.sellThroughRatePercent)} sell-through`,
          },
          { icon: Wallet, tone: "primary", label: "Deal sales", value: formatVnd(deals.completedDealSalesAmount) },
        ]}
      />

      <KpiGrid
        columns={3}
        items={[
          {
            icon: Gift,
            tone: "primary",
            label: "Saved by customers",
            value: formatVnd(deals.customerEstimatedSavingsAmount),
            hint: "Versus original prices",
          },
          {
            icon: ShoppingBasket,
            label: "Units allocated",
            value: formatNumber(deals.totalAllocatedUnits),
            hint: "In deals created in this time",
          },
          {
            icon: Hourglass,
            label: "Units reserved",
            value: formatNumber(deals.totalReservedUnits),
            hint: "Held for unpaid orders",
          },
        ]}
      />

      <div className="grid gap-8 md:grid-cols-2">
        <DonutBreakdown
          title="Deals by status"
          centerLabel="deals"
          colorByStatus
          items={deals.statusBreakdown.map((s) => ({ key: s.label, label: s.label, value: s.count }))}
          emptyText="No deals were created in this time."
        />
        <DonutBreakdown
          title="How deals finished"
          centerLabel="deals"
          items={[
            { key: "ended", label: "Ended on schedule", value: deals.endedOnScheduleCount, tone: "info" },
            { key: "cancelled", label: "Cancelled early", value: deals.cancelledEarlyCount, tone: "danger" },
            { key: "running", label: "Not finished yet", value: stillRunning, tone: "primary" },
          ]}
          emptyText="No deals to show yet."
        />
      </div>
    </Panel>
  );
}
