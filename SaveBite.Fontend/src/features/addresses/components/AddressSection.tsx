import { useEffect, useState } from "react";
import { LoaderCircle, MapPin, Navigation, Pencil, Plus, Star, Trash2 } from "lucide-react";
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
  }, []);

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
    <section
      aria-labelledby="addresses-heading"
      className="rounded-2xl border border-border-default bg-bg-surface p-6 sm:p-8"
    >
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2
            id="addresses-heading"
            className="text-xl font-semibold tracking-tight text-text-primary"
          >
            My Addresses
          </h2>
          <p className="mt-1 text-sm text-text-secondary">
            Manage the saved addresses for your account.
          </p>
        </div>

        {!isAdding && (
          <button
            type="button"
            onClick={openAddForm}
            className="inline-flex w-fit shrink-0 items-center gap-2 rounded-full border border-primary-200 bg-primary-50 px-5 py-2.5 text-sm font-semibold text-primary-700 transition-colors hover:bg-primary-100"
          >
            <Plus className="size-4" aria-hidden="true" />
            Add new address
          </button>
        )}
      </div>

      {isAdding && (
        <div className="mt-6">
          <AddressForm
            isFirstAddress={addresses.length === 0}
            onSaved={handleCreated}
            onCancel={() => setIsAdding(false)}
          />
        </div>
      )}

      <div className="mt-6">
        {isLoading && (
          <div className="flex items-center justify-center gap-2 py-8 text-sm text-text-secondary">
            <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
            Loading addresses...
          </div>
        )}

        {!isLoading && loadError && (
          <div className="rounded-xl border border-danger/20 bg-danger/5 px-4 py-3 text-sm text-danger">
            Unable to load addresses: {loadError}
          </div>
        )}

        {!isLoading && !loadError && addresses.length === 0 && !isAdding && (
          <div className="flex flex-col items-center rounded-xl border border-dashed border-border-default px-6 py-10 text-center">
            <span
              className="flex size-12 items-center justify-center rounded-full bg-primary-50 text-primary-600"
              aria-hidden="true"
            >
              <MapPin className="size-6" />
            </span>
            <p className="mt-3 text-sm font-semibold text-text-primary">No saved addresses yet</p>
            <p className="mt-1 text-sm text-text-secondary">
              Add an address to use it when placing orders.
            </p>
          </div>
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
    </section>
  );
}

const actionButtonClass =
  "inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-60";

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
    } catch (error) {
      setActionError(getApiErrorMessage(error));
    } finally {
      setPendingAction(null);
    }
  }

  const isBusy = pendingAction !== null;

  return (
    <li
      className={`flex gap-4 rounded-xl border p-4 transition-colors ${
        address.isDefault
          ? "border-primary-200 bg-primary-50/40"
          : "border-border-default bg-bg-surface hover:border-primary-200"
      }`}
    >
      <span
        className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary-50 text-primary-600"
        aria-hidden="true"
      >
        <MapPin className="size-5" />
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <h3 className="text-sm font-semibold text-text-primary">{address.label || "Address"}</h3>

          {address.isDefault && (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-primary-100 px-2.5 py-0.5 text-xs font-semibold text-primary-800 ring-1 ring-inset ring-primary-300">
              <span className="size-1.5 rounded-full bg-primary-600" aria-hidden="true" />
              Default
            </span>
          )}
        </div>

        <p className="mt-1 text-sm leading-relaxed text-text-secondary">{formatAddress(address)}</p>

        <p className="mt-3 inline-flex items-center gap-1.5 text-xs tabular-nums text-text-muted">
          <Navigation className="size-3.5" aria-hidden="true" />
          {formatCoordinate(address.latitude)} · {formatCoordinate(address.longitude)}
        </p>

        <div className="mt-4 flex flex-wrap items-center gap-2 border-t border-border-default pt-3">
          {!address.isDefault && (
            <button
              type="button"
              onClick={() => void runAction("default", onSetDefault)}
              disabled={isBusy}
              className={`${actionButtonClass} border-primary-200 bg-primary-50 text-primary-700 hover:bg-primary-100`}
            >
              {pendingAction === "default" ? (
                <LoaderCircle className="size-3.5 animate-spin" aria-hidden="true" />
              ) : (
                <Star className="size-3.5" aria-hidden="true" />
              )}
              {pendingAction === "default" ? "Setting..." : "Set default"}
            </button>
          )}

          <button
            type="button"
            onClick={onEdit}
            disabled={isBusy}
            className={`${actionButtonClass} border-border-default bg-bg-surface text-text-secondary hover:bg-bg-subtle`}
          >
            <Pencil className="size-3.5" aria-hidden="true" />
            Edit
          </button>

          {!address.isDefault && (
            <button
              type="button"
              onClick={() => void runAction("delete", onDelete)}
              disabled={isBusy}
              className={`${actionButtonClass} border-red-200 bg-red-50 text-danger hover:bg-red-100`}
            >
              {pendingAction === "delete" ? (
                <LoaderCircle className="size-3.5 animate-spin" aria-hidden="true" />
              ) : (
                <Trash2 className="size-3.5" aria-hidden="true" />
              )}
              {pendingAction === "delete" ? "Deleting..." : "Delete"}
            </button>
          )}
        </div>

        {actionError && <p className="mt-2 text-xs text-danger">{actionError}</p>}
      </div>
    </li>
  );
}
