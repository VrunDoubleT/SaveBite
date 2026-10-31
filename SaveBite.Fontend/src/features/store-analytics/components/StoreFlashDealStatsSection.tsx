import {
  EyeOff,
  Flame,
  Hourglass,
  PackageX,
  ShoppingBasket,
  Tag,
  TimerOff,
  Wallet,
  Zap,
} from "lucide-react";
import {
  DonutBreakdown,
  EmptyState,
  KpiGrid,
  Panel,
  ProgressBar,
  StatusBadge,
} from "@/shared/components/analytics";
import type { StoreFlashDealStatistics } from "../types/storeAnalytics.types";
import { formatNumber, formatPercent, formatShortDate, formatVnd } from "../utils/formatAnalytics";

interface StoreFlashDealStatsSectionProps {
  deals: StoreFlashDealStatistics;
  scopeLabel: string;
}

export function StoreFlashDealStatsSection({ deals, scopeLabel }: StoreFlashDealStatsSectionProps) {
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
          {
            icon: Hourglass,
            label: "Waiting to start",
            value: formatNumber(deals.pendingNowCount),
          },
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
            value: formatNumber(deals.completedOrderItemsQuantitySold),
            hint: `${formatNumber(deals.ordersWithDealsCount)} orders, ${formatPercent(deals.sellThroughRatePercent)} sell-through`,
          },
          {
            icon: Wallet,
            tone: "primary",
            label: "Deal sales",
            value: formatVnd(deals.completedOrderItemsSalesAmount),
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

      <div>
        <h3 className="mb-3 text-sm font-semibold text-neutral-900">Best-selling deals</h3>
        {deals.topPerformingDeals.length === 0 ? (
          <EmptyState
            icon={ShoppingBasket}
            title="No deal sales in this time"
            description="Deals appear here once customers pick up their orders."
          />
        ) : (
          <div className="overflow-x-auto rounded-md border border-neutral-200">
            <table className="w-full min-w-[680px] divide-y divide-neutral-200 text-left text-sm">
              <thead className="bg-neutral-50 text-xs text-neutral-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Product</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="min-w-40 px-4 py-3 font-medium">Sold of allocated</th>
                  <th scope="col" className="px-4 py-3 text-right font-medium">Sales</th>
                  <th scope="col" className="px-4 py-3 font-medium">Sale window</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-100">
                {deals.topPerformingDeals.map((d) => {
                  const sellThrough =
                    d.allocatedQuantity > 0 ? (d.quantitySoldFromCompletedOrders * 100) / d.allocatedQuantity : 0;
                  return (
                    <tr key={d.flashDealId} className="hover:bg-neutral-50">
                      <td className="px-4 py-3 font-medium text-neutral-900">{d.productName}</td>
                      <td className="px-4 py-3">
                        <StatusBadge status={d.status} />
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <div className="w-24 shrink-0">
                            <ProgressBar percent={sellThrough} label={`${d.productName} sold`} />
                          </div>
                          <span className="whitespace-nowrap text-xs tabular-nums text-neutral-600">
                            {formatNumber(d.quantitySoldFromCompletedOrders)} / {formatNumber(d.allocatedQuantity)}
                          </span>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-right font-semibold tabular-nums text-primary-700">
                        {formatVnd(d.revenueFromCompletedOrders)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 tabular-nums text-neutral-500">
                        {formatShortDate(d.saleStartTime)} to {formatShortDate(d.orderEndTime)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Panel>
  );
}
