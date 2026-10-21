import { Navigate, useParams } from "react-router-dom";
import { AdminLevelsPanel } from "@/features/trust/components/AdminLevelsPanel";
import { PenaltyRulesPanel } from "@/features/trust/components/PenaltyRulesPanel";
import { ScoreRulesPanel } from "@/features/trust/components/ScoreRulesPanel";
import { TrustTabs } from "@/features/trust/components/TrustTabs";
import {
  ADMIN_TRUST_BASE_PATH,
  ADMIN_TRUST_TABS,
  findTab,
  tabPath,
} from "@/features/trust/utils/trustTabs";

// Routes: /admin/trust/:tab (levels | penalty-rules | score-rules | earning-rules)
export function TrustConfigurationPage() {
  const { tab } = useParams();
  const activeTab = findTab(ADMIN_TRUST_TABS, tab);

  // /admin/trust and unknown tabs open the first tab (replaces the history entry).
  if (!activeTab) {
    return <Navigate to={tabPath(ADMIN_TRUST_BASE_PATH, ADMIN_TRUST_TABS[0].id)} replace />;
  }

  return (
    <div className="space-y-6">
      <header>
        <h2 className="text-2xl font-bold text-text-primary">Trust Score configuration</h2>
        <p className="mt-1 text-sm text-text-secondary">
          Manage levels, the points customers earn or lose, and no-show penalties.
        </p>
      </header>

      <TrustTabs
        tabs={ADMIN_TRUST_TABS}
        active={activeTab}
        basePath={ADMIN_TRUST_BASE_PATH}
        ariaLabel="Trust Score configuration sections"
      />

      <div key={activeTab} role="tabpanel" className="trust-panel">
        {activeTab === "levels" && <AdminLevelsPanel />}
        {activeTab === "penalty-rules" && <PenaltyRulesPanel />}
        {activeTab === "score-rules" && <ScoreRulesPanel variant="all" />}
        {activeTab === "earning-rules" && <ScoreRulesPanel variant="earning" />}
      </div>
    </div>
  );
}
