import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { useAuthStore } from "@/shared/stores/authStore";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { feedbackApi } from "../api/feedbackApi";
import { subscribeFeedbackSync } from "../utils/feedbackSync";

export function ShopReviewsBadge({ shopId }: { shopId?: string }) {
  const userId = useAuthStore((state) => state.user?.id);
  const realtime = useRealtimeSubscriptions();
  const { key } = useLocation();
  const [result, setResult] = useState<{ scope: string; count: number }>();
  const scope = `${userId}:${shopId ?? "owner"}`;

  useEffect(() => {
    if (!userId) return;
    const controller = new AbortController();
    let reading = false;
    let dirty = false;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        while (dirty && !controller.signal.aborted) {
          dirty = false;
          const response = await feedbackApi.getShopFeedbacks(
            { shopId, page: 1, pageSize: 1, needsReplyOnly: true },
            controller.signal,
          );
          if (!controller.signal.aborted) setResult({ scope, count: response.totalItems });
        }
      } catch {
        // Keep the last count and retry on navigation, focus or a relevant event.
      } finally {
        reading = false;
      }
    }
    const update = () => { void refresh(); };
    const off = [
      subscribeFeedbackSync(update),
      realtime.on(REALTIME_EVENTS.FEEDBACK_CHANGED, update),
      realtime.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, update),
      realtime.onConnected((again) => { if (again) update(); }),
    ];
    update();
    return () => {
      controller.abort();
      off.forEach((unsubscribe) => unsubscribe());
    };
  }, [userId, shopId, scope, realtime, key]);

  const count = result?.scope === scope ? result.count : 0;
  if (count <= 0) return null;
  const label = `${count} unanswered 1–2 star reviews`;
  return (
    <span
      aria-live="polite"
      aria-atomic="true"
      aria-label={label}
      title={label}
      className="inline-flex min-w-5 items-center justify-center rounded-full bg-emerald-600 px-1.5 py-0.5 text-xs font-bold leading-4 text-white"
    >
      {count}
    </span>
  );
}
