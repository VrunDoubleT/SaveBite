import { CustomerActionButton } from "@/shared/components/CustomerMutationControls";
import { Badge, Card, EmptyState, ErrorState, Skeleton } from "@/shared/components/ui";
import { toast } from "@/shared/stores/toastStore";
import { useEffect, useState } from "react";
import { MapPin, Navigation, Pencil, Plus, Star, Trash2 } from "lucide-react";
import { addressApi } from "@/features/addresses/api/addressApi";
import { AddressForm } from "@/features/addresses/components/AddressForm";
import type { UserAddress } from "@/features/addresses/types/address.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";

function formatAddress(address: UserAddress): string {
  return [address.addressLine, address.ward, address.district, address.city]
    .filter(Boolean)
    .join(", ");
}

function formatCoordinate(value: number): string {
  return value.toFixed(4);
}

export function AddressSection() {
  const [loadAttempt, setLoadAttempt] = useState(0);
  const [addresses, setAddresses] = useState<UserAddress[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    async function loadAddresses() {
      try {
        const data = await addressApi.getAddresses();

        if (mounted) setAddresses(data);
      } catch (error) {
        if (mounted) setLoadError(getApiErrorMessage(error));
      } finally {
        if (mounted) setIsLoading(false);
      }
    }

    void loadAddresses();

    return () => {
      mounted = false;
    };
  }, [loadAttempt]);

  function openAddForm() {
    setEditingId(null);
    setIsAdding(true);
  }

  function openEditForm(addressId: string) {
    setIsAdding(false);
    setEditingId(addressId);
  }

  function handleCreated(created: UserAddress) {
    setAddresses((current) =>
      created.isDefault
        ? [created, ...current.map((address) => ({ ...address, isDefault: false }))]
        : [...current, created],
    );
    setIsAdding(false);
    toast.success("Address added.");
  }

  function handleUpdated(updated: UserAddress) {
    setAddresses((current) =>
      updated.isDefault
        ? [
            updated,
            ...current
              .filter((address) => address.id !== updated.id)
              .map((address) => ({ ...address, isDefault: false })),
          ]
        : current.map((address) => (address.id === updated.id ? updated : address)),
    );
    setEditingId(null);
    toast.success("Address updated.");
  }

  async function handleSetDefault(addressId: string) {
    await addressApi.setDefaultAddress(addressId);

    setAddresses((current) => {
      const target = current.find((address) => address.id === addressId);

      if (!target) return current;

      return [
        { ...target, isDefault: true },
        ...current
          .filter((address) => address.id !== addressId)
          .map((address) => ({ ...address, isDefault: false })),
      ];
    });
  }

  async function handleDelete(addressId: string) {
    await addressApi.deleteAddress(addressId);

    setAddresses((current) => current.filter((address) => address.id !== addressId));
  }

  return (
    <Card padding="compact" aria-labelledby="addresses-heading">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2
            id="addresses-heading"
            className="flex items-center gap-2 text-section font-semibold text-text-primary"
          >
            <span className="flex size-8 shrink-0 items-center justify-center rounded-md bg-secondary-50 text-secondary-700">
              <MapPin className="size-4" aria-hidden="true" />
            </span>
            My addresses
          </h2>
          <p className="mt-1 text-body text-text-secondary">
            Manage the saved addresses for your account.
          </p>
        </div>

        {!isAdding && (
          <CustomerActionButton
            type="button"
            onClick={openAddForm}
            disabled={isLoading}
            className="min-h-11"
          >
            <Plus className="size-4" aria-hidden="true" />
            Add new address
          </CustomerActionButton>
        )}
      </div>

      {isAdding && (
        <div className="mt-4">
          <AddressForm
            isFirstAddress={addresses.length === 0}
            onSaved={handleCreated}
            onCancel={() => setIsAdding(false)}
          />
        </div>
      )}

      <div className="mt-4">
        {isLoading && (
          <div role="status" aria-label="Loading addresses" className="space-y-3">
            <span className="sr-only">Loading addresses…</span>
            {[0, 1].map((index) => (
              <Skeleton key={index} className="h-36" />
            ))}
          </div>
        )}
        {!isLoading && loadError && (
          <ErrorState
            title="Couldn't load your addresses"
            description={loadError}
            onRetry={() => {
              setLoadError(null);
              setIsLoading(true);
              setLoadAttempt((value) => value + 1);
            }}
          />
        )}
        {!isLoading && !loadError && addresses.length === 0 && !isAdding && (
          <EmptyState
            title="No saved addresses yet"
            description="Save a location to find nearby deals more easily."
            icon={MapPin}
            action={
              <CustomerActionButton variant="primary" onClick={openAddForm}>
                <Plus className="size-4" aria-hidden="true" />
                Add an address
              </CustomerActionButton>
            }
          />
        )}

        {!isLoading && !loadError && addresses.length > 0 && (
          <ul className="space-y-3">
            {addresses.map((address) =>
              address.id === editingId ? (
                <li key={address.id}>
                  <AddressForm
                    address={address}
                    onSaved={handleUpdated}
                    onCancel={() => setEditingId(null)}
                  />
                </li>
              ) : (
                <AddressItem
                  key={address.id}
                  address={address}
                  onEdit={() => openEditForm(address.id)}
                  onSetDefault={() => handleSetDefault(address.id)}
                  onDelete={() => handleDelete(address.id)}
                />
              ),
            )}
          </ul>
        )}
      </div>
    </Card>
  );
}

interface AddressItemProps {
  address: UserAddress;
  onEdit: () => void;
  onSetDefault: () => Promise<void>;
  onDelete: () => Promise<void>;
}

function AddressItem({ address, onEdit, onSetDefault, onDelete }: AddressItemProps) {
  const [pendingAction, setPendingAction] = useState<"default" | "delete" | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function runAction(action: "default" | "delete", task: () => Promise<void>) {
    setPendingAction(action);
    setActionError(null);

    try {
      await task();
      toast.success(action === "default" ? "Default address updated." : "Address deleted.");
    } catch (error) {
      setActionError(getApiErrorMessage(error));
    } finally {
      setPendingAction(null);
    }
  }

  const isBusy = pendingAction !== null;

  return (
    <li
      className={`flex min-w-0 gap-3 rounded-md border p-3 ${
        address.isDefault
          ? "border-primary-200 bg-primary-50/50"
          : "border-border-default bg-bg-surface hover:border-primary-200"
      }`}
    >
      <span
        className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary-50 text-primary-700"
        aria-hidden="true"
      >
        <MapPin className="size-5" />
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <h3
            title={address.label || "Address"}
            className="text-body font-semibold text-text-primary [overflow-wrap:anywhere]"
          >
            {address.label || "Address"}
          </h3>

          {address.isDefault && <Badge tone="primary">Default</Badge>}
        </div>

        <p className="mt-2 text-body leading-relaxed text-text-secondary [overflow-wrap:anywhere]">
          {formatAddress(address)}
        </p>

        <p className="mt-3 inline-flex items-center gap-1.5 text-caption tabular-nums text-text-muted">
          <Navigation className="size-3.5" aria-hidden="true" />
          {formatCoordinate(address.latitude)} · {formatCoordinate(address.longitude)}
        </p>

        <div className="mt-3 flex flex-wrap items-center gap-2 border-t border-border-default pt-3">
          {!address.isDefault && (
            <CustomerActionButton
              variant="secondary"
              type="button"
              onClick={() => void runAction("default", onSetDefault)}
              disabled={isBusy}
              className="min-h-11 text-primary-700"
              loading={pendingAction === "default"}
            >
              <Star className="size-4" aria-hidden="true" />
              {pendingAction === "default" ? "Setting..." : "Set default"}
            </CustomerActionButton>
          )}

          <CustomerActionButton
            variant="secondary"
            type="button"
            onClick={onEdit}
            disabled={isBusy}
            className="min-h-11"
          >
            <Pencil className="size-3.5" aria-hidden="true" />
            Edit
          </CustomerActionButton>

          {!address.isDefault && (
            <CustomerActionButton
              variant="secondary"
              type="button"
              onClick={() => void runAction("delete", onDelete)}
              disabled={isBusy}
              className="min-h-11 text-danger-strong hover:bg-danger-soft"
              loading={pendingAction === "delete"}
            >
              <Trash2 className="size-4" aria-hidden="true" />
              {pendingAction === "delete" ? "Deleting..." : "Delete"}
            </CustomerActionButton>
          )}
        </div>

        {actionError && (
          <p role="alert" className="mt-2 text-body text-danger-strong">
            {actionError}
          </p>
        )}
      </div>
    </li>
  );
}
