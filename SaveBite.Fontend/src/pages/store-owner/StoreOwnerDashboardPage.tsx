import { CalendarDays, History } from "lucide-react";
import {
  AnalyticsPeriodFilter,
  StoreFlashDealStatsSection,
  StoreOrderStatsSection,
  StorePaymentMethodSection,
  StoreRevenueOverviewSection,
  StoreSummaryComparison,
} from "@/features/store-analytics/components";
import type { DetailView } from "@/features/store-analytics/hooks/useAnalyticsPeriod";
import { useStoreAnalytics } from "@/features/store-analytics/hooks/useStoreAnalytics";
import { describePeriod, formatPeriodRange } from "@/features/store-analytics/utils/formatAnalytics";
import {
  DashboardHeader,
  DashboardSkeleton,
  SegmentedTabs,
  type TabItem,
} from "@/shared/components/analytics";
import { ErrorModal } from "@/shared/components/modal/ErrorModal";

// The period and the "all time" switch live in the query string (?preset=month&month=2026-10&view=all-time).
export function StoreOwnerDashboardPage() {
  const analytics = useStoreAnalytics();
  const { data, loading, errorMessage, clearError, view, setView } = analytics;

  if (loading && !data) {
    return <DashboardSkeleton label="Loading store analytics..." />;
  }

  const selectedName = data ? describePeriod(data.period) : "Selected period";
  const tabs: TabItem<DetailView>[] = [
    { id: "selected", label: selectedName, icon: CalendarDays },
    { id: "overall", label: "All time", icon: History },
  ];
  const snapshot = data ? (view === "selected" ? data.selected : data.overall) : null;
  const scopeLabel = view === "selected" ? selectedName.toLowerCase() : "all time";
  const granularity = view === "selected" && data ? data.period.granularity : "month";

  return (
    <div className={`space-y-6 transition-opacity ${loading ? "opacity-60" : ""}`} aria-busy={loading}>
      <DashboardHeader
        title={data?.shopName ?? "Store performance"}
        subtitle="Revenue, orders, payments and Flash Deals for your shop."
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
          <StoreSummaryComparison period={data.period} selected={data.selected} overall={data.overall} />

          <div className="flex flex-wrap items-center gap-3">
            <p className="text-sm font-medium text-neutral-700">Show details for</p>
            <SegmentedTabs label="Detail view" items={tabs} value={view} onChange={setView} />
          </div>

          <StoreRevenueOverviewSection
            revenue={snapshot.revenueOverview}
            granularity={granularity}
            scopeLabel={scopeLabel}
          />

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <StoreOrderStatsSection orders={snapshot.orderStatistics} scopeLabel={scopeLabel} />
            <StorePaymentMethodSection paymentMethods={snapshot.revenueByPaymentMethod} scopeLabel={scopeLabel} />
          </div>

          <StoreFlashDealStatsSection deals={snapshot.flashDealStatistics} scopeLabel={scopeLabel} />
        </>
      )}

      <ErrorModal message={errorMessage} onClose={clearError} />
    </div>
  );
}
