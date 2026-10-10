<<<<<<< HEAD
import { useState } from "react";

import LatestShopApplication from "@/features/account/shop-application/components/LatestShopApplication";
import ShopApplicationHistory from "@/features/account/shop-application/components/ShopApplicationHistory";

export default function ShopApplicationPage() {
  const [historyRefreshKey, setHistoryRefreshKey] = useState(0);

  return (
    <div className="w-full space-y-8">
      <LatestShopApplication
        onApplicationChanged={() => setHistoryRefreshKey((current) => current + 1)}
      />
      <ShopApplicationHistory refreshKey={historyRefreshKey} />
=======
export function ShopApplicationPage() {
  return (
    <div className="rounded-lg border border-border-default bg-bg-surface p-6">
      <h2 className="text-lg font-semibold text-text-primary">Shop Application</h2>

      <p className="mt-2 text-sm text-text-secondary">Your shop application will appear here.</p>
>>>>>>> origin/feature/iss-1-user-profile-address-management
    </div>
  );
}
