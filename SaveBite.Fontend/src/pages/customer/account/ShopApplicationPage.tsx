import { useState } from "react";

import LatestShopApplication from "@/features/account/shop-application/components/LatestShopApplication";
import ShopApplicationHistory from "@/features/account/shop-application/components/ShopApplicationHistory";

export default function ShopApplicationPage() {
  const [historyRefreshKey, setHistoryRefreshKey] = useState(0);

  return (
    <div className="w-full space-y-8">
      <LatestShopApplication onApplicationChanged={() => setHistoryRefreshKey((current) => current + 1)} />
      <ShopApplicationHistory refreshKey={historyRefreshKey} />
    </div>
  );
}
