import { Banknote, CreditCard, ListChecks, Undo2, Wallet } from "lucide-react";
import { DonutBreakdown, KpiGrid, Panel } from "@/shared/components/analytics";
import {
  formatNumber,
  formatPaymentMethod,
  formatVnd,
  formatVndCompact,
} from "@/features/store-analytics/utils/formatAnalytics";
import type { PlatformPaymentAndRefundStatistics } from "../types/platformAnalytics.types";

interface PlatformPaymentRefundSectionProps {
  paymentsAndRefunds: PlatformPaymentAndRefundStatistics;
  scopeLabel: string;
}

export function PlatformPaymentRefundSection({
  paymentsAndRefunds: fin,
  scopeLabel,
}: PlatformPaymentRefundSectionProps) {
  return (
    <Panel
      icon={CreditCard}
      title={`Payments and refunds, ${scopeLabel}`}
      description="Payments are counted by the day they were paid, refund requests by the day they were opened, and transfers by the day the money was sent."
    >
      <KpiGrid
        columns={4}
        items={[
          {
            icon: Wallet,
            tone: "primary",
            label: "Collected",
            value: formatVnd(fin.totalCollectedPaymentAmount),
            hint: `${formatNumber(fin.succeededPaymentsCount)} successful payments`,
          },
          {
            icon: ListChecks,
            label: "Payment attempts",
            value: formatNumber(fin.totalPaymentTransactions),
            hint: `${formatNumber(fin.pendingPaymentsCount)} pending, ${formatNumber(fin.failedOrCancelledPaymentsCount)} failed or cancelled`,
          },
          {
            icon: Undo2,
            tone: "warning",
            label: "Refunds requested",
            value: formatVnd(fin.totalRequestedRefundAmount),
            hint: `${formatNumber(fin.totalRefundRequests)} requests, ${formatNumber(fin.inProgressRefundsCount)} in progress`,
          },
          {
            icon: Banknote,
            label: "Refunds sent",
            value: formatVnd(fin.totalTransferredRefundAmount),
            hint: `${formatNumber(fin.completedOrTransferredRefundsCount)} settled, ${formatNumber(fin.rejectedOrWithdrawnRefundsCount)} rejected or withdrawn`,
          },
        ]}
      />

      <div className="grid gap-8 md:grid-cols-2">
        <DonutBreakdown
          title="Collected by payment method"
          centerLabel="collected"
          centerValue={formatVndCompact(fin.totalCollectedPaymentAmount)}
          items={fin.paymentMethodBreakdown.map((m) => ({
            key: m.paymentMethod,
            label: formatPaymentMethod(m.paymentMethod),
            value: m.totalAmount,
            display: formatVnd(m.totalAmount),
            hint: `${formatNumber(m.transactionCount)} payments`,
          }))}
          emptyText="No successful payments in this time."
        />
        <DonutBreakdown
          title="Payments by status"
          centerLabel="payments"
          colorByStatus
          items={fin.paymentStatusBreakdown
            .filter((s) => s.count > 0)
            .map((s) => ({ key: s.label, label: s.label, value: s.count, hint: formatVnd(s.amount) }))}
          emptyText="No payments in this time."
        />
        <DonutBreakdown
          title="Refunds by status"
          centerLabel="refunds"
          colorByStatus
          items={fin.refundStatusBreakdown
            .filter((s) => s.count > 0)
            .map((s) => ({ key: s.label, label: s.label, value: s.count, hint: formatVnd(s.amount) }))}
          emptyText="No refund requests in this time."
        />
        <DonutBreakdown
          title="Refunds by type"
          centerLabel="refunds"
          items={fin.refundTypeBreakdown
            .filter((t) => t.count > 0)
            .map((t) => ({ key: t.label, label: t.label, value: t.count, hint: formatVnd(t.amount) }))}
          emptyText="No refund requests in this time."
        />
      </div>
    </Panel>
  );
}
