import { Navigate, useParams } from "react-router-dom";
import { TrustHistoryPanel } from "@/features/trust/components/TrustHistoryPanel";
import { TrustLevelsPanel } from "@/features/trust/components/TrustLevelsPanel";
import { TrustOverviewPanel } from "@/features/trust/components/TrustOverviewPanel";
import { TrustPenaltiesPanel } from "@/features/trust/components/TrustPenaltiesPanel";
import { TrustTabs } from "@/features/trust/components/TrustTabs";
import {
  CUSTOMER_TRUST_BASE_PATH,
  CUSTOMER_TRUST_TABS,
  findTab,
  tabPath,
} from "@/features/trust/utils/trustTabs";

// Routes: /account/trust-score/:tab (overview | levels | history | penalties)
export function TrustScorePage() {
  const { tab } = useParams();
  const activeTab = findTab(CUSTOMER_TRUST_TABS, tab);

  // /account/trust-score and unknown tabs open the first tab (replaces the history entry).
  if (!activeTab) {
    return <Navigate to={tabPath(CUSTOMER_TRUST_BASE_PATH, CUSTOMER_TRUST_TABS[0].id)} replace />;
  }

  return (
    <div className="space-y-6">
      <header>
        <h2 className="text-2xl font-bold text-text-primary">Trust Score &amp; Level</h2>
        <p className="mt-1 text-sm text-text-secondary">
          Your Trust Score decides your level and the benefits you get when ordering.
        </p>
      </header>

      <TrustTabs
        tabs={CUSTOMER_TRUST_TABS}
        active={activeTab}
        basePath={CUSTOMER_TRUST_BASE_PATH}
        ariaLabel="Trust Score sections"
      />

      <div key={activeTab} role="tabpanel" className="trust-panel">
        {activeTab === "overview" && <TrustOverviewPanel />}
        {activeTab === "levels" && <TrustLevelsPanel />}
        {activeTab === "history" && <TrustHistoryPanel />}
        {activeTab === "penalties" && <TrustPenaltiesPanel />}
      </div>
    </div>
  );
}
