import { Navigate, useNavigate, useParams } from "react-router-dom";
import { TrustHistoryPanel } from "@/features/trust/components/TrustHistoryPanel";
import { TrustLevelsPanel } from "@/features/trust/components/TrustLevelsPanel";
import { TrustOverviewPanel } from "@/features/trust/components/TrustOverviewPanel";
import { TrustPenaltiesPanel } from "@/features/trust/components/TrustPenaltiesPanel";
import { Tabs } from "@/shared/components/ui";
import { Award, Gift, History, ShieldAlert } from "lucide-react";
import "@/features/trust/styles/trust.css";
import {
  CUSTOMER_TRUST_BASE_PATH,
  CUSTOMER_TRUST_TABS,
  findTab,
  tabPath,
} from "@/features/trust/utils/trustTabs";

// Routes: /account/trust-score/:tab (overview | levels | history | penalties)
export function TrustScorePage() {
  const { tab } = useParams();
  const navigate = useNavigate();
  const activeTab = findTab(CUSTOMER_TRUST_TABS, tab);

  // /account/trust-score and unknown tabs open the first tab (replaces the history entry).
  if (!activeTab) {
    return <Navigate to={tabPath(CUSTOMER_TRUST_BASE_PATH, CUSTOMER_TRUST_TABS[0].id)} replace />;
  }

  return (
    <div className="space-y-4">
      <Tabs
        appearance="cards"
        value={activeTab}
        onChange={(value) => navigate(tabPath(CUSTOMER_TRUST_BASE_PATH, value))}
        label="Trust Score sections"
        items={CUSTOMER_TRUST_TABS.map((item, index) => ({
          value: item.id,
          label: item.label,
          icon: [Award, Gift, History, ShieldAlert][index],
          description: ["Your membership", "Compare benefits", "Your points", "Recovery progress"][
            index
          ],
        }))}
      >
        <div key={activeTab} className="trust-panel !min-h-0">
          {activeTab === "overview" && <TrustOverviewPanel />}
          {activeTab === "levels" && <TrustLevelsPanel />}
          {activeTab === "history" && <TrustHistoryPanel />}
          {activeTab === "penalties" && <TrustPenaltiesPanel />}
        </div>
      </Tabs>
    </div>
  );
}
