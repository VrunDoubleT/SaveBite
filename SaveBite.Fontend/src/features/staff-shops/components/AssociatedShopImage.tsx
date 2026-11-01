import { useState } from "react";
import { Store } from "lucide-react";
import type { AssociatedShop } from "../types/staff.types";

export function AssociatedShopImage({ shop }: { shop: AssociatedShop }) {
  const [failedUrls, setFailedUrls] = useState<string[]>([]);
  const imageUrl = [shop.shopLogoUrl, shop.shopCoverImageUrl].find(
    (url): url is string => Boolean(url) && !failedUrls.includes(url!),
  );

  return (
    <div className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-emerald-50 text-emerald-600">
      {imageUrl ? (
        <img
          src={imageUrl}
          alt={shop.shopName}
          loading="lazy"
          onError={() => setFailedUrls((current) => [...current, imageUrl])}
          className="h-full w-full object-cover"
        />
      ) : (
        <Store size={28} aria-hidden="true" />
      )}
    </div>
  );
}
