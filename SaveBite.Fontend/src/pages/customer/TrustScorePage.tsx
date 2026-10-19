import { useState } from "react";
import { TrustHistoryPanel } from "@/features/trust/components/TrustHistoryPanel";
import { TrustLevelsPanel } from "@/features/trust/components/TrustLevelsPanel";
import { TrustOverviewPanel } from "@/features/trust/components/TrustOverviewPanel";
import { TrustPenaltiesPanel } from "@/features/trust/components/TrustPenaltiesPanel";
import { TrustTabs, type TrustTabItem } from "@/features/trust/components/TrustTabs";

type TrustTab = "overview" | "levels" | "history" | "penalties";

const TABS: ReadonlyArray<TrustTabItem<TrustTab>> = [
  { id: "overview", label: "Overview" },
  { id: "levels", label: "Levels & benefits" },
  { id: "history", label: "Score history" },
  { id: "penalties", label: "Penalties" },
];

export function TrustScorePage() {
  const [activeTab, setActiveTab] = useState<TrustTab>("overview");

  return (
    <div className="space-y-6">
      <header>
        <h2 className="text-2xl font-bold text-text-primary">Trust Score &amp; Level</h2>
        <p className="mt-1 text-sm text-text-secondary">
          Your Trust Score decides your level and the benefits you get when ordering.
        </p>
      </header>

      <TrustTabs
        tabs={TABS}
        active={activeTab}
        onChange={setActiveTab}
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
