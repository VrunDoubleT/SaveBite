import { BadgeCheck, ShieldAlert, ShoppingBag, UserCheck, UserPlus, Users } from "lucide-react";
import { BarTrend, DonutBreakdown, KpiGrid, Panel, type ChartGranularity } from "@/shared/components/analytics";
import { formatNumber, formatPercent } from "@/features/store-analytics/utils/formatAnalytics";
import type { PlatformUserStatistics } from "../types/platformAnalytics.types";

interface PlatformUserStatsSectionProps {
  users: PlatformUserStatistics;
  scopeLabel: string;
  granularity: ChartGranularity;
}

const TREND_TITLES: Record<ChartGranularity, string> = {
  hour: "New accounts per hour",
  day: "New accounts per day",
  month: "New accounts per month",
};

export function PlatformUserStatsSection({ users, scopeLabel, granularity }: PlatformUserStatsSectionProps) {
  const verifiedShare = users.totalUsers > 0 ? (users.verifiedUsers * 100) / users.totalUsers : 0;

  return (
    <Panel
      icon={Users}
      title={`Users, ${scopeLabel}`}
      description="How fast accounts are growing and how many customers actually ordered in this time."
    >
      <KpiGrid
        columns={3}
        items={[
          { icon: Users, label: "All users", value: formatNumber(users.totalUsers) },
          {
            icon: UserPlus,
            tone: "primary",
            label: "Joined in this time",
            value: formatNumber(users.newUsersInPeriod),
          },
          {
            icon: ShoppingBag,
            tone: "info",
            label: "Customers who ordered",
            value: formatNumber(users.activeBuyersInPeriod),
          },
          {
            icon: BadgeCheck,
            label: "Verified email",
            value: formatNumber(users.verifiedUsers),
            hint: `${formatPercent(verifiedShare)} of users`,
          },
          {
            icon: UserCheck,
            label: "Active accounts",
            value: formatNumber(users.activeAccounts),
            hint: `${formatNumber(users.activeCustomerProfiles)} active customer profiles`,
          },
          {
            icon: ShieldAlert,
            tone: users.suspendedAccounts > 0 ? "danger" : "neutral",
            label: "Suspended accounts",
            value: formatNumber(users.suspendedAccounts),
            hint: `${formatNumber(users.suspendedCustomerProfiles)} suspended customer profiles`,
          },
        ]}
      />

      <div>
        <h3 className="mb-3 text-sm font-semibold text-neutral-900">{TREND_TITLES[granularity]}</h3>
        <BarTrend
          granularity={granularity}
          formatValue={(n) => `${formatNumber(n)} account${n === 1 ? "" : "s"}`}
          formatAxis={formatNumber}
          data={users.newUsersTrend.map((pt) => ({ date: pt.date, value: pt.count }))}
        />
      </div>

      <div className="grid gap-8 md:grid-cols-2">
        <DonutBreakdown
          title="By role"
          centerLabel="users"
          items={users.roleBreakdown.map((r) => ({ key: r.label, label: r.label, value: r.count }))}
        />
        <DonutBreakdown
          title="By trust level"
          centerLabel="with a level"
          emptyText="No trust levels assigned yet."
          items={users.trustLevelBreakdown.map((t) => ({ key: t.label, label: t.label, value: t.count }))}
        />
      </div>
    </Panel>
  );
}
