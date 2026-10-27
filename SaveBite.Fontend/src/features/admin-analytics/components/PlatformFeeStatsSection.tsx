import { CircleAlert, Coins, Landmark, Receipt, ShoppingBag, Undo2, Wallet } from "lucide-react";
import {
  DonutBreakdown,
  EmptyState,
  KpiGrid,
  Panel,
  StatusBadge,
} from "@/shared/components/analytics";
import {
  formatNumber,
  formatShortDate,
  formatVnd,
} from "@/features/store-analytics/utils/formatAnalytics";
import type { PlatformFeeStatistics } from "../types/platformAnalytics.types";

interface PlatformFeeStatsSectionProps {
  fees: PlatformFeeStatistics;
  scopeLabel: string;
}

export function PlatformFeeStatsSection({ fees, scopeLabel }: PlatformFeeStatsSectionProps) {
  const rate =
    fees.currentFeeRatePercent === null ? "Not configured" : `${fees.currentFeeRatePercent}%`;

  return (
    <Panel
      icon={Landmark}
      title={`Platform fees, ${scopeLabel}`}
      description="Fees earned on orders completed in this time, and how the monthly fee statements are being settled."
      action={
        <span className="inline-flex items-center gap-2 rounded-full bg-primary-50 px-3 py-1.5 text-sm text-primary-800">
          Current fee rate
          <strong className="font-semibold tabular-nums">{rate}</strong>
        </span>
      }
    >
      <KpiGrid
        columns={3}
        items={[
          {
            icon: Coins,
            tone: "primary",
            label: "Net fees earned",
            value: formatVnd(fees.netAccruedFeeAmount),
            hint: "Fees earned plus adjustments",
          },
          {
            icon: Receipt,
            label: "Fees earned",
            value: formatVnd(fees.grossAccruedFeeAmount),
            hint: `${formatNumber(fees.totalFeeBearingOrders)} orders carried a fee`,
          },
          {
            icon: Undo2,
            tone: "warning",
            label: "Adjustments for refunds",
            value: formatVnd(fees.refundFeeAdjustmentAmount),
            hint: "Applied to fees of this time",
          },
          {
            icon: Wallet,
            tone: "info",
            label: "Statements paid",
            value: formatVnd(fees.totalPaidStatementAmount),
            hint: "Settled during this time",
          },
          {
            icon: CircleAlert,
            tone: fees.totalOutstandingStatementAmount > 0 ? "warning" : "neutral",
            label: "Unpaid statements",
            value: formatVnd(fees.totalOutstandingStatementAmount),
            hint: "All periods, not affected by the filter",
          },
          {
            icon: ShoppingBag,
            label: "Fee-bearing orders",
            value: formatNumber(fees.totalFeeBearingOrders),
          },
        ]}
      />

      <div className="grid gap-8 lg:grid-cols-2">
        <DonutBreakdown
          title="Statements by status"
          centerLabel="statements"
          colorByStatus
          items={fees.statementStatusBreakdown.map((s) => ({
            key: s.label,
            label: s.label,
            value: s.count,
            hint: formatVnd(s.amount),
          }))}
          emptyText="No statements for the billing months in this time."
        />

        <div className="min-w-0">
          <h3 className="mb-3 text-sm font-semibold text-neutral-900">Latest statements</h3>
          {fees.recentStatements.length === 0 ? (
            <EmptyState
              icon={Landmark}
              title="No statements in this time"
              description="Statements are created per shop for each billing month."
            />
          ) : (
            <div className="overflow-x-auto rounded-md border border-neutral-200">
              <table className="w-full min-w-[420px] divide-y divide-neutral-200 text-left text-sm">
                <thead className="bg-neutral-50 text-xs text-neutral-500">
                  <tr>
                    <th scope="col" className="px-4 py-3 font-medium">Shop</th>
                    <th scope="col" className="px-4 py-3 font-medium">Status</th>
                    <th scope="col" className="px-4 py-3 text-right font-medium">Fee</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-neutral-100">
                  {fees.recentStatements.map((st) => (
                    <tr key={st.statementId} className="hover:bg-neutral-50">
                      <td className="px-4 py-3">
                        <p className="font-medium text-neutral-900">{st.shopName}</p>
                        <p className="text-xs tabular-nums text-neutral-500">
                          {String(st.billingMonth).padStart(2, "0")}/{st.billingYear}
                          {" · "}
                          {st.paidAt
                            ? `Paid ${formatShortDate(st.paidAt)}`
                            : st.dueAt
                              ? `Due ${formatShortDate(st.dueAt)}`
                              : "Not issued"}
                        </p>
                      </td>
                      <td className="px-4 py-3">
                        <StatusBadge status={st.status} />
                      </td>
                      <td className="px-4 py-3 text-right font-semibold tabular-nums text-neutral-900">
                        {formatVnd(st.totalFeeAmount)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </Panel>
  );
}
