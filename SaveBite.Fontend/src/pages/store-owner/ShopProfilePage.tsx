import { useCallback, useEffect, useState } from "react";
import { LoaderCircle, Store } from "lucide-react";
import { ownerShopApi } from "@/features/shops/api/ownerShopApi";
import { ShopProfileHeader } from "@/features/shops/components/ShopProfileHeader";
import { ShopProfileDetails } from "@/features/shops/components/ShopProfileDetails";
import { ShopBusinessInfoForm } from "@/features/shops/components/ShopBusinessInfoForm";
import type { OwnerShop, UpdateBusinessInfo } from "@/features/shops/types/ownerShop.types";
import { subscribeShopStatusEvents } from "@/features/shops/realtime/subscribeShopStatusEvents";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";

export function ShopProfilePage() {
  const [shop, setShop] = useState<OwnerShop | null>(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const realtime = useRealtimeSubscriptions();

  // Reloads the profile on demand: after a realtime status change, on "Try again",
  // and after saving. It is intentionally not called directly inside a useEffect.
  const reloadProfile = useCallback(async () => {
    try {
      setShop(await ownerShopApi.getMyShopProfile());
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load your shop profile.");
    } finally {
      setLoading(false);
    }
  }, []);

  // Initial load: setState is only called inside promise callbacks,
  // so the react-hooks/set-state-in-effect rule is satisfied.
  useEffect(() => {
    let cancelled = false;
    ownerShopApi
      .getMyShopProfile()
      .then((data) => {
        if (!cancelled) setShop(data);
      })
      .catch((error) => {
        if (!cancelled)
          toast.error(getApiErrorMessage(error) || "Unable to load your shop profile.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true; // Ignore the result if the component unmounts mid-request.
    };
  }, []);

  // SignalR: when an admin locks/unlocks the shop, silently reload the profile
  // so the status and reason update immediately without a page refresh.
  useEffect(
    () => subscribeShopStatusEvents(realtime, () => void reloadProfile()),
    [realtime, reloadProfile],
  );

  async function saveBusinessInfo(
    input: UpdateBusinessInfo,
    logo: File | null,
    coverImage: File | null,
  ) {
    setIsSaving(true);
    try {
      await ownerShopApi.updateBusinessInfo(input);
      if (logo || coverImage) await ownerShopApi.updateImages({ logo, coverImage });
      await reloadProfile();
      setIsEditing(false);
      toast.success("Shop profile updated successfully.");
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to save shop profile.");
      throw error;
    } finally {
      setIsSaving(false);
    }
  }

  if (loading && !shop)
    return (
      <div className="flex min-h-64 items-center justify-center gap-3 text-neutral-500">
        <LoaderCircle className="animate-spin text-emerald-600" /> Loading shop profile…
      </div>
    );
  if (!shop)
    return (
      <section className="mx-auto max-w-3xl rounded-xl border border-neutral-200 bg-white p-8 text-center shadow-2xs">
        <Store className="mx-auto mb-3 text-emerald-600" size={36} />
        <h1 className="text-xl font-bold text-neutral-900">Shop profile unavailable</h1>
        <p className="mt-2 text-sm text-neutral-500">
          We couldn't load the shop associated with your account.
        </p>
        <button
          onClick={() => {
            setLoading(true);
            void reloadProfile();
          }}
          className="mt-5 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-700"
        >
          Try again
        </button>
      </section>
    );

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Shop Profile</h1>
          <p className="mt-1 text-sm text-neutral-500">
            View your shop details, registration documents and current status.
          </p>
        </div>
      </div>
      {isEditing ? (
        <ShopBusinessInfoForm
          shop={shop}
          isSaving={isSaving}
          onCancel={() => setIsEditing(false)}
          onSubmit={saveBusinessInfo}
        />
      ) : (
        <>
          <ShopProfileHeader shop={shop} onEdit={() => setIsEditing(true)} />
          <ShopProfileDetails shop={shop} />
        </>
      )}
    </div>
  );
}

export default ShopProfilePage;