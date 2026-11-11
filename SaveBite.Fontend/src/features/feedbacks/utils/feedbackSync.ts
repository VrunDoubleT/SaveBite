const BROADCAST_CHANNEL_NAME = "savebite_feedback_channel";
const STORAGE_KEY = "savebite_feedback_sync";

export interface FeedbackSyncDetail {
  action?: "hide" | "restore" | string;
  feedbackId?: string;
  shopId?: string;
  productId?: string;
  rating?: number;
}

export function emitFeedbackSync(
  action: "hide" | "restore" | string,
  feedbackId?: string,
  extra?: Partial<FeedbackSyncDetail>,
) {
  const detail: FeedbackSyncDetail = { action, feedbackId, ...extra };

  // 1. Same-window custom event
  window.dispatchEvent(
    new CustomEvent("feedback:invalidated", { detail }),
  );

  // 2. Cross-tab BroadcastChannel
  try {
    if (typeof BroadcastChannel !== "undefined") {
      const channel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
      channel.postMessage({ ...detail, timestamp: Date.now() });
      channel.close();
    }
  } catch {}

  // 3. Cross-tab StorageEvent (works across all tabs of same browser)
  try {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ ...detail, timestamp: Date.now() }),
    );
  } catch {}
}

export function subscribeFeedbackSync(
  onSync: (detail?: FeedbackSyncDetail) => void,
): () => void {
  const handleCustomEvent = (e: Event) => {
    const detail = (e as CustomEvent<FeedbackSyncDetail>).detail;
    onSync(detail);
  };

  const handleFocus = () => onSync();

  window.addEventListener("feedback:invalidated", handleCustomEvent);
  window.addEventListener("focus", handleFocus);

  let channel: BroadcastChannel | null = null;
  try {
    if (typeof BroadcastChannel !== "undefined") {
      channel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
      channel.onmessage = (e: MessageEvent<FeedbackSyncDetail>) => {
        onSync(e.data);
      };
    }
  } catch {}

  const handleStorage = (e: StorageEvent) => {
    if (e.key === STORAGE_KEY && e.newValue) {
      try {
        const parsed = JSON.parse(e.newValue) as FeedbackSyncDetail;
        onSync(parsed);
      } catch {
        onSync();
      }
    }
  };
  window.addEventListener("storage", handleStorage);

  return () => {
    window.removeEventListener("feedback:invalidated", handleCustomEvent);
    window.removeEventListener("focus", handleFocus);
    window.removeEventListener("storage", handleStorage);
    channel?.close();
  };
}
