import { useCallback, useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import {
  CreditCard,
  ExternalLink,
  FileText,
  Image as ImageIcon,
  Landmark,
  LoaderCircle,
  MapPin,
  Paperclip,
  Clock3,
  Save,
  Store,
  Upload,
  X,
  AlertCircle,
  CheckCircle2,
} from "lucide-react";

import { getApiErrorMessage } from "@/shared/api/httpClient";
import { shopApplicationApi } from "../api/shopApplicationApi";
import { validateShopApplication } from "../schemas/shopApplication.schema";

import type {
  ShopApplication,
  ShopApplicationForm as FormValues,
  ShopDocumentType,
  ShopDocumentUpload,
} from "../types/shopApplication.types";

interface Props {
  application?: ShopApplication | null;
  onSaved: (application: ShopApplication) => void;
  onCancel: () => void;
}

type FormFieldErrors = Partial<Record<keyof FormValues | "documents", string>>;

const EMPTY_FORM: FormValues = {
  name: "",
  description: "",
  businessLicenseNo: "",
  addressLine: "",
  ward: "",
  district: "",
  city: "",
  latitude: "",
  longitude: "",
  openingTime: "",
  closingTime: "",
  bankName: "",
  bankAccountNumber: "",
  bankAccountHolder: "",
  payosClientId: "",
  payosApiKey: "",
  payosChecksumKey: "",
};

/* -------------------------------------------------------------------------- */
/*  Shared styles                                                              */
/* -------------------------------------------------------------------------- */

const inputClass =
  "mt-1.5 w-full rounded-xl border border-border-default bg-bg-surface px-3.5 py-2.5 text-sm text-text-primary placeholder:text-text-muted outline-none transition hover:border-neutral-300 focus:border-primary-500 focus:ring-4 focus:ring-primary-100 aria-[invalid=true]:border-danger aria-[invalid=true]:focus:ring-danger/20";

const labelClass = "block text-sm font-medium text-text-primary";

const focusRing =
  "focus-within:ring-4 focus-within:ring-primary-100 focus-within:border-primary-500";

/* -------------------------------------------------------------------------- */
/*  Helpers                                                                    */
/* -------------------------------------------------------------------------- */

function toForm(application?: ShopApplication | null): FormValues {
  if (!application) {
    return { ...EMPTY_FORM };
  }

  return {
    name: application.name,
    description: application.description ?? "",
    businessLicenseNo: application.businessLicenseNo ?? "",
    addressLine: application.addressLine,
    ward: application.ward ?? "",
    district: application.district ?? "",
    city: application.city ?? "",
    latitude: String(application.latitude),
    longitude: String(application.longitude),
    openingTime: application.openingTime?.slice(0, 5) ?? "",
    closingTime: application.closingTime?.slice(0, 5) ?? "",
    bankName: application.bankName,
    bankAccountNumber: application.bankAccountNumber,
    bankAccountHolder: application.bankAccountHolder,

    // Existing PayOS credentials are intentionally not returned by the API.
    // Empty values mean "keep the current configuration" during resubmission.
    payosClientId: "",
    payosApiKey: "",
    payosChecksumKey: "",
  };
}

function documentText(type: ShopDocumentType) {
  return type === "BusinessLicense"
    ? "Business license"
    : type === "FoodSafetyCertificate"
      ? "Food safety certificate"
      : "Other document";
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function usePreviewUrl(file?: File) {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!file) {
      setUrl(null);
      return;
    }

    const objectUrl = URL.createObjectURL(file);
    setUrl(objectUrl);

    return () => URL.revokeObjectURL(objectUrl);
  }, [file]);

  return url;
}

/* -------------------------------------------------------------------------- */
/*  Main form                                                                  */
/* -------------------------------------------------------------------------- */

export function ShopApplicationForm({ application, onSaved, onCancel }: Props) {
  const [form, setForm] = useState<FormValues>(() => toForm(application));

  const [logo, setLogo] = useState<File>();
  const [coverImage, setCoverImage] = useState<File>();
  const [documents, setDocuments] = useState<ShopDocumentUpload[]>([]);

  const [saving, setSaving] = useState(false);
  const [locating, setLocating] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FormFieldErrors>({});

  const errorRef = useRef<HTMLDivElement>(null);

  const isEditing = Boolean(application);
  const hasPayos = Boolean(isEditing && application?.hasPayosConfiguration);

  useEffect(() => {
    setForm(toForm(application));
    setLogo(undefined);
    setCoverImage(undefined);
    setDocuments([]);
    setError("");
    setFieldErrors({});
  }, [application]);

  useEffect(() => {
    if (error) {
      errorRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
    }
  }, [error]);

  function setField<K extends keyof FormValues>(field: K, value: FormValues[K]) {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));

    setFieldErrors((current) => ({
      ...current,
      [field]: undefined,
    }));

    setError("");
  }

  /**
   * Validate only the field that triggered onBlur.
   * The shared validator can still validate the whole form internally,
   * but only the current field's error is displayed.
   */
  const validateField = useCallback(
    (field: keyof FormValues) => {
      const validation = validateShopApplication(form, hasPayos, documents);
      const message = validation.errors[field];

      setFieldErrors((current) => ({
        ...current,
        [field]: message,
      }));
    },
    [form, hasPayos, documents],
  );

  function useCurrentLocation() {
    if (!navigator.geolocation) {
      setError("Your browser does not support geolocation.");
      return;
    }

    setLocating(true);
    setError("");

    navigator.geolocation.getCurrentPosition(
      ({ coords }) => {
        setField("latitude", String(coords.latitude));
        setField("longitude", String(coords.longitude));
        setLocating(false);
      },
      () => {
        setError("Unable to get your current location. Please enter the coordinates manually.");
        setLocating(false);
      },
      {
        enableHighAccuracy: true,
        timeout: 10000,
      },
    );
  }

  function addDocument(type: ShopDocumentType, file?: File) {
    if (!file) return;

    setDocuments((current) => {
      const filtered = current.filter((document) => document.type !== type);

      return [
        ...filtered,
        {
          type,
          file,
        },
      ];
    });

    setFieldErrors((current) => ({
      ...current,
      documents: undefined,
    }));

    setError("");
  }

  function removeDocument(type: ShopDocumentType) {
    setDocuments((current) => current.filter((document) => document.type !== type));
    setFieldErrors((current) => ({
      ...current,
      documents: undefined,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    const validation = validateShopApplication(form, hasPayos, documents);

    setFieldErrors(validation.errors);

    if (!validation.isValid) {
      const firstError = Object.values(validation.errors).find((message): message is string =>
        Boolean(message),
      );

      setError(firstError ?? "Please check your information and try again.");
      return;
    }

    setSaving(true);

    try {
      const result = isEditing
        ? await shopApplicationApi.resubmit(application!.id, form, logo, coverImage, documents)
        : await shopApplicationApi.create(form, logo, coverImage, documents);

      onSaved(result);
    } catch (requestError) {
      setError(getApiErrorMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      className="rounded-2xl border border-border-default bg-bg-surface shadow-sm"
    >
      {/* Header */}
      <header className="flex items-start justify-between gap-4 rounded-t-2xl border-b border-border-default bg-gradient-to-r from-primary-50 to-bg-surface px-5 py-5 sm:px-8">
        <div className="flex items-start gap-4">
          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-primary-600 text-white shadow-sm">
            <Store size={22} />
          </div>

          <div>
            <h2 className="text-xl font-semibold tracking-tight text-text-primary">
              {isEditing ? "Edit shop application" : "Register your shop"}
            </h2>

            <p className="mt-1 max-w-xl text-sm leading-6 text-text-secondary">
              {isEditing
                ? "Update your information and resubmit your application for review."
                : "Fill in your shop details to apply as a SaveBite Store Owner. Fields marked with * are required."}
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={onCancel}
          className="rounded-full p-2 text-text-muted transition hover:bg-neutral-100 hover:text-text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
          aria-label="Close"
        >
          <X size={20} />
        </button>
      </header>

      <div className="px-5 py-2 sm:px-8">
        {/* Error */}
        {error && (
          <div
            ref={errorRef}
            role="alert"
            className="mt-6 flex items-start gap-3 rounded-xl border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger"
          >
            <AlertCircle size={18} className="mt-0.5 shrink-0" />
            <p>{error}</p>
          </div>
        )}

        <div className="divide-y divide-border-default">
          {/* Shop information */}
          <Section
            icon={<Store size={18} />}
            title="Shop information"
            description="The basics customers and our review team will see."
          >
            <div className="grid gap-4 md:grid-cols-2">
              <Field
                label="Shop name"
                value={form.name}
                onChange={(value) => setField("name", value)}
                onBlur={() => validateField("name")}
                placeholder="e.g. Sunrise Bakery"
                required
                error={fieldErrors.name}
              />

              <Field
                label="Business license number"
                value={form.businessLicenseNo}
                onChange={(value) => setField("businessLicenseNo", value)}
                onBlur={() => validateField("businessLicenseNo")}
                placeholder="e.g. 0123456789"
                required
                error={fieldErrors.businessLicenseNo}
              />

              <label className={`${labelClass} md:col-span-2`}>
                Description
                <textarea
                  className={`${inputClass} min-h-28 resize-y`}
                  value={form.description}
                  onChange={(event) => setField("description", event.target.value)}
                  onBlur={() => validateField("description")}
                  placeholder="Tell customers what your shop sells."
                  maxLength={2000}
                  aria-invalid={Boolean(fieldErrors.description)}
                />
                <span className="mt-1 block text-right text-xs font-normal text-text-muted">
                  {form.description.length}/2000
                </span>
                {fieldErrors.description && (
                  <span role="alert" className="mt-1 block text-xs font-normal text-danger">
                    {fieldErrors.description}
                  </span>
                )}
              </label>
            </div>
          </Section>

          {/* Branding */}
          <Section
            icon={<ImageIcon size={18} />}
            title="Logo and cover"
            description="This is how your shop will look. Click an image to change it."
          >
            <BrandingUploader
              logo={logo}
              cover={coverImage}
              existingLogoUrl={application?.logoUrl}
              existingCoverUrl={application?.coverImageUrl}
              onLogoChange={setLogo}
              onCoverChange={setCoverImage}
            />
          </Section>

          {/* Address */}
          <Section
            icon={<MapPin size={18} />}
            title="Shop address"
            description="Your location is used for distance-based discovery."
          >
            <div className="grid gap-4 md:grid-cols-2">
              <Field
                label="Address"
                value={form.addressLine}
                onChange={(value) => setField("addressLine", value)}
                onBlur={() => validateField("addressLine")}
                placeholder="House number and street"
                required
                error={fieldErrors.addressLine}
                className="md:col-span-2"
              />

              <Field
                label="Ward"
                value={form.ward}
                onChange={(value) => setField("ward", value)}
                onBlur={() => validateField("ward")}
                error={fieldErrors.ward}
              />

              <Field
                label="District"
                value={form.district}
                onChange={(value) => setField("district", value)}
                onBlur={() => validateField("district")}
                error={fieldErrors.district}
              />

              <Field
                label="City"
                value={form.city}
                onChange={(value) => setField("city", value)}
                onBlur={() => validateField("city")}
                error={fieldErrors.city}
                className="md:col-span-2"
              />
            </div>

            <div className="mt-5 rounded-xl border border-border-default bg-neutral-50 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-medium text-text-primary">Coordinates</p>
                  <p className="text-xs text-text-secondary">
                    Use your current position or enter them manually.
                  </p>
                </div>

                <button
                  type="button"
                  onClick={useCurrentLocation}
                  disabled={locating}
                  className="inline-flex items-center gap-2 rounded-full border border-primary-200 bg-bg-surface px-4 py-2 text-sm font-semibold text-primary-700 transition hover:bg-primary-50 focus:outline-none focus-visible:ring-4 focus-visible:ring-primary-100 disabled:opacity-60"
                >
                  {locating ? (
                    <LoaderCircle size={16} className="animate-spin" />
                  ) : (
                    <MapPin size={16} />
                  )}

                  {locating ? "Locating…" : "Use current location"}
                </button>
              </div>

              <div className="mt-3 grid gap-4 sm:grid-cols-2">
                <Field
                  label="Latitude"
                  type="number"
                  step="any"
                  value={form.latitude}
                  onChange={(value) => setField("latitude", value)}
                  onBlur={() => validateField("latitude")}
                  placeholder="e.g. 9.2941"
                  required
                  error={fieldErrors.latitude}
                />

                <Field
                  label="Longitude"
                  type="number"
                  step="any"
                  value={form.longitude}
                  onChange={(value) => setField("longitude", value)}
                  onBlur={() => validateField("longitude")}
                  placeholder="e.g. 105.7216"
                  required
                  error={fieldErrors.longitude}
                />
              </div>
            </div>
          </Section>

          {/* Opening hours */}
          <Section
            icon={<Clock3 size={18} />}
            title="Opening hours"
            description="When customers can pick up their orders."
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                label="Opening time"
                type="time"
                value={form.openingTime}
                onChange={(value) => setField("openingTime", value)}
                onBlur={() => validateField("openingTime")}
                required
                error={fieldErrors.openingTime}
              />

              <Field
                label="Closing time"
                type="time"
                value={form.closingTime}
                onChange={(value) => setField("closingTime", value)}
                onBlur={() => validateField("closingTime")}
                required
                error={fieldErrors.closingTime}
              />
            </div>
          </Section>

          {/* Documents */}
          <Section
            icon={<FileText size={18} />}
            title="Supporting documents"
            description="Add your business license or food safety certificate."
          >
            {application?.documents?.length ? (
              <div className="mb-5">
                <p className="mb-2 text-sm font-medium text-text-primary">Current documents</p>

                <div className="space-y-2">
                  {application.documents.map((document) => (
                    <ExistingDocument key={document.id} document={document} />
                  ))}
                </div>
              </div>
            ) : null}

            {documents.length > 0 && (
              <div className="mb-5">
                <p className="mb-2 text-sm font-medium text-text-primary">Ready to upload</p>

                <div className="space-y-2">
                  {documents.map((document) => (
                    <NewDocument
                      key={document.type}
                      document={document}
                      onRemove={() => removeDocument(document.type)}
                    />
                  ))}
                </div>
              </div>
            )}

            {fieldErrors.documents && (
              <p role="alert" className="mb-3 text-xs text-danger">
                {fieldErrors.documents}
              </p>
            )}

            <DocumentUploader onAdd={addDocument} />
          </Section>

          {/* Bank */}
          <Section
            icon={<Landmark size={18} />}
            title="Bank information"
            description="Where your payouts will be sent."
          >
            <div className="grid gap-4 md:grid-cols-2">
              <Field
                label="Bank"
                value={form.bankName}
                onChange={(value) => setField("bankName", value)}
                onBlur={() => validateField("bankName")}
                required
                error={fieldErrors.bankName}
              />

              <Field
                label="Account number"
                value={form.bankAccountNumber}
                onChange={(value) => setField("bankAccountNumber", value)}
                onBlur={() => validateField("bankAccountNumber")}
                inputMode="numeric"
                required
                error={fieldErrors.bankAccountNumber}
              />

              <Field
                label="Account holder"
                value={form.bankAccountHolder}
                onChange={(value) => setField("bankAccountHolder", value)}
                onBlur={() => validateField("bankAccountHolder")}
                className="md:col-span-2"
                required
                error={fieldErrors.bankAccountHolder}
              />
            </div>
          </Section>

          {/* PayOS */}
          <Section
            icon={<CreditCard size={18} />}
            title="PayOS"
            description="Connect PayOS to accept online payments."
          >
            {hasPayos ? (
              <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-primary-200 bg-primary-50 px-4 py-3 text-sm text-primary-800">
                <CheckCircle2 size={18} className="mt-0.5 shrink-0 text-primary-600" />

                <p>
                  PayOS is already configured. Leave these fields empty to keep the current
                  configuration.
                </p>
              </div>
            ) : null}

            <div className="grid gap-4 md:grid-cols-3">
              <Field
                label="Client ID"
                value={form.payosClientId}
                onChange={(value) => setField("payosClientId", value)}
                onBlur={() => validateField("payosClientId")}
                autoComplete="off"
                required={!hasPayos}
                error={fieldErrors.payosClientId}
              />

              <Field
                label="API key"
                type="password"
                value={form.payosApiKey}
                onChange={(value) => setField("payosApiKey", value)}
                onBlur={() => validateField("payosApiKey")}
                autoComplete="new-password"
                required={!hasPayos}
                error={fieldErrors.payosApiKey}
              />

              <Field
                label="Checksum key"
                type="password"
                value={form.payosChecksumKey}
                onChange={(value) => setField("payosChecksumKey", value)}
                onBlur={() => validateField("payosChecksumKey")}
                autoComplete="new-password"
                required={!hasPayos}
                error={fieldErrors.payosChecksumKey}
              />
            </div>
          </Section>
        </div>
      </div>

      {/* Actions */}
      <footer className="sticky bottom-0 z-20 flex flex-col-reverse gap-3 rounded-b-2xl border-t border-border-default bg-bg-surface/90 px-5 py-4 backdrop-blur sm:flex-row sm:items-center sm:justify-end sm:px-8">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-xl border border-border-default bg-bg-surface px-5 py-2.5 text-sm font-semibold text-text-secondary transition hover:bg-neutral-50 focus:outline-none focus-visible:ring-4 focus-visible:ring-neutral-200"
        >
          Cancel
        </button>

        <button
          type="submit"
          disabled={saving}
          className="inline-flex items-center justify-center gap-2 rounded-xl bg-primary-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700 focus:outline-none focus-visible:ring-4 focus-visible:ring-primary-200 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? <LoaderCircle size={17} className="animate-spin" /> : <Save size={17} />}

          {saving ? "Saving…" : isEditing ? "Save and resubmit" : "Submit application"}
        </button>
      </footer>
    </form>
  );
}

/* -------------------------------------------------------------------------- */
/*  Layout pieces                                                              */
/* -------------------------------------------------------------------------- */

function Section({
  icon,
  title,
  description,
  children,
}: {
  icon: ReactNode;
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <section className="grid gap-5 py-7 lg:grid-cols-[220px_minmax(0,1fr)] lg:gap-10">
      <div className="flex items-start gap-3 lg:block">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-primary-50 text-primary-700">
          {icon}
        </div>

        <div className="lg:mt-3">
          <h3 className="text-base font-semibold text-text-primary">{title}</h3>

          {description && (
            <p className="mt-1 text-sm leading-5 text-text-secondary">{description}</p>
          )}
        </div>
      </div>

      <div className="min-w-0">{children}</div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Field                                                                      */
/* -------------------------------------------------------------------------- */

function Field({
  label,
  value,
  onChange,
  onBlur,
  type = "text",
  required = false,
  className = "",
  placeholder,
  step,
  inputMode,
  autoComplete,
  error,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  type?: string;
  required?: boolean;
  className?: string;
  placeholder?: string;
  step?: string;
  inputMode?: "numeric" | "decimal" | "text";
  autoComplete?: string;
  error?: string;
}) {
  const hasError = Boolean(error);

  return (
    <label className={`${labelClass} ${className}`}>
      <span>
        {label}
        {required && <span className="ml-0.5 text-danger">*</span>}
      </span>

      <input
        type={type}
        value={value}
        required={required}
        placeholder={placeholder}
        step={step}
        inputMode={inputMode}
        autoComplete={autoComplete}
        onChange={(event) => onChange(event.target.value)}
        onBlur={onBlur}
        aria-invalid={hasError}
        className={inputClass}
      />

      {error && (
        <span role="alert" className="mt-1 block text-xs font-normal text-danger">
          {error}
        </span>
      )}
    </label>
  );
}

/* -------------------------------------------------------------------------- */
/*  Logo + cover                                                               */
/* -------------------------------------------------------------------------- */

function BrandingUploader({
  logo,
  cover,
  existingLogoUrl,
  existingCoverUrl,
  onLogoChange,
  onCoverChange,
}: {
  logo?: File;
  cover?: File;
  existingLogoUrl?: string | null;
  existingCoverUrl?: string | null;
  onLogoChange: (file?: File) => void;
  onCoverChange: (file?: File) => void;
}) {
  const logoPreview = usePreviewUrl(logo);
  const coverPreview = usePreviewUrl(cover);

  const logoUrl = logoPreview ?? existingLogoUrl ?? null;
  const coverUrl = coverPreview ?? existingCoverUrl ?? null;

  const accept = "image/jpeg,image/png,image/webp";

  return (
    <div>
      <div className="relative pb-14">
        {/* Cover */}
        <label
          className={`group relative block h-44 cursor-pointer overflow-hidden rounded-2xl border border-border-default bg-gradient-to-br from-primary-50 to-neutral-50 transition sm:h-52 ${focusRing}`}
        >
          {coverUrl ? (
            <img src={coverUrl} alt="Cover preview" className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full flex-col items-center justify-center text-text-muted">
              <ImageIcon size={30} strokeWidth={1.5} />
              <span className="mt-2 text-sm font-medium text-text-secondary">
                Add a cover image
              </span>
              <span className="mt-0.5 text-xs">Wide image · JPG, PNG or WebP</span>
            </div>
          )}

          <div className="absolute inset-0 flex items-center justify-center bg-black/40 opacity-0 transition group-hover:opacity-100 group-focus-within:opacity-100">
            <span className="inline-flex items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-semibold text-text-primary shadow">
              <Upload size={15} />
              {coverUrl ? "Change cover" : "Upload cover"}
            </span>
          </div>

          <input
            type="file"
            accept={accept}
            className="sr-only"
            onChange={(event) => {
              onCoverChange(event.target.files?.[0]);
              event.currentTarget.value = "";
            }}
          />
        </label>

        {cover && (
          <button
            type="button"
            onClick={() => onCoverChange(undefined)}
            className="absolute right-3 top-3 z-10 inline-flex items-center gap-1 rounded-full bg-white/95 px-3 py-1.5 text-xs font-semibold text-danger shadow transition hover:bg-white"
          >
            <X size={13} />
            Remove
          </button>
        )}

        {/* Logo */}
        <label
          className={`group absolute bottom-0 left-5 z-10 block h-24 w-24 cursor-pointer overflow-hidden rounded-3xl border-4 border-white bg-white shadow-lg shadow-black/10 ring-1 ring-border-default ${focusRing}`}
        >
          {logoUrl ? (
            <img
              src={logoUrl}
              alt="Logo preview"
              className="h-full w-full rounded-[1.25rem] object-cover"
            />
          ) : (
            <div className="flex h-full w-full flex-col items-center justify-center rounded-[1.25rem] bg-gradient-to-br from-primary-50 to-neutral-100 text-text-muted">
              <Store size={26} strokeWidth={1.6} />
            </div>
          )}

          <div className="absolute inset-0 flex items-center justify-center rounded-[1.25rem] bg-black/45 opacity-0 transition group-hover:opacity-100 group-focus-within:opacity-100">
            <Upload size={20} className="text-white" />
          </div>

          <input
            type="file"
            accept={accept}
            className="sr-only"
            onChange={(event) => {
              onLogoChange(event.target.files?.[0]);
              event.currentTarget.value = "";
            }}
          />
        </label>

        <div className="absolute bottom-1 left-[8.25rem] right-0 min-w-0">
          <p className="text-sm font-medium text-text-primary">Logo</p>

          <div className="flex items-center gap-3">
            <p className="truncate text-xs text-text-secondary">
              {logo ? logo.name : existingLogoUrl ? "Current logo" : "Square image works best"}
            </p>

            {logo && (
              <button
                type="button"
                onClick={() => onLogoChange(undefined)}
                className="shrink-0 text-xs font-medium text-danger hover:underline"
              >
                Remove
              </button>
            )}
          </div>
        </div>
      </div>

      {cover && <p className="mt-1 truncate text-xs text-text-secondary">Cover: {cover.name}</p>}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Documents                                                                  */
/* -------------------------------------------------------------------------- */

function ExistingDocument({ document }: { document: ShopApplication["documents"][number] }) {
  return (
    <a
      href={document.fileUrl}
      target="_blank"
      rel="noreferrer"
      className="group flex items-center gap-3 rounded-xl border border-border-default bg-bg-surface px-3.5 py-3 text-sm transition hover:border-primary-300 hover:bg-primary-50 focus:outline-none focus-visible:ring-4 focus-visible:ring-primary-100"
    >
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary-50 text-primary-600">
        <Paperclip size={17} />
      </div>

      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-text-primary">
          {documentText(document.documentType)}
        </p>

        <p className="truncate text-xs text-text-secondary">{document.originalFileName}</p>
      </div>

      <span className="inline-flex shrink-0 items-center gap-1 text-xs font-semibold text-primary-700">
        View
        <ExternalLink size={13} />
      </span>
    </a>
  );
}

function NewDocument({
  document,
  onRemove,
}: {
  document: ShopDocumentUpload;
  onRemove: () => void;
}) {
  const previewUrl = usePreviewUrl(document.file);
  const isImage = document.file.type.startsWith("image/");

  return (
    <div className="flex items-center gap-3 rounded-xl border border-primary-200 bg-primary-50/40 px-3.5 py-3">
      {isImage && previewUrl ? (
        <img
          src={previewUrl}
          alt={document.file.name}
          className="h-10 w-10 shrink-0 rounded-lg border border-border-default object-cover"
        />
      ) : (
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary-50 text-primary-600">
          <FileText size={18} />
        </div>
      )}

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="truncate text-sm font-medium text-text-primary">
            {documentText(document.type)}
          </p>

          <span className="shrink-0 rounded-full bg-primary-100 px-2 py-0.5 text-[11px] font-medium text-primary-800">
            New
          </span>
        </div>

        <p className="truncate text-xs text-text-secondary">
          {document.file.name} · {formatSize(document.file.size)}
        </p>
      </div>

      <button
        type="button"
        onClick={onRemove}
        aria-label={`Remove ${documentText(document.type)}`}
        className="shrink-0 rounded-full p-1.5 text-text-muted transition hover:bg-danger/10 hover:text-danger focus:outline-none focus-visible:ring-2 focus-visible:ring-danger"
      >
        <X size={16} />
      </button>
    </div>
  );
}

function DocumentUploader({ onAdd }: { onAdd: (type: ShopDocumentType, file?: File) => void }) {
  const [type, setType] = useState<ShopDocumentType>("BusinessLicense");

  return (
    <div className="rounded-xl border border-dashed border-border-default bg-neutral-50 p-4 transition hover:border-primary-400">
      <div className="flex flex-col gap-3 sm:flex-row">
        <select
          aria-label="Document type"
          value={type}
          onChange={(event) => setType(event.target.value as ShopDocumentType)}
          className="w-full rounded-xl border border-border-default bg-bg-surface px-3.5 py-2.5 text-sm text-text-primary outline-none transition focus:border-primary-500 focus:ring-4 focus:ring-primary-100 sm:w-64"
        >
          <option value="BusinessLicense">Business license</option>
          <option value="FoodSafetyCertificate">Food safety certificate</option>
          <option value="Other">Other document</option>
        </select>

        <label
          className={`inline-flex flex-1 cursor-pointer items-center justify-center gap-2 rounded-xl border border-primary-200 bg-bg-surface px-4 py-2.5 text-sm font-semibold text-primary-700 transition hover:bg-primary-50 ${focusRing}`}
        >
          <Upload size={16} />
          Choose file
          <input
            type="file"
            accept="application/pdf,image/jpeg,image/png,image/webp"
            className="sr-only"
            onChange={(event) => {
              const file = event.target.files?.[0];

              if (file) {
                onAdd(type, file);
              }

              event.currentTarget.value = "";
            }}
          />
        </label>
      </div>

      <p className="mt-3 text-xs text-text-secondary">
        PDF, JPG, PNG or WebP. Choosing the same document type again replaces the previous file.
      </p>
    </div>
  );
}
