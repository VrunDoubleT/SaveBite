import { Flame, PackageCheck, Store, StoreIcon, ShieldAlert, Sparkles } from "lucide-react";
import { DonutBreakdown, KpiGrid, Panel } from "@/shared/components/analytics";
import { formatNumber } from "@/features/store-analytics/utils/formatAnalytics";
import type { PlatformShopStatistics } from "../types/platformAnalytics.types";

interface PlatformShopStatsSectionProps {
  shops: PlatformShopStatistics;
  scopeLabel: string;
}

export function PlatformShopStatsSection({ shops, scopeLabel }: PlatformShopStatsSectionProps) {
  return (
    <Panel
      icon={Store}
      title={`Shops, ${scopeLabel}`}
      description="How many shops are on the platform, which of them are trading, and how registrations are moving."
    >
      <KpiGrid
        columns={3}
        items={[
          { icon: Store, label: "All shops", value: formatNumber(shops.totalShops) },
          {
            icon: Sparkles,
            tone: "primary",
            label: "Opened in this time",
            value: formatNumber(shops.newShopsInPeriod),
            hint: `${formatNumber(shops.newApplicationsInPeriod)} new applications`,
          },
          {
            icon: Flame,
            tone: "info",
            label: "With a deal on sale",
            value: formatNumber(shops.shopsWithActiveDeals),
            hint: "Right now",
          },
          {
            icon: PackageCheck,
            label: "With completed orders",
            value: formatNumber(shops.shopsWithCompletedOrdersInPeriod),
            hint: "In this time",
          },
          { icon: StoreIcon, label: "Active", value: formatNumber(shops.activeShops) },
          {
            icon: ShieldAlert,
            tone: shops.suspendedShops > 0 ? "danger" : "neutral",
            label: "Suspended",
            value: formatNumber(shops.suspendedShops),
            hint: `${formatNumber(shops.pausedShops)} paused`,
          },
        ]}
      />

      <div className="grid gap-8 md:grid-cols-2">
        <DonutBreakdown
          title="Shops by status"
          centerLabel="shops"
          colorByStatus
          items={shops.shopStatusBreakdown.map((s) => ({ key: s.label, label: s.label, value: s.count }))}
        />
        <DonutBreakdown
          title="Applications, all time"
          centerLabel="applications"
          colorByStatus
          items={shops.applicationStatusBreakdown.map((a) => ({ key: a.label, label: a.label, value: a.count }))}
        />
      </div>
    </Panel>
  );
}
