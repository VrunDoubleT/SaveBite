const BROADCAST_CHANNEL_NAME = "savebite_feedback_channel";
const STORAGE_KEY = "savebite_feedback_sync";

export function emitFeedbackSync(action: "hide" | "restore", feedbackId?: string) {
  // 1. Same-window custom event
  window.dispatchEvent(
    new CustomEvent("feedback:invalidated", { detail: { action, feedbackId } }),
  );

  // 2. Cross-tab BroadcastChannel
  try {
    if (typeof BroadcastChannel !== "undefined") {
      const channel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
      channel.postMessage({ action, feedbackId, timestamp: Date.now() });
      channel.close();
    }
  } catch {}

  // 3. Cross-tab StorageEvent (works across all tabs of same browser)
  try {
    localStorage.setItem(STORAGE_KEY, `${action}:${feedbackId ?? ""}:${Date.now()}`);
  } catch {}
}

export function subscribeFeedbackSync(onSync: () => void): () => void {
  const handleEvent = () => onSync();

  window.addEventListener("feedback:invalidated", handleEvent);
  window.addEventListener("focus", handleEvent);

  let channel: BroadcastChannel | null = null;
  try {
    if (typeof BroadcastChannel !== "undefined") {
      channel = new BroadcastChannel(BROADCAST_CHANNEL_NAME);
      channel.onmessage = handleEvent;
    }
  } catch {}

  const handleStorage = (e: StorageEvent) => {
    if (e.key === STORAGE_KEY) {
      onSync();
    }
  };
  window.addEventListener("storage", handleStorage);

  return () => {
    window.removeEventListener("feedback:invalidated", handleEvent);
    window.removeEventListener("focus", handleEvent);
    window.removeEventListener("storage", handleStorage);
    channel?.close();
  };
}
