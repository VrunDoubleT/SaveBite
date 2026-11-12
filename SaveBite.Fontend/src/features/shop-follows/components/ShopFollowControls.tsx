import { InlineSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useEffect, useRef, useState } from "react";
import { Bell, BellOff, Check, Plus } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import { shopFollowApi, type ShopFollow } from "../api/shopFollowApi";
import { Button } from "@/shared/components/ui";
import { useAuthStore } from "@/shared/stores/authStore";
import { getApiErrorMessage } from "@/shared/api";
import { shopApi } from "@/features/shops/api/shopApi";
import { toast } from "@/shared/stores/toastStore";

interface Props {
  shopId: string;
  ownerUserId?: string;
  shopName: string;
  compact?: boolean;
  initialFollow?: ShopFollow | null;
  onChange?: (follow: ShopFollow | null) => void;
}
export function ShopFollowControls(props: Props) {
  const user = useAuthStore((state) => state.user);
  if (user?.id && user.id === props.ownerUserId) return null;
  // Reset pending requests and local state when the shop or signed-in customer changes.
  return (
    <FollowControls
      key={`${user?.id ?? "guest"}:${props.shopId}:${props.initialFollow === undefined ? "fetch" : (props.initialFollow?.followedAt ?? "none")}`}
      {...props}
    />
  );
}
function FollowControls({ shopId, shopName, initialFollow, onChange, compact = false }: Props) {
  const user = useAuthStore((state) => state.user);
  const initializing = useAuthStore((state) => state.isInitializing);
  const authenticated = useAuthStore((state) => state.isAuthenticated);
  const [follow, setFollow] = useState<ShopFollow | null>(initialFollow ?? null);
  const [loading, setLoading] = useState(Boolean(authenticated && initialFollow === undefined));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);
  const lock = useRef(false);
  const navigate = useNavigate();
  const location = useLocation();
  useEffect(() => {
    if (!authenticated || user?.role === "admin" || initialFollow !== undefined) return;
    const controller = new AbortController();
    shopApi
      .getShopProfile(shopId, controller.signal)
      .then((value) => {
        if (!controller.signal.aborted) {
          setFollow(value.currentUserFollow ?? null);
          setError(null);
        }
      })
      .catch((cause: unknown) => {
        if (!controller.signal.aborted) setError(getApiErrorMessage(cause));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [authenticated, user?.role, shopId, initialFollow, attempt]);

  if (user?.role === "admin") return null;
  async function mutate(action: "follow" | "notifications") {
    if (useAuthStore.getState().user?.customerStatus === "suspended") return;
    if (!authenticated) {
      navigate("/login", { state: { from: location } });
      return;
    }
    if (lock.current) return;
    lock.current = true;
    setBusy(true);
    try {
      let next: ShopFollow | null;
      if (action === "notifications" && follow) {
        next = await shopFollowApi.settings(
          shopId,
          follow.notificationStatus === "Enabled" ? "Disabled" : "Enabled",
        );
        toast.success(
          next.notificationStatus === "Enabled"
            ? "Deal notifications enabled."
            : "Deal notifications turned off.",
        );
      } else if (follow) {
        await shopFollowApi.unfollow(shopId);
        next = null;
        toast.success(`Unfollowed ${shopName}.`);
      } else {
        next = await shopFollowApi.follow(shopId);
        toast.success(`Following ${shopName}. Deal notifications are on.`);
      }
      setFollow(next);
      onChange?.(next);
    } catch (cause) {
      toast.error(getApiErrorMessage(cause));
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  if (error)
    return (
      <div role="alert" className="space-y-2 text-caption text-text-secondary">
        <p>Couldn’t load follow settings. {error}</p>
        <Button
          variant="secondary"
          onClick={() => {
            setError(null);
            setLoading(true);
            setAttempt((value) => value + 1);
          }}
        >
          Retry
        </Button>
      </div>
    );
  const enabled = follow?.notificationStatus === "Enabled";
  return (
    <div className={compact ? "min-w-0 space-y-1" : "w-full min-w-0 space-y-2"}>
      <div
        className={
          compact
            ? "flex flex-wrap items-center gap-2"
            : `grid items-center gap-2 ${follow ? "grid-cols-[minmax(0,1fr)_44px]" : "grid-cols-1"}`
        }
      >
        <Button
          variant={compact ? "secondary" : "primary"}
          size={compact ? "sm" : "md"}
          className={`${compact ? "min-h-9 text-primary-700" : "min-h-11 w-full"} focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700`}
          disabled={loading || initializing || busy || user?.customerStatus === "suspended"}
          loading={loading || busy}
          aria-pressed={Boolean(follow)}
          onClick={() => void mutate("follow")}
        >
          {follow ? (
            <Check className="size-4" aria-hidden="true" />
          ) : (
            <Plus className="size-4" aria-hidden="true" />
          )}
          {loading ? (
            <InlineSkeleton label="Loading follow status" className="w-24" />
          ) : follow ? (
            "Unfollow store"
          ) : (
            "Follow store"
          )}
        </Button>
        {follow && (
          <Button
            variant={enabled && !compact ? "primary" : "secondary"}
            className={`${compact ? "size-9 min-h-9 text-primary-700" : "size-11"} !p-0 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700`}
            disabled={busy || user?.customerStatus === "suspended"}
            aria-label={`${enabled ? "Turn off" : "Turn on"} deal notifications for ${shopName}`}
            aria-pressed={enabled}
            onClick={() => void mutate("notifications")}
          >
            {enabled ? (
              <Bell className={compact ? "size-4" : "size-5"} aria-hidden="true" />
            ) : (
              <BellOff className={compact ? "size-4" : "size-5"} aria-hidden="true" />
            )}
            <span className="sr-only">Notification settings</span>
          </Button>
        )}
      </div>
      {follow && (
        <p
          className={
            compact
              ? "sr-only"
              : `text-caption font-semibold ${enabled ? "text-primary-700" : "text-text-secondary"}`
          }
          role="status"
        >
          Deal notifications {enabled ? "on" : "off"}
        </p>
      )}
    </div>
  );
}
