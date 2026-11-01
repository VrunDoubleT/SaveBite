import { CreditCard, Receipt, Wallet } from "lucide-react";
import { DonutBreakdown, KpiGrid, Panel } from "@/shared/components/analytics";
import type { StorePaymentMethodRevenue } from "../types/storeAnalytics.types";
import { formatNumber, formatPaymentMethod, formatVnd, formatVndCompact } from "../utils/formatAnalytics";

interface StorePaymentMethodSectionProps {
  paymentMethods: StorePaymentMethodRevenue;
  scopeLabel: string;
}

export function StorePaymentMethodSection({ paymentMethods, scopeLabel }: StorePaymentMethodSectionProps) {
  return (
    <Panel
      icon={CreditCard}
      title={`How customers pay, ${scopeLabel}`}
      description="Successful payments in this time, grouped by the payment method recorded for each one."
    >
      <KpiGrid
        columns={2}
        items={[
          {
            icon: Wallet,
            tone: "primary",
            label: "Collected",
            value: formatVnd(paymentMethods.totalSucceededPaymentAmount),
          },
          {
            icon: Receipt,
            label: "Successful payments",
            value: formatNumber(paymentMethods.totalSucceededPaymentCount),
          },
        ]}
      />

      <DonutBreakdown
        title="Collected by payment method"
        centerLabel="collected"
        centerValue={formatVndCompact(paymentMethods.totalSucceededPaymentAmount)}
        items={paymentMethods.methods.map((m) => ({
          key: m.paymentMethod,
          label: formatPaymentMethod(m.paymentMethod),
          value: m.totalAmount,
          display: formatVnd(m.totalAmount),
          hint: `${formatNumber(m.transactionCount)} payment${m.transactionCount === 1 ? "" : "s"}`,
        }))}
        emptyText="No successful payments in this time."
      />
    </Panel>
  );
}
