import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Check, LoaderCircle, MapPin, X } from "lucide-react";

import { addressApi } from "@/features/addresses/api/addressApi";
import type {
  CreateUserAddressInput,
  UpdateAddressInput,
  UserAddress,
} from "@/features/addresses/types/address.types";
import {
  createAddressSchema,
  updateAddressSchema,
  type CreateAddressFormValues,
  type UpdateAddressFormValues,
} from "@/features/addresses/schemas/address.schema";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { AddressLocationMap } from "@/features/addresses/components/AddressLocationMap";
import { useAddressLocation } from "@/features/addresses/hooks/useAddressLocation";
import {
  parseCoordinates,
  type AddressCoordinates,
  type ResolvedAddress,
} from "@/features/addresses/utils/addressLocation";

interface AddressFormProps {
  /**
   * If provided, the form edits the existing address.
   * If omitted, the form creates a new address.
   */
  address?: UserAddress;
  isFirstAddress?: boolean;
  onSaved: (address: UserAddress) => void;
  onCancel: () => void;
}

const inputClass =
  "mt-1.5 block w-full rounded-xl border border-border-default bg-bg-surface px-4 py-2.5 text-sm font-normal text-text-primary outline-none transition placeholder:text-text-muted focus:border-primary-500 focus:ring-2 focus:ring-primary-100 aria-[invalid=true]:border-danger aria-[invalid=true]:focus:ring-danger/20";

const buttonBaseClass =
  "inline-flex items-center justify-center gap-2 rounded-full border px-5 py-2.5 text-sm font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-60";

const buttonVariants = {
  soft: "border-primary-200 bg-primary-50 text-primary-700 hover:bg-primary-100",
  outline: "border-border-default bg-bg-surface text-text-secondary hover:bg-bg-subtle",
  solid: "border-primary-600 bg-primary-600 text-white hover:bg-primary-700",
} as const;

function buttonClass(variant: keyof typeof buttonVariants) {
  return `${buttonBaseClass} ${buttonVariants[variant]}`;
}

type AddressFormValues = CreateAddressFormValues | UpdateAddressFormValues;

function getEmptyValues(isFirstAddress: boolean): AddressFormValues {
  return {
    label: "",
    addressLine: "",
    ward: "",
    district: "",
    city: "",
    latitude: "",
    longitude: "",
    isDefault: isFirstAddress,
  };
}

function toFormValues(address: UserAddress): AddressFormValues {
  return {
    label: address.label ?? "",
    addressLine: address.addressLine,
    ward: address.ward ?? "",
    district: address.district ?? "",
    city: address.city ?? "",
    latitude: String(address.latitude),
    longitude: String(address.longitude),
    isDefault: address.isDefault,
  };
}

export function AddressForm({
  address,
  isFirstAddress = false,
  onSaved,
  onCancel,
}: AddressFormProps) {
  const isEditing = Boolean(address);

  /**
   * The default flag is locked when unchecking it would leave the user
   * without any default address:
   * - creating the first address, or
   * - editing the address that is currently the default.
   * To change the default, the user sets another address as default.
   */
  const isDefaultLocked = isEditing ? Boolean(address?.isDefault) : isFirstAddress;

  const [isSaving, setIsSaving] = useState(false);
  const [showMap, setShowMap] = useState(false);

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    setValue,
    formState: { errors },
  } = useForm<AddressFormValues>({
    resolver: zodResolver(isEditing ? updateAddressSchema : createAddressSchema),
    mode: "onTouched",
    defaultValues: address ? toFormValues(address) : getEmptyValues(isFirstAddress),
  });

  useEffect(() => {
    reset(address ? toFormValues(address) : getEmptyValues(isFirstAddress));
  }, [address, isFirstAddress, reset]);

  const [latitude, longitude] = useWatch({ control, name: ["latitude", "longitude"] });
  const point = useMemo(() => parseCoordinates(latitude, longitude), [latitude, longitude]);

  const fillPoint = useCallback(
    (selected: AddressCoordinates) => {
      setShowMap(true);
      setValue("latitude", String(selected.latitude), { shouldDirty: true, shouldValidate: true });
      setValue("longitude", String(selected.longitude), {
        shouldDirty: true,
        shouldValidate: true,
      });
      // Never retain address text belonging to the previously selected point.
      for (const name of ["addressLine", "ward", "district", "city"] as const) {
        setValue(name, "", { shouldDirty: true });
      }
    },
    [setValue],
  );

  const fillAddress = useCallback(
    (resolved: ResolvedAddress) => {
      for (const name of ["addressLine", "ward", "district", "city"] as const) {
        setValue(name, resolved[name], { shouldDirty: true, shouldValidate: true });
      }
    },
    [setValue],
  );

  const location = useAddressLocation(fillPoint, fillAddress);
  const isBusy = isSaving || location.phase !== "idle";

  function fillFromCurrentLocation() {
    setShowMap(true);
    location.locate();
  }

  const submit = handleSubmit(async (values) => {
    if (isBusy) return;
    setIsSaving(true);

    try {
      const commonValues = {
        label: values.label.trim() || null,
        addressLine: values.addressLine.trim(),
        ward: values.ward.trim() || null,
        district: values.district.trim() || null,
        city: values.city.trim() || null,
        latitude: Number(values.latitude),
        longitude: Number(values.longitude),
        // A locked default stays default; otherwise follow the checkbox.
        isDefault: isDefaultLocked || values.isDefault,
      };

      let saved: UserAddress;

      if (isEditing && address) {
        const input: UpdateAddressInput = commonValues;

        saved = await addressApi.updateAddress(address.id, input);
      } else {
        const input: CreateUserAddressInput = commonValues;

        saved = await addressApi.createAddress(input);
      }

      onSaved(saved);
    } catch (error) {
      setError("root.server", {
        message: getApiErrorMessage(error),
      });
    } finally {
      setIsSaving(false);
    }
  });

  const defaultHint = isDefaultLocked
    ? isEditing
      ? "This is your default address. To use a different one, set another address as default."
      : "This is your first address, so it will be set as your default address."
    : "Use this address by default when placing orders.";

  return (
    <form
      onSubmit={submit}
      noValidate
      className="rounded-2xl border border-border-default bg-bg-subtle p-5 sm:p-6"
    >
      {/* HEADER */}
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 className="text-base font-semibold tracking-tight text-text-primary">
            {isEditing ? "Edit address" : "New address"}
          </h3>

          <p className="mt-1 text-sm text-text-secondary">
            Use your current location to fill in the details, or enter them manually.
          </p>
        </div>

        <button
          type="button"
          onClick={fillFromCurrentLocation}
          disabled={isBusy}
          className={buttonClass("soft")}
        >
          {location.phase !== "idle" ? (
            <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
          ) : (
            <MapPin className="size-4" aria-hidden="true" />
          )}

          {location.phase === "locating"
            ? "Locating..."
            : location.phase === "resolving"
              ? "Finding address..."
              : "Use my location"}
        </button>
      </div>

      <div className="mt-4 space-y-3">
        {location.error && (
          <p
            role="alert"
            className="rounded-xl border border-danger/20 bg-danger/5 px-4 py-3 text-sm text-danger"
          >
            {location.error}
          </p>
        )}
        {location.phase !== "idle" && (
          <p role="status" aria-live="polite" className="text-sm text-text-secondary">
            {location.phase === "locating"
              ? "Waiting for your browser location..."
              : "Updating address for the selected point..."}
          </p>
        )}
        {location.accuracy !== null && (
          <p className="text-xs text-text-secondary">
            GPS accuracy: approximately {Math.round(location.accuracy)} m. Check the pin and adjust
            it if needed.
          </p>
        )}
        {showMap && (
          <AddressLocationMap point={point} disabled={isSaving} onSelect={location.selectPoint} />
        )}
      </div>
      <fieldset disabled={isBusy} className="mt-5 min-w-0 space-y-5 disabled:opacity-60">
        {/* ADDRESS DETAILS */}
        <FormSection title="Address details">
          <div className="grid gap-4 md:grid-cols-2">
            <FormField label="Label" error={errors.label?.message}>
              <input
                {...register("label")}
                className={inputClass}
                placeholder="Home, Work..."
                aria-invalid={Boolean(errors.label)}
              />
            </FormField>

            <FormField label="Address line" required error={errors.addressLine?.message}>
              <input
                {...register("addressLine")}
                className={inputClass}
                placeholder="House number, street name..."
                aria-invalid={Boolean(errors.addressLine)}
              />
            </FormField>

            <FormField label="Ward" error={errors.ward?.message}>
              <input
                {...register("ward")}
                className={inputClass}
                aria-invalid={Boolean(errors.ward)}
              />
            </FormField>

            <FormField label="District" error={errors.district?.message}>
              <input
                {...register("district")}
                className={inputClass}
                aria-invalid={Boolean(errors.district)}
              />
            </FormField>

            <FormField
              label="City / Province"
              className="md:col-span-2"
              error={errors.city?.message}
            >
              <input
                {...register("city")}
                className={inputClass}
                aria-invalid={Boolean(errors.city)}
              />
            </FormField>
          </div>
        </FormSection>

        {/* COORDINATES */}
        <FormSection
          title="Coordinates"
          required
          description="Filled in automatically when you use your location. Enter them manually if needed."
        >
          <div className="grid gap-4 min-[480px]:grid-cols-2">
            <FormField label="Latitude" required error={errors.latitude?.message}>
              <input
                {...register("latitude")}
                type="number"
                step="any"
                inputMode="decimal"
                placeholder="e.g. 10.762622"
                className={inputClass}
                aria-invalid={Boolean(errors.latitude)}
              />
            </FormField>

            <FormField label="Longitude" required error={errors.longitude?.message}>
              <input
                {...register("longitude")}
                type="number"
                step="any"
                inputMode="decimal"
                placeholder="e.g. 106.660172"
                className={inputClass}
                aria-invalid={Boolean(errors.longitude)}
              />
            </FormField>
          </div>
        </FormSection>

        {/* DEFAULT ADDRESS */}
        <label
          className={`flex items-start gap-3 rounded-xl border border-border-default bg-bg-surface p-4 ${
            isDefaultLocked ? "cursor-not-allowed" : "cursor-pointer"
          }`}
        >
          <input
            {...register("isDefault")}
            type="checkbox"
            disabled={isDefaultLocked}
            className="mt-0.5 size-4 shrink-0 accent-primary-600"
          />

          <span className="min-w-0">
            <span className="block text-sm font-medium text-text-primary">
              Set as default address
            </span>

            <span className="mt-0.5 block text-xs text-text-secondary">{defaultHint}</span>
          </span>
        </label>
      </fieldset>

      {/* SERVER ERROR */}
      {errors.root?.server?.message && (
        <div
          role="alert"
          className="mt-4 rounded-xl border border-danger/20 bg-danger/5 px-4 py-3 text-sm text-danger"
        >
          {errors.root.server.message}
        </div>
      )}

      {/* ACTIONS */}
      <div className="mt-5 flex flex-wrap justify-end gap-3 border-t border-border-default pt-5">
        <button
          type="button"
          onClick={onCancel}
          disabled={isSaving}
          className={buttonClass("outline")}
        >
          <X className="size-4" aria-hidden="true" />
          Cancel
        </button>

        <button type="submit" disabled={isBusy} className={buttonClass("solid")}>
          {isSaving ? (
            <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
          ) : (
            <Check className="size-4" aria-hidden="true" />
          )}

          {isSaving ? "Saving..." : isEditing ? "Save changes" : "Save address"}
        </button>
      </div>
    </form>
  );
}

interface FormSectionProps {
  title: string;
  description?: string;
  required?: boolean;
  children: ReactNode;
}

function FormSection({ title, description, required, children }: FormSectionProps) {
  return (
    <fieldset className="min-w-0 rounded-xl border border-border-default bg-bg-surface p-4">
      <legend className="px-2 text-sm font-semibold text-text-primary">
        {title}
        {required && <span className="ml-1 text-danger">*</span>}
      </legend>

      {description && <p className="mb-3 text-xs text-text-secondary">{description}</p>}

      {children}
    </fieldset>
  );
}

interface FormFieldProps {
  label: string;
  required?: boolean;
  error?: string;
  className?: string;
  children: ReactNode;
}

function FormField({ label, required, error, className = "", children }: FormFieldProps) {
  return (
    <label className={`block text-sm font-medium text-text-primary ${className}`}>
      {label}
      {required && <span className="ml-1 text-danger">*</span>}

      {children}

      {error && (
        <span role="alert" className="mt-1 block text-xs font-normal text-danger">
          {error}
        </span>
      )}
    </label>
  );
}
