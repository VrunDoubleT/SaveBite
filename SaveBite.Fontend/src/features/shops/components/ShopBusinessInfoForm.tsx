import { useCallback, useEffect, useState, type ChangeEvent, type ReactNode } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ImagePlus, LoaderCircle, MapPin, Save, UploadCloud, Undo2 } from "lucide-react";
import type { OwnerShop, UpdateBusinessInfo } from "@/features/shops/types/ownerShop.types";
import {
  updateBusinessInfoSchema,
  type UpdateBusinessInfoValues,
  validateShopImage,
} from "@/features/shops/schemas/ownerShop.schema";
import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";
import { useAddressLocation } from "@/features/addresses/hooks/useAddressLocation";
import type {
  AddressCoordinates,
  ResolvedAddress,
} from "@/features/addresses/utils/addressLocation";

const inputClass =
  "mt-1.5 w-full rounded-xl border border-border-default bg-bg-surface px-3.5 py-3 text-sm text-text-primary shadow-sm outline-none transition placeholder:text-text-muted hover:border-primary-300 focus:border-primary-500 focus:ring-4 focus:ring-primary-100 aria-[invalid=true]:border-danger aria-[invalid=true]:ring-4 aria-[invalid=true]:ring-danger/10";
const labelClass = "block text-sm font-medium text-text-primary";

function toFormValues(shop: OwnerShop): UpdateBusinessInfoValues {
  return {
    name: shop.name,
    description: shop.description ?? "",
    addressLine: shop.addressLine ?? "",
    ward: shop.ward ?? "",
    district: shop.district ?? "",
    city: shop.city ?? "",
    latitude: shop.latitude ?? 0,
    longitude: shop.longitude ?? 0,
    openingTime: shop.openingTime?.slice(0, 5) ?? null,
    closingTime: shop.closingTime?.slice(0, 5) ?? null,
  };
}

function FormField({
  label,
  required,
  error,
  className = "",
  children,
}: {
  label: string;
  required?: boolean;
  error?: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <label className={`${labelClass} ${className}`}>
      {label} {required && <span className="text-danger">*</span>}
      {children}
      {error && (
        <span role="alert" className="mt-1.5 block text-xs font-normal text-danger">
          {error}
        </span>
      )}
    </label>
  );
}

/** Settings-style row: title and help text on the left, fields on the right. */
function FormSection({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <section className="grid gap-5 border-t border-border-default pt-8 first:border-t-0 first:pt-0 lg:grid-cols-[15rem_1fr] lg:gap-12">
      <div className="lg:sticky lg:top-6 lg:self-start">
        <h3 className="text-base font-semibold text-text-primary">{title}</h3>
        <p className="mt-1.5 text-sm leading-6 text-text-secondary">{description}</p>
      </div>
      <div className="min-w-0 space-y-4">{children}</div>
    </section>
  );
}

/** Blue-framed card that groups related fields so each block stands out. */
function HighlightCard({ className = "", children }: { className?: string; children: ReactNode }) {
  return (
    <div
      className={`rounded-2xl bg-gradient-to-br from-primary-50 to-bg-subtle p-4 ring-1 ring-inset ring-primary-100 sm:p-5 ${className}`}
    >
      {children}
    </div>
  );
}

/** Label, hint / error line and Undo button for one image. */
function ImageCaption({
  label,
  required,
  file,
  error,
  onUndo,
  className = "",
}: {
  label: string;
  required?: boolean;
  file: File | null;
  error?: string;
  onUndo: () => void;
  className?: string;
}) {
  return (
    <div className={`min-w-0 ${className}`}>
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-text-primary">
          {label} {required && <span className="text-danger">*</span>}
        </p>
        {file && (
          <button
            type="button"
            onClick={onUndo}
            aria-label={`Remove selected ${label.toLowerCase()}`}
            className="inline-flex items-center gap-1 rounded-full bg-bg-surface px-2.5 py-0.5 text-xs font-medium text-text-secondary shadow-sm transition hover:text-danger focus-visible:outline-2 focus-visible:outline-primary-500"
          >
            <Undo2 size={13} aria-hidden /> Undo
          </button>
        )}
      </div>
      {error ? (
        <p role="alert" className="mt-1 text-xs text-danger">
          {error}
        </p>
      ) : (
        <p className="mt-1 truncate text-xs text-text-secondary">
          {file ? `New file: ${file.name}` : "JPG, PNG or WEBP, up to 5 MB"}
        </p>
      )}
    </div>
  );
}

/** Clickable upload frame with live preview. `logo` is a square tile, `cover` is a wide banner. */
function ImageDrop({
  variant,
  label,
  currentUrl,
  file,
  onFileChange,
  onError,
  hasError,
  className = "",
}: {
  variant: "logo" | "cover";
  label: string;
  currentUrl?: string | null;
  file: File | null;
  onFileChange: (file: File | null) => void;
  onError: (message?: string) => void;
  hasError?: boolean;
  className?: string;
}) {
  const [preview, setPreview] = useState<string | null>(null);
  useEffect(() => {
    if (!file) {
      setPreview(null);
      return;
    }
    const url = URL.createObjectURL(file);
    setPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const next = event.target.files?.[0] ?? null;
    event.target.value = "";
    if (!next) return;
    const validationError = validateShopImage(next);
    onError(validationError);
    if (validationError) {
      onFileChange(null);
      return;
    }
    onFileChange(next);
  }

  const hasImage = Boolean(preview || currentUrl);
  const isLogo = variant === "logo";

  const frame = isLogo
    ? `size-28 shrink-0 rounded-3xl border-4 bg-bg-subtle shadow-lg sm:size-32 ${
        hasError ? "border-danger/60" : "border-bg-surface"
      }`
    : `h-40 w-full rounded-2xl border-2 bg-bg-surface sm:h-52 ${
        hasImage ? "border-solid" : "border-dashed"
      } ${hasError ? "border-danger/60" : "border-primary-200"}`;

  return (
    <label
      className={`group relative flex cursor-pointer items-center justify-center overflow-hidden transition hover:border-primary-400 focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-primary-500 ${frame} ${className}`}
    >
      {hasImage ? (
        <>
          <img
            src={preview ?? currentUrl ?? ""}
            alt={`${label} preview`}
            className="h-full w-full object-cover"
          />
          {isLogo ? (
            <span className="absolute bottom-1.5 right-1.5 flex size-8 items-center justify-center rounded-full bg-black/60 text-white backdrop-blur transition group-hover:bg-black/80">
              <ImagePlus size={15} aria-hidden />
              <span className="sr-only">Replace logo</span>
            </span>
          ) : (
            <span className="absolute bottom-3 right-3 inline-flex items-center gap-1.5 rounded-full bg-black/60 px-3.5 py-1.5 text-xs font-semibold text-white backdrop-blur transition group-hover:bg-black/80">
              <ImagePlus size={14} aria-hidden /> Replace
            </span>
          )}
        </>
      ) : (
        <span className="flex flex-col items-center gap-2 px-3 text-center">
          <span className="flex size-11 items-center justify-center rounded-full bg-primary-50 text-primary-600 shadow-sm transition group-hover:scale-105">
            <UploadCloud size={22} aria-hidden />
          </span>
          <span className="text-xs font-medium text-text-secondary">
            {isLogo ? "Upload logo" : "Click to upload a cover image"}
          </span>
        </span>
      )}
      <input
        className="sr-only"
        type="file"
        accept="image/jpeg,image/png,image/webp"
        onChange={handleChange}
      />
    </label>
  );
}

export function ShopBusinessInfoForm({
  shop,
  isSaving,
  onCancel,
  onSubmit,
}: {
  shop: OwnerShop;
  isSaving: boolean;
  onCancel: () => void;
  onSubmit: (
    input: UpdateBusinessInfo,
    logo: File | null,
    coverImage: File | null,
  ) => Promise<void>;
}) {
  const [logo, setLogo] = useState<File | null>(null);
  const [coverImage, setCoverImage] = useState<File | null>(null);
  const [imageErrors, setImageErrors] = useState<{
    logo?: string;
    coverImage?: string;
  }>({});
  const {
    register,
    handleSubmit,
    reset,
    setValue,
    setError,
    formState: { errors, isDirty },
  } = useForm<UpdateBusinessInfoValues>({
    resolver: zodResolver(updateBusinessInfoSchema),
    mode: "onTouched",
    defaultValues: toFormValues(shop),
  });

  useEffect(() => {
    reset(toFormValues(shop));
  }, [shop, reset]);

  const fillShopPoint = useCallback(
    (point: AddressCoordinates) => {
      setValue("latitude", Number(point.latitude.toFixed(7)), {
        shouldDirty: true,
        shouldValidate: true,
      });
      setValue("longitude", Number(point.longitude.toFixed(7)), {
        shouldDirty: true,
        shouldValidate: true,
      });
      // A new point invalidates the previously resolved address until reverse geocoding completes.
      for (const field of ["addressLine", "ward", "district", "city"] as const) {
        setValue(field, "", { shouldDirty: true, shouldValidate: true });
      }
    },
    [setValue],
  );

  const fillShopAddress = useCallback(
    (address: ResolvedAddress) => {
      setValue("addressLine", address.addressLine, {
        shouldDirty: true,
        shouldValidate: true,
      });
      setValue("ward", address.ward, {
        shouldDirty: true,
        shouldValidate: true,
      });
      setValue("district", address.district, {
        shouldDirty: true,
        shouldValidate: true,
      });
      setValue("city", address.city, {
        shouldDirty: true,
        shouldValidate: true,
      });
    },
    [setValue],
  );

  const location = useAddressLocation(fillShopPoint, fillShopAddress);
  const locating = location.phase !== "idle";
  const hasChanges = isDirty || Boolean(logo) || Boolean(coverImage);

  function handleUseCurrentLocation() {
    location.locate();
  }

  const submit = handleSubmit(async (values) => {
    const nextErrors = {
      logo: validateShopImage(logo),
      coverImage: validateShopImage(coverImage),
    };
    if (!shop.logoUrl && !logo) nextErrors.logo = "Shop logo is required.";
    setImageErrors(nextErrors);
    if (nextErrors.logo || nextErrors.coverImage) return;
    try {
      await onSubmit(
        {
          ...values,
          name: values.name.trim(),
          description: values.description.trim(),
          addressLine: values.addressLine.trim(),
          ward: values.ward.trim(),
          district: values.district.trim(),
          city: values.city.trim(),
          openingTime: values.openingTime ? `${values.openingTime.slice(0, 5)}:00` : null,
          closingTime: values.closingTime ? `${values.closingTime.slice(0, 5)}:00` : null,
        },
        logo,
        coverImage,
      );
    } catch (error) {
      const apiErrors = getApiValidationErrors(error);
      for (const field of [
        "name",
        "description",
        "addressLine",
        "ward",
        "district",
        "city",
        "latitude",
        "longitude",
        "openingTime",
        "closingTime",
      ] as const) {
        if (apiErrors[field]) setError(field, { message: apiErrors[field] });
      }
      setError("root.server", {
        message: getApiErrorMessage(error) || "Unable to save shop information.",
      });
    }
  });

  return (
    // No overflow-hidden here: it would stop the sticky action bar from sticking to the viewport.
    <form
      onSubmit={submit}
      noValidate
      className="rounded-[2rem] border border-border-default bg-bg-surface shadow-sm"
    >
      <div className="space-y-9 p-5 sm:p-8">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-text-primary">
            Edit business information
          </h2>
          <p className="mt-1.5 text-sm text-text-secondary">
            Update your business details, logo and cover image in one place.
          </p>
        </div>

        {errors.root?.server?.message && (
          <p
            role="alert"
            className="rounded-xl border border-danger/30 bg-danger/5 p-3.5 text-sm text-danger"
          >
            {errors.root.server.message}
          </p>
        )}

        <FormSection
          title="Shop images"
          description="A logo is required. Keep your current logo or upload a new one."
        >
          <HighlightCard>
            <ImageCaption
              label="Cover image"
              file={coverImage}
              error={imageErrors.coverImage}
              onUndo={() => setCoverImage(null)}
            />
            <ImageDrop
              variant="cover"
              label="Cover image"
              currentUrl={shop.coverImageUrl}
              file={coverImage}
              onFileChange={setCoverImage}
              onError={(message) =>
                setImageErrors((current) => ({
                  ...current,
                  coverImage: message,
                }))
              }
              hasError={!!imageErrors.coverImage}
              className="mt-2.5"
            />

            {/* Logo overlaps the cover, mirroring the real shop profile */}
            <div className="relative z-10 -mt-12 flex items-end gap-4 px-3 sm:-mt-14 sm:px-5">
              <ImageDrop
                variant="logo"
                label="Shop logo"
                currentUrl={shop.logoUrl}
                file={logo}
                onFileChange={setLogo}
                onError={(message) => setImageErrors((current) => ({ ...current, logo: message }))}
                hasError={!!imageErrors.logo}
              />
              <ImageCaption
                label="Shop logo"
                required={!shop.logoUrl}
                file={logo}
                error={imageErrors.logo}
                onUndo={() => setLogo(null)}
                className="flex-1 pb-2"
              />
            </div>
          </HighlightCard>
        </FormSection>

        <FormSection
          title="Basic details"
          description="Your shop name and a short description customers will read."
        >
          <HighlightCard className="space-y-4">
            <FormField label="Shop name" required error={errors.name?.message}>
              <input
                {...register("name")}
                maxLength={200}
                aria-invalid={!!errors.name}
                className={inputClass}
              />
            </FormField>
            <FormField label="Description" error={errors.description?.message}>
              <textarea
                {...register("description")}
                maxLength={2000}
                rows={4}
                aria-invalid={!!errors.description}
                className={`${inputClass} resize-y`}
              />
            </FormField>
          </HighlightCard>
        </FormSection>

        <FormSection
          title="Location"
          description="Pin your shop with your device location, or enter the details yourself."
        >
          <HighlightCard>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex min-w-0 items-start gap-3">
                <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-bg-surface text-primary-700 shadow-sm">
                  <MapPin size={18} aria-hidden />
                </span>
                <div className="min-w-0">
                  <p className="text-sm font-semibold text-text-primary">Use current location</p>
                  <p className="mt-0.5 text-xs leading-5 text-text-secondary">
                    Fills in the coordinates and replaces the address below.
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={handleUseCurrentLocation}
                disabled={locating || isSaving}
                className="inline-flex items-center gap-2 rounded-full bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {locating ? (
                  <LoaderCircle size={16} className="animate-spin" aria-hidden />
                ) : (
                  <MapPin size={16} aria-hidden />
                )}
                {location.phase === "locating"
                  ? "Getting location…"
                  : location.phase === "resolving"
                    ? "Finding address…"
                    : "Use current location"}
              </button>
            </div>
            {location.error && (
              <p role="alert" className="mt-3 text-xs text-danger">
                {location.error}
              </p>
            )}
            {location.accuracy !== null && (
              <p className="mt-3 text-xs text-text-secondary">
                GPS accuracy: about {Math.round(location.accuracy)} m.
              </p>
            )}
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <FormField label="Latitude" required error={errors.latitude?.message}>
                <input
                  type="number"
                  step="any"
                  {...register("latitude", { valueAsNumber: true })}
                  aria-invalid={!!errors.latitude}
                  className={`${inputClass} tabular-nums`}
                />
              </FormField>
              <FormField label="Longitude" required error={errors.longitude?.message}>
                <input
                  type="number"
                  step="any"
                  {...register("longitude", { valueAsNumber: true })}
                  aria-invalid={!!errors.longitude}
                  className={`${inputClass} tabular-nums`}
                />
              </FormField>
            </div>
          </HighlightCard>

          <HighlightCard className="grid gap-4 sm:grid-cols-2">
            <FormField
              label="Street address"
              required
              error={errors.addressLine?.message}
              className="sm:col-span-2"
            >
              <input
                {...register("addressLine")}
                maxLength={500}
                aria-invalid={!!errors.addressLine}
                className={inputClass}
              />
            </FormField>
            <FormField label="Ward / Commune" error={errors.ward?.message}>
              <input
                {...register("ward")}
                maxLength={100}
                aria-invalid={!!errors.ward}
                className={inputClass}
              />
            </FormField>
            <FormField label="District" error={errors.district?.message}>
              <input
                {...register("district")}
                maxLength={100}
                aria-invalid={!!errors.district}
                className={inputClass}
              />
            </FormField>
            <FormField label="City / Province" error={errors.city?.message}>
              <input
                {...register("city")}
                maxLength={100}
                aria-invalid={!!errors.city}
                className={inputClass}
              />
            </FormField>
          </HighlightCard>
        </FormSection>

        <FormSection title="Opening hours" description="Leave both empty if your hours vary.">
          <HighlightCard className="grid gap-4 sm:max-w-md sm:grid-cols-2">
            <FormField label="Opening time" error={errors.openingTime?.message}>
              <input
                type="time"
                {...register("openingTime")}
                aria-invalid={!!errors.openingTime}
                className={`${inputClass} tabular-nums`}
              />
            </FormField>
            <FormField label="Closing time" error={errors.closingTime?.message}>
              <input
                type="time"
                {...register("closingTime")}
                aria-invalid={!!errors.closingTime}
                className={`${inputClass} tabular-nums`}
              />
            </FormField>
          </HighlightCard>
        </FormSection>
      </div>

      {/* Sticky action bar */}
      <div className="sticky bottom-0 z-10 flex flex-col gap-3 rounded-b-[2rem] border-t border-border-default bg-bg-surface/90 px-5 py-4 backdrop-blur-md sm:flex-row sm:items-center sm:justify-between sm:px-8">
        <p
          role="status"
          className={`flex items-center gap-2 text-sm ${
            hasChanges ? "text-text-primary" : "text-text-muted"
          }`}
        >
          <span
            aria-hidden
            className={`size-2 rounded-full ${hasChanges ? "bg-amber-500" : "bg-border-default"}`}
          />
          {hasChanges ? "You have unsaved changes" : "No changes yet"}
        </p>
        <div className="flex flex-col-reverse gap-3 sm:flex-row">
          <button
            type="button"
            onClick={onCancel}
            disabled={isSaving}
            className="inline-flex items-center justify-center rounded-full border border-border-default px-6 py-2.5 text-sm font-semibold text-text-secondary transition hover:bg-bg-subtle focus-visible:outline-2 focus-visible:outline-primary-500 disabled:opacity-60"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSaving}
            className="inline-flex items-center justify-center gap-2 rounded-full bg-primary-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500 disabled:opacity-60"
          >
            {isSaving ? (
              <LoaderCircle className="animate-spin" size={17} aria-hidden />
            ) : (
              <Save size={17} aria-hidden />
            )}
            Save changes
          </button>
        </div>
      </div>
    </form>
  );
}
