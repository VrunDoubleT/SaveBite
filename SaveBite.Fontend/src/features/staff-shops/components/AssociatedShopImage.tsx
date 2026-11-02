import { useState } from "react";
import { Store } from "lucide-react";
import type { AssociatedShop } from "../types/staff.types";

export function AssociatedShopImage({ shop }: { shop: AssociatedShop }) {
  const [failedUrls, setFailedUrls] = useState<string[]>([]);
  const imageUrl = [shop.shopLogoUrl, shop.shopCoverImageUrl].find(
    (url): url is string => Boolean(url) && !failedUrls.includes(url!),
  );

  return (
    <div className="flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-md bg-primary-50 text-primary-700">
      {imageUrl ? (
        <img
          src={imageUrl}
          alt={shop.shopName}
          loading="lazy"
          onError={() => setFailedUrls((current) => [...current, imageUrl])}
          className="h-full w-full object-cover"
        />
      ) : (
        <Store size={22} aria-hidden="true" />
      )}
    </div>
  );
}
