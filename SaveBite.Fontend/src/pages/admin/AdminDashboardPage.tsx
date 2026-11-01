import {
  Banknote,
  CalendarDays,
  CreditCard,
  History,
  Landmark,
  LayoutDashboard,
  Users,
  Zap,
} from "lucide-react";
import { Navigate, useLocation, useParams } from "react-router-dom";
import {
  PlatformFeeStatsSection,
  PlatformFlashDealStatsSection,
  PlatformPaymentRefundSection,
  PlatformRevenueOrderSection,
  PlatformShopStatsSection,
  PlatformSummaryComparison,
  PlatformUserStatsSection,
} from "@/features/admin-analytics/components";
import { usePlatformAnalytics } from "@/features/admin-analytics/hooks/usePlatformAnalytics";
import {
  DEFAULT_PLATFORM_ANALYTICS_TAB,
  findPlatformAnalyticsTab,
  platformAnalyticsTabPath,
  type PlatformAnalyticsTab,
} from "@/features/admin-analytics/utils/analyticsTabs";
import type { DetailView } from "@/features/store-analytics/hooks/useAnalyticsPeriod";
import { AnalyticsPeriodFilter } from "@/features/store-analytics/components";
import { describePeriod, formatPeriodRange } from "@/features/store-analytics/utils/formatAnalytics";
import {
  DashboardHeader,
  DashboardSkeleton,
  SegmentedTabs,
  type TabItem,
} from "@/shared/components/analytics";
import { ErrorModal } from "@/shared/components/modal/ErrorModal";

const SECTION_TABS: TabItem<PlatformAnalyticsTab>[] = [
  { id: "overview", label: "Overview", icon: LayoutDashboard },
  { id: "revenue-orders", label: "Revenue and orders", icon: Banknote },
  { id: "fees", label: "Fees", icon: Landmark },
  { id: "users-shops", label: "Users and shops", icon: Users },
  { id: "deals", label: "Flash Deals", icon: Zap },
  { id: "payments-refunds", label: "Payments and refunds", icon: CreditCard },
];

// Routes: /admin/analytics/:tab (overview | revenue-orders | fees | users-shops | deals | payments-refunds)
// The period and the "all time" switch stay in the query string (?preset=month&month=2026-10&view=all-time).
export function AdminDashboardPage() {
  const { tab } = useParams();
  const { search } = useLocation();
  const activeTab = findPlatformAnalyticsTab(tab);

  // /admin/analytics and unknown tabs open the first tab (replaces the history entry).
  if (!activeTab) {
    return (
      <Navigate
        replace
        to={{ pathname: platformAnalyticsTabPath(DEFAULT_PLATFORM_ANALYTICS_TAB), search }}
      />
    );
  }

  return <PlatformDashboard activeTab={activeTab} />;
}

function PlatformDashboard({ activeTab }: { activeTab: PlatformAnalyticsTab }) {
  const analytics = usePlatformAnalytics();
  const { search } = useLocation();
  const { data, loading, errorMessage, clearError, view, setView } = analytics;

  if (loading && !data) {
    return <DashboardSkeleton label="Loading platform analytics..." />;
  }

  const selectedName = data ? describePeriod(data.period) : "Selected period";
  const viewTabs: TabItem<DetailView>[] = [
    { id: "selected", label: selectedName, icon: CalendarDays },
    { id: "overall", label: "All time", icon: History },
  ];
  const snapshot = data ? (view === "selected" ? data.selected : data.overall) : null;
  const scopeLabel = view === "selected" ? selectedName.toLowerCase() : "all time";
  const granularity = view === "selected" && data ? data.period.granularity : "month";
  const show = (...tabs: PlatformAnalyticsTab[]) => activeTab === "overview" || tabs.includes(activeTab);

  return (
    <div className={`space-y-6 transition-opacity ${loading ? "opacity-60" : ""}`} aria-busy={loading}>
      <DashboardHeader
        title="Platform analytics"
        subtitle="Revenue, fees, users, shops, Flash Deals, payments and refunds across SaveBite."
        periodLabel={data ? formatPeriodRange(data.period) : undefined}
        actions={
          <AnalyticsPeriodFilter
            preset={analytics.preset}
            onSelectPreset={analytics.selectPreset}
            month={analytics.month}
            onMonthChange={analytics.selectMonth}
            year={analytics.year}
            onYearChange={analytics.selectYear}
            fromDate={analytics.fromDate}
            onFromDateChange={analytics.setFromDate}
            toDate={analytics.toDate}
            onToDateChange={analytics.setToDate}
            rangeError={analytics.rangeError}
            loading={loading}
            onRefresh={analytics.refresh}
          />
        }
      />

      {data && snapshot && (
        <>
          <PlatformSummaryComparison period={data.period} selected={data.selected} overall={data.overall} />

          <div className="flex flex-wrap items-center gap-3">
            <p className="text-sm font-medium text-neutral-700">Show details for</p>
            <SegmentedTabs label="Detail view" items={viewTabs} value={view} onChange={setView} />
          </div>

          {/* Each section is its own URL; the period and view in the query string are carried along. */}
          <SegmentedTabs
            label="Dashboard sections"
            items={SECTION_TABS}
            value={activeTab}
            getTo={(id) => ({ pathname: platformAnalyticsTabPath(id), search })}
          />

          {show("revenue-orders") && (
            <PlatformRevenueOrderSection
              revenueAndOrders={snapshot.revenueAndOrders}
              granularity={granularity}
              scopeLabel={scopeLabel}
            />
          )}

          {show("fees") && <PlatformFeeStatsSection fees={snapshot.platformFees} scopeLabel={scopeLabel} />}

          {show("users-shops") && (
            <div className="grid grid-cols-1 gap-6 2xl:grid-cols-2">
              <PlatformUserStatsSection users={snapshot.users} scopeLabel={scopeLabel} granularity={granularity} />
              <PlatformShopStatsSection shops={snapshot.shops} scopeLabel={scopeLabel} />
            </div>
          )}

          {show("deals") && <PlatformFlashDealStatsSection deals={snapshot.flashDeals} scopeLabel={scopeLabel} />}

          {show("payments-refunds") && (
            <PlatformPaymentRefundSection paymentsAndRefunds={snapshot.paymentsAndRefunds} scopeLabel={scopeLabel} />
          )}
        </>
      )}

      <ErrorModal message={errorMessage} onClose={clearError} />
    </div>
  );
}
