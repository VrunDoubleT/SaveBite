import { useCallback, useEffect, useState } from "react";
import { LoaderCircle, Store } from "lucide-react";
import { ownerShopApi } from "@/features/shops/api/ownerShopApi";
import { ShopProfileHeader } from "@/features/shops/components/ShopProfileHeader";
import { ShopProfileDetails } from "@/features/shops/components/ShopProfileDetails";
import { ShopBusinessInfoForm } from "@/features/shops/components/ShopBusinessInfoForm";
import type { OwnerShop, UpdateBusinessInfo } from "@/features/shops/types/ownerShop.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";

export function ShopProfilePage() {
  const [shop, setShop] = useState<OwnerShop | null>(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const loadProfile = useCallback(async () => {
    setLoading(true);
    try {
      setShop(await ownerShopApi.getMyShopProfile());
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load your shop profile.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadProfile();
  }, [loadProfile]);

  async function saveBusinessInfo(
    input: UpdateBusinessInfo,
    logo: File | null,
    coverImage: File | null,
  ) {
    setIsSaving(true);
    try {
      await ownerShopApi.updateBusinessInfo(input);
      if (logo || coverImage) await ownerShopApi.updateImages({ logo, coverImage });
      await loadProfile();
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
      <div className="flex min-h-64 items-center justify-center gap-3 text-text-secondary">
        <LoaderCircle className="animate-spin" /> Loading shop profile…
      </div>
    );
  if (!shop)
    return (
      <section className="mx-auto max-w-3xl rounded-2xl border border-border-default bg-bg-surface p-8 text-center">
        <Store className="mx-auto mb-3 text-primary-600" size={36} />
        <h1 className="text-xl font-bold text-text-primary">Shop profile unavailable</h1>
        <p className="mt-2 text-sm text-text-secondary">
          We couldn't load the shop associated with your account.
        </p>
        <button
          onClick={() => void loadProfile()}
          className="mt-5 rounded-lg bg-primary-600 px-4 py-2 font-semibold text-white hover:bg-primary-700"
        >
          Try again
        </button>
      </section>
    );

  return (
    <div className="space-y-6">
      <header>
        <p className="mb-2 text-xs font-bold tracking-[0.16em] text-primary-700">
          STORE OWNER WORKSPACE
        </p>
        <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
          Shop Profile
        </h1>
        <p className="mt-2 text-sm text-text-secondary">
          View your shop details, registration documents and current status.
        </p>
      </header>
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
