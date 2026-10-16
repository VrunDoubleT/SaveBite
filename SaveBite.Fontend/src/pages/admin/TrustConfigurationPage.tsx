import { useState } from "react";
import { AdminLevelsPanel } from "@/features/trust/components/AdminLevelsPanel";
import { PenaltyRulesPanel } from "@/features/trust/components/PenaltyRulesPanel";
import { ScoreRulesPanel } from "@/features/trust/components/ScoreRulesPanel";
import { TrustTabs, type TrustTabItem } from "@/features/trust/components/TrustTabs";

type ConfigTab = "levels" | "penalties" | "score" | "earning";

const TABS: ReadonlyArray<TrustTabItem<ConfigTab>> = [
  { id: "levels", label: "Levels" },
  { id: "penalties", label: "Penalty rules" },
  { id: "score", label: "Trust Score rules" },
  { id: "earning", label: "Earning rules" },
];

export function TrustConfigurationPage() {
  const [activeTab, setActiveTab] = useState<ConfigTab>("levels");

  return (
    <div className="space-y-6">
      <header>
        <h2 className="text-2xl font-bold text-text-primary">Trust Score configuration</h2>
        <p className="mt-1 text-sm text-text-secondary">
          Manage levels, the points customers earn or lose, and no-show penalties.
        </p>
      </header>

      <TrustTabs
        tabs={TABS}
        active={activeTab}
        onChange={setActiveTab}
        ariaLabel="Trust Score configuration sections"
      />

      <div key={activeTab} role="tabpanel" className="trust-panel">
        {activeTab === "levels" && <AdminLevelsPanel />}
        {activeTab === "penalties" && <PenaltyRulesPanel />}
        {activeTab === "score" && <ScoreRulesPanel variant="all" />}
        {activeTab === "earning" && <ScoreRulesPanel variant="earning" />}
      </div>
    </div>
  );
}
