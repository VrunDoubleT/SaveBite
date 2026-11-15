const BROADCAST_CHANNEL_NAME = "savebite_product_channel";
const STORAGE_KEY = "savebite_product_sync";

export interface ProductSyncDetail {
  action?: "hide" | "restore" | string;
  productId?: string;
  shopId?: string;
}

export function emitProductSync(
  action: "hide" | "restore" | string,
  productId?: string,
  extra?: Partial<ProductSyncDetail>,
) {
  const detail: ProductSyncDetail = { action, productId, ...extra };

  // 1. Same-window custom event
  window.dispatchEvent(
    new CustomEvent("product:invalidated", { detail }),
  );

  // 2. Cross-tab BroadcastChannel
  try {
    if (typeof BroadcastChannel !== "undefined") {
      const channel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
      channel.postMessage({ ...detail, timestamp: Date.now() });
      channel.close();
    }
  } catch {
    // Browser privacy settings may disable cross-tab APIs; local events still work.
  }

  // 3. Cross-tab StorageEvent (works across all tabs of same browser)
  try {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ ...detail, timestamp: Date.now() }),
    );
  } catch {
    // Browser privacy settings may disable cross-tab APIs; local events still work.
  }
}

export function subscribeProductSync(
  onSync: (detail?: ProductSyncDetail) => void,
): () => void {
  const handleCustomEvent = (e: Event) => {
    const detail = (e as CustomEvent<ProductSyncDetail>).detail;
    onSync(detail);
  };

  window.addEventListener("product:invalidated", handleCustomEvent);

  let channel: BroadcastChannel | null = null;
  try {
    if (typeof BroadcastChannel !== "undefined") {
      channel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
      channel.onmessage = (e: MessageEvent<ProductSyncDetail>) => {
        onSync(e.data);
      };
    }
  } catch {
    // Browser privacy settings may disable cross-tab APIs; local events still work.
  }

  const handleStorage = (e: StorageEvent) => {
    if (e.key === STORAGE_KEY && e.newValue) {
      try {
        const parsed = JSON.parse(e.newValue) as ProductSyncDetail;
        onSync(parsed);
      } catch {
        onSync();
      }
    }
  };
  window.addEventListener("storage", handleStorage);

  return () => {
    window.removeEventListener("product:invalidated", handleCustomEvent);
    window.removeEventListener("storage", handleStorage);
    channel?.close();
  };
}
