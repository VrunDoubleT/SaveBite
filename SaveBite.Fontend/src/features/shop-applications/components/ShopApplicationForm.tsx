import { SecretInput } from "@/shared/components/ui/SecretInput";
import { toast } from "@/shared/stores/toastStore";
import { UploadTaskGuard } from "@/shared/uploads/UploadTaskGuard";
import {
  CustomerActionFileInput,
  CustomerMutationForm,
  CustomerActionNativeButton,
} from "@/shared/components/CustomerMutationControls";
import { OpeningHoursPicker } from "@/shared/components/ui/OpeningHoursPicker";
import { useCallback, useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import {
  CreditCard,
  ExternalLink,
  FileText,
  Image as ImageIcon,
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
import { validateShopApplication, validateLogoFile } from "../schemas/shopApplication.schema";
import { AddressLocationMap } from "@/features/addresses/components/AddressLocationMap";
import { useAddressLocation } from "@/features/addresses/hooks/useAddressLocation";
import { useLocation } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { useAuthStore } from "@/shared/stores/authStore";
import {
  getUploadDraft,
  releaseUploadDraft,
  removeUploadDraft,
} from "@/shared/uploads/uploadDraftStore";
import { startOrResumeUploadDraft, uploadReferences } from "@/shared/uploads/uploadDrafts";
import type { UploadReference } from "@/shared/uploads/uploadDrafts";
import type {
  AddressCoordinates,
  ResolvedAddress,
} from "@/features/addresses/utils/addressLocation";

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
  onQueued: () => void;
}

type FormFieldErrors = Partial<Record<keyof FormValues | "documents" | "logo", string>>;

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
  payosClientId: "",
  payosApiKey: "",
  payosChecksumKey: "",
};

/*  Shared styles */
const inputClass =
  "mt-1.5 w-full rounded-md border border-border-default bg-bg-surface px-3.5 py-2.5 text-sm text-text-primary placeholder:text-text-muted outline-none transition hover:border-neutral-300 focus:border-primary-500 focus:ring-4 focus:ring-primary-100 aria-[invalid=true]:border-danger aria-[invalid=true]:focus:ring-danger/20";

const labelClass = "block text-sm font-medium text-text-primary";

const focusRing =
  "focus-within:ring-4 focus-within:ring-primary-100 focus-within:border-primary-500";

/*  Helpers */
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

/*  Main form */
function ShopApplicationFormContent({ application, onSaved, onCancel, onQueued }: Props) {
  const routeLocation = useLocation();
  const userId = useAuthStore((state) => state.user?.id);
  const [form, setForm] = useState<FormValues>(() => toForm(application));

  const [logo, setLogo] = useState<File>();
  const [coverImage, setCoverImage] = useState<File>();
  const [documents, setDocuments] = useState<ShopDocumentUpload[]>([]);

  const [saving, setSaving] = useState(false);
  const [verifyingPayOs, setVerifyingPayOs] = useState(false);
  const [verifiedPayOsInput, setVerifiedPayOsInput] = useState<string | null>(null);
  const [payOsVerificationError, setPayOsVerificationError] = useState("");
  const verificationInFlight = useRef(false);
  const payOsInput = JSON.stringify([application?.id, form.payosClientId.trim(), form.payosApiKey.trim(), form.payosChecksumKey.trim()]);
  const payOsVerified = verifiedPayOsInput === payOsInput;

  async function verifyPayOs() {
    if (verificationInFlight.current) return;
    verificationInFlight.current = true;
    setVerifyingPayOs(true);
    setVerifiedPayOsInput(null);
    setPayOsVerificationError("");
    const checkedInput = payOsInput;
    try {
      await shopApplicationApi.verifyPayOs({
        applicationId: application?.id,
        clientId: form.payosClientId.trim() || undefined,
        apiKey: form.payosApiKey.trim() || undefined,
        checksumKey: form.payosChecksumKey.trim() || undefined,
      });
      setVerifiedPayOsInput(checkedInput);
    } catch (error) {
      setPayOsVerificationError(getApiErrorMessage(error) || "We could not check your PayOS keys right now. Please try again shortly.");
    } finally {
      verificationInFlight.current = false;
      setVerifyingPayOs(false);
    }
  }
  const [submitting, setSubmitting] = useState(false);
  const [uploadNotice, setUploadNotice] = useState("");
  const uploadController = useRef<AbortController | null>(null);
  const submittingRef = useRef(false);

  const mounted = useRef(true);
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  function cancelOrEdit() {
    if (submittingRef.current) return;
    if (uploadController.current) {
      uploadController.current.abort();
      return;
    }
    onCancel();
  }
  const [activeDraftId, setActiveDraftId] = useState<string>();

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
    const draftId = (routeLocation.state as { uploadDraftId?: string } | null)?.uploadDraftId;
    if (!draftId || !userId) return;
    let active = true;
    void getUploadDraft(draftId).then((draft) => {
      if (!active || !draft || draft.userId !== userId) return;
      const saved = draft.formData as {
        applicationId?: string;
        form?: FormValues;
        logo?: File;
        coverImage?: File;
        documents?: ShopDocumentUpload[];
      };
      if ((saved.applicationId ?? undefined) !== (application?.id ?? undefined)) return;
      setActiveDraftId(draft.id);
      if (saved.form) setForm(saved.form);
      setLogo(saved.logo);
      setCoverImage(saved.coverImage);
      setDocuments(saved.documents ?? []);
    });
    return () => {
      active = false;
    };
  }, [application?.id, routeLocation.state, userId]);

  useEffect(() => {
    if (error) {
      errorRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
    }
  }, [error]);

  const setField = useCallback(<K extends keyof FormValues>(field: K, value: FormValues[K]) => {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));

    setFieldErrors((current) => ({
      ...current,
      [field]: undefined,
    }));

    setError("");
  }, []);

  /**
   * Validate only the field that triggered onBlur.
   * The shared validator can still validate the whole form internally,
   * but only the current field's error is displayed.
   */
  const validateField = useCallback(
    (field: keyof FormValues) => {
      const validation = validateShopApplication(
        form,
        hasPayos,
        documents,
        application?.documents
          .filter((document) => document.isCurrent)
          .map((document) => document.documentType) ?? [],
        { file: logo, hasExisting: Boolean(application?.logoUrl) },
      );
      const message = validation.errors[field];

      setFieldErrors((current) => ({
        ...current,
        [field]: message,
      }));
    },
    [form, hasPayos, documents, logo, application],
  );

  const onLocationPoint = useCallback(
    (point: AddressCoordinates) => {
      setField("latitude", String(point.latitude));
      setField("longitude", String(point.longitude));
      setField("addressLine", "");
      setField("ward", "");
      setField("district", "");
      setField("city", "");
    },
    [setField],
  );

  const onLocationAddress = useCallback(
    (address: ResolvedAddress) => {
      setField("addressLine", address.addressLine);
      setField("ward", address.ward);
      setField("district", address.district);
      setField("city", address.city);
    },
    [setField],
  );

  const location = useAddressLocation(onLocationPoint, onLocationAddress);
  const locating = location.phase !== "idle";

  function useCurrentLocation() {
    location.locate();
  }

  function handleLogoChange(file?: File) {
    if (!file) {
      setLogo(undefined);
      setFieldErrors((current) => ({ ...current, logo: undefined }));
      return;
    }

    const message = validateLogoFile(file);

    if (message) {
      setFieldErrors((current) => ({ ...current, logo: message }));
      return;
    }

    setLogo(file);
    setFieldErrors((current) => ({ ...current, logo: undefined }));
    setError("");
  }

  function addDocuments(type: ShopDocumentType, files: File[]) {
    if (!files || files.length === 0) return;

    setDocuments((current) => {
      const remainingSlots = Math.max(0, 10 - current.length);
      const filesToAdd = files.slice(0, remainingSlots);
      setFieldErrors((prev) => ({
        ...prev,
        documents:
          files.length > remainingSlots
            ? `Only ${remainingSlots} more document(s) can be added (max 10 total).`
            : undefined,
      }));
      return [...current, ...filesToAdd.map((file) => ({ type, file }))];
    });

    setError("");
  }

  function removeDocument(indexToRemove: number) {
    setDocuments((current) => current.filter((_, index) => index !== indexToRemove));
    setFieldErrors((current) => ({
      ...current,
      documents: undefined,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (saving || uploadController.current) return;
    if (!payOsVerified || verifyingPayOs) {
      setError("Verify your PayOS credentials successfully before submitting the application.");
      return;
    }
    setUploadNotice("");
    setError("");

    const validation = validateShopApplication(
      form,
      hasPayos,
      documents,
      application?.documents
        .filter((document) => document.isCurrent)
        .map((document) => document.documentType) ?? [],
      { file: logo, hasExisting: Boolean(application?.logoUrl) },
    );

    setFieldErrors(validation.errors);

    if (!validation.isValid) {
      const firstError = Object.values(validation.errors).find((message): message is string =>
        Boolean(message),
      );

      setError(firstError ?? "Please check your information and try again.");
      return;
    }

    setSaving(true);
    const controller = new AbortController();
    uploadController.current = controller;
    let draftId = activeDraftId;
    let queued = false;

    try {
      const uploads = [
        ...(logo ? [{ field: "logo", file: logo }] : []),
        ...(coverImage ? [{ field: "coverImage", file: coverImage }] : []),
        ...documents.map(({ file }) => ({ field: "documents", file })),
      ];
      let references: UploadReference[] = [];
      if (uploads.length) {
        if (!userId) throw new Error("Sign in before uploading application files.");
        const path = application
          ? APP_PATHS.ACCOUNT_SHOP_APPLICATION_EDIT.replace(":applicationId", application.id)
          : APP_PATHS.ACCOUNT_SHOP_APPLICATION_NEW;
        draftId ??= crypto.randomUUID();
        setActiveDraftId(draftId);
        const draft = await startOrResumeUploadDraft(
          {
            draftId,
            signal: controller.signal,
            keepActiveUntilSubmitted: true,
            onQueued: () => {
              queued = true;
              onQueued();
            },
            userId,
            purpose: "shop-application",
            targetId: application?.id,
            title: isEditing ? "Resubmit shop application" : "Register shop",
            path,
            formData: { applicationId: application?.id, form, logo, coverImage, documents },
            files: uploads,
          },
          draftId,
        );
        setActiveDraftId(draft.id);
        draftId = draft.id;
        references = uploadReferences(draft.files);
      }
      controller.signal.throwIfAborted();
      submittingRef.current = true;
      setSubmitting(true);
      const result = isEditing
        ? await shopApplicationApi.resubmit(
            application!.id,
            form,
            logo,
            coverImage,
            documents,
            references,
          )
        : await shopApplicationApi.create(form, logo, coverImage, documents, references);

      if (draftId) await removeUploadDraft(draftId);
      setActiveDraftId(undefined);
      window.dispatchEvent(new Event("shop-application:updated"));
      if (queued) toast.success("Shop application submitted successfully.");
      else if (mounted.current) onSaved(result);
    } catch (requestError) {
      if (
        controller.signal.aborted ||
        (requestError instanceof DOMException && requestError.name === "AbortError")
      ) {
        if (queued) toast.info("Upload stopped. Your application draft is saved.");
        setUploadNotice(
          "Upload stopped. Your information and selected files are kept. Edit them and submit again when ready.",
        );
      } else {
        if (queued) toast.error(getApiErrorMessage(requestError));
        else setError(getApiErrorMessage(requestError));
      }
    } finally {
      if (draftId) releaseUploadDraft(draftId);
      uploadController.current = null;
      submittingRef.current = false;
      setSubmitting(false);
      setSaving(false);
    }
  }

  return (
    <CustomerMutationForm
      onSubmit={handleSubmit}
      noValidate
      className="rounded-lg border border-border-default bg-bg-surface shadow-sm"
    >
      {/* Header */}
      <header className="flex items-start justify-between gap-4 rounded-t-lg border-b border-border-default bg-gradient-to-r from-primary-50 to-bg-surface p-4 sm:p-5">
        <div className="flex items-start gap-4">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary-600 text-white shadow-sm">
            <Store size={22} />
          </div>

          <div>
            <h2 className="text-section font-semibold tracking-tight text-text-primary">
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
          onClick={cancelOrEdit}
          disabled={submitting}
          className="rounded-full p-2 text-text-muted transition hover:bg-neutral-100 hover:text-text-primary focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
          aria-label="Close"
        >
          <X size={20} />
        </button>
      </header>

      {uploadNotice && (
        <p
          role="status"
          className="mx-4 mt-4 rounded-md bg-primary-50 p-3 text-sm text-primary-800 sm:mx-5"
        >
          {uploadNotice}
        </p>
      )}
      <fieldset disabled={saving} className="min-w-0 border-0 p-0">
        <div className="px-4 py-2 sm:px-5">
          {/* Error */}
          {error && (
            <div
              ref={errorRef}
              role="alert"
              className="mt-6 flex items-start gap-3 rounded-md border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger"
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
                onLogoChange={handleLogoChange}
                onCoverChange={setCoverImage}
                logoError={fieldErrors.logo}
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

              <div className="mt-5 rounded-md border border-border-default bg-neutral-50 p-4">
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
                {location.error && (
                  <p role="alert" className="mt-3 text-xs text-danger">
                    {location.error}
                  </p>
                )}
                <div className="mt-4">
                  <AddressLocationMap
                    point={
                      form.latitude && form.longitude
                        ? { latitude: Number(form.latitude), longitude: Number(form.longitude) }
                        : null
                    }
                    disabled={saving}
                    onSelect={location.selectPoint}
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
              <OpeningHoursPicker
                openingTime={form.openingTime}
                closingTime={form.closingTime}
                onChange={({ openingTime, closingTime }) => {
                  setField("openingTime", openingTime);
                  setField("closingTime", closingTime);
                }}
                onBlur={validateField}
                openingError={fieldErrors.openingTime}
                closingError={fieldErrors.closingTime}
                disabled={saving}
              />
            </Section>

            {/* Documents */}
            <Section
              icon={<FileText size={18} />}
              title="Supporting documents"
              description="Add your business license or food safety certificate."
            >
              {application?.documents?.some(
                (document) =>
                  document.isCurrent &&
                  !documents.some((upload) => upload.type === document.documentType),
              ) ? (
                <div className="mb-5">
                  <p className="mb-2 text-sm font-medium text-text-primary">Current documents</p>

                  <div className="space-y-2">
                    {application.documents
                      .filter(
                        (document) =>
                          document.isCurrent &&
                          !documents.some((upload) => upload.type === document.documentType),
                      )
                      .map((document) => (
                        <ExistingDocument key={document.id} document={document} />
                      ))}
                  </div>
                </div>
              ) : null}

              {documents.length > 0 && (
                <div className="mb-5">
                  <p className="mb-2 text-sm font-medium text-text-primary">
                    Ready to upload ({documents.length}/10)
                  </p>

                  <div className="space-y-2">
                    {documents.map((document, index) => (
                      <NewDocument
                        key={`${document.type}-${document.file.name}-${index}`}
                        document={document}
                        onRemove={() => removeDocument(index)}
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

              <DocumentUploader onAdd={addDocuments} />
            </Section>

            {/* PayOS */}
            <Section
              icon={<CreditCard size={18} />}
              title="PayOS"
              description="Connect PayOS to accept online payments."
            >
              {hasPayos ? (
                <div className="mb-4 flex items-start gap-2.5 rounded-md border border-primary-200 bg-primary-50 px-4 py-3 text-sm text-primary-800">
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
                  label="API Key"
                  type="password"
                  value={form.payosApiKey}
                  onChange={(value) => setField("payosApiKey", value)}
                  onBlur={() => validateField("payosApiKey")}
                  autoComplete="new-password"
                  required={!hasPayos}
                  error={fieldErrors.payosApiKey}
                />

                <Field
                  label="Checksum Key"
                  type="password"
                  value={form.payosChecksumKey}
                  onChange={(value) => setField("payosChecksumKey", value)}
                  onBlur={() => validateField("payosChecksumKey")}
                  autoComplete="new-password"
                  required={!hasPayos}
                  error={fieldErrors.payosChecksumKey}
                />
              </div>
              <div className="mt-4 space-y-3">
                <p className="text-sm text-text-secondary">
                  Verify your payment credentials before submitting. Verification creates and cancels a temporary payment link; no payment is made.
                  Changing a key requires verification again.
                </p>
                <CustomerActionNativeButton type="button" onClick={() => void verifyPayOs()}
                  disabled={verifyingPayOs || saving || payOsVerified}
                  className="inline-flex min-h-11 items-center gap-2 rounded-md bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-60">
                  {verifyingPayOs && <LoaderCircle size={17} className="animate-spin" />}
                  {verifyingPayOs ? "Verifying PayOS…" : payOsVerified ? "PayOS verified" : "Verify PayOS credentials"}
                </CustomerActionNativeButton>
                <p role="status" className="text-sm text-text-secondary">
                  {payOsVerified ? "Verification successful. You can submit your application." : "Verify PayOS to enable submission."}
                </p>
                {payOsVerificationError && <p role="alert" className="text-sm text-red-700">{payOsVerificationError}</p>}
              </div>
            </Section>
          </div>
        </div>
      </fieldset>
      {/* Actions */}
      <footer className="sticky bottom-0 z-20 flex flex-col-reverse gap-3 rounded-b-lg border-t border-border-default bg-bg-surface/90 px-4 py-4 backdrop-blur sm:flex-row sm:items-center sm:justify-end sm:px-5">
        <button
          type="button"
          onClick={cancelOrEdit}
          disabled={submitting}
          className="min-h-11 rounded-md border border-border-default bg-bg-surface px-4 py-2.5 text-sm font-semibold text-text-secondary transition hover:bg-neutral-50 focus:outline-none focus-visible:ring-4 focus-visible:ring-neutral-200"
        >
          {saving ? (submitting ? "Submitting…" : "Stop upload and edit") : "Cancel"}
        </button>

        <CustomerActionNativeButton
          type="submit"
          disabled={saving || verifyingPayOs || !payOsVerified}
          className="inline-flex min-h-11 items-center justify-center gap-2 rounded-md bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700 focus:outline-none focus-visible:ring-4 focus-visible:ring-primary-200 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? <LoaderCircle size={17} className="animate-spin" /> : <Save size={17} />}

          {saving ? "Saving…" : isEditing ? "Save and resubmit" : "Submit application"}
        </CustomerActionNativeButton>
      </footer>
    </CustomerMutationForm>
  );
}

/*  Layout pieces */
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
    <section className="grid gap-4 py-5 lg:grid-cols-[180px_minmax(0,1fr)] lg:gap-6">
      <div className="flex items-start gap-3 lg:block">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary-50 text-primary-700">
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

/*  Field */
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
  lang,
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
  lang?: string;
  error?: string;
}) {
  const hasError = Boolean(error);

  return (
    <label className={`${labelClass} ${className}`}>
      <span>
        {label}
        {required && <span className="ml-0.5 text-danger">*</span>}
      </span>

      {type === "password" ? (
        <SecretInput
          secretLabel={label}
          lang={lang}
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
      ) : (
        <input
          type={type}
          lang={lang}
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
      )}

      {error && (
        <span role="alert" className="mt-1 block text-xs font-normal text-danger">
          {error}
        </span>
      )}
    </label>
  );
}

/*  Logo + cover */
function BrandingUploader({
  logo,
  cover,
  existingLogoUrl,
  existingCoverUrl,
  onLogoChange,
  onCoverChange,
  logoError,
}: {
  logo?: File;
  cover?: File;
  existingLogoUrl?: string | null;
  existingCoverUrl?: string | null;
  onLogoChange: (file?: File) => void;
  onCoverChange: (file?: File) => void;
  logoError?: string;
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
          className={`group relative block h-44 cursor-pointer overflow-hidden rounded-lg border border-border-default bg-gradient-to-br from-primary-50 to-neutral-50 transition sm:h-52 ${focusRing}`}
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

          <CustomerActionFileInput
            accept={accept}
            className="sr-only"
            onChange={(event) => {
              onCoverChange(event.target.files?.[0]);
              event.currentTarget.value = "";
            }}
          />
        </label>

        {cover && (
          <CustomerActionNativeButton
            type="button"
            onClick={() => onCoverChange(undefined)}
            className="absolute right-3 top-3 z-10 inline-flex items-center gap-1 rounded-full bg-white/95 px-3 py-1.5 text-xs font-semibold text-danger shadow transition hover:bg-white"
          >
            <X size={13} />
            Remove
          </CustomerActionNativeButton>
        )}

        {/* Logo */}
        <label
          className={`group absolute bottom-0 left-5 z-10 block h-24 w-24 cursor-pointer overflow-hidden rounded-3xl border-4 bg-white shadow-lg shadow-black/10 ring-1 ${
            logoError ? "border-danger ring-danger" : "border-white ring-border-default"
          } ${focusRing}`}
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

          <CustomerActionFileInput
            accept={accept}
            className="sr-only"
            onChange={(event) => {
              onLogoChange(event.target.files?.[0]);
              event.currentTarget.value = "";
            }}
          />
        </label>

        <div className="absolute bottom-1 left-[8.25rem] right-0 min-w-0">
          <p className="text-sm font-medium text-text-primary">
            Logo<span className="ml-0.5 text-danger">*</span>
          </p>

          <div className="flex items-center gap-3">
            <p className="truncate text-xs text-text-secondary">
              {logo ? logo.name : existingLogoUrl ? "Current logo" : "Square image works best"}
            </p>

            {logo && (
              <CustomerActionNativeButton
                type="button"
                onClick={() => onLogoChange(undefined)}
                className="shrink-0 text-xs font-medium text-danger hover:underline"
              >
                Remove
              </CustomerActionNativeButton>
            )}
          </div>
        </div>
      </div>

      {logoError && (
        <p role="alert" className="mt-1 text-xs font-normal text-danger">
          {logoError}
        </p>
      )}

      {cover && <p className="mt-1 truncate text-xs text-text-secondary">Cover: {cover.name}</p>}
    </div>
  );
}

/*  Documents */
function ExistingDocument({ document }: { document: ShopApplication["documents"][number] }) {
  return (
    <a
      href={document.fileUrl}
      target="_blank"
      rel="noreferrer"
      className="group flex items-center gap-3 rounded-md border border-border-default bg-bg-surface px-3.5 py-3 text-sm transition hover:border-primary-300 hover:bg-primary-50 focus:outline-none focus-visible:ring-4 focus-visible:ring-primary-100"
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
    <div className="flex items-center gap-3 rounded-md border border-primary-200 bg-primary-50/40 px-3.5 py-3">
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

      <CustomerActionNativeButton
        type="button"
        onClick={onRemove}
        aria-label={`Remove ${documentText(document.type)}`}
        className="shrink-0 rounded-full p-1.5 text-text-muted transition hover:bg-danger/10 hover:text-danger focus:outline-none focus-visible:ring-2 focus-visible:ring-danger"
      >
        <X size={16} />
      </CustomerActionNativeButton>
    </div>
  );
}

function DocumentUploader({ onAdd }: { onAdd: (type: ShopDocumentType, files: File[]) => void }) {
  const [type, setType] = useState<ShopDocumentType>("BusinessLicense");

  return (
    <div className="rounded-md border border-dashed border-border-default bg-neutral-50 p-4 transition hover:border-primary-400">
      <div className="flex flex-col gap-3 sm:flex-row">
        <select
          aria-label="Document type"
          value={type}
          onChange={(event) => setType(event.target.value as ShopDocumentType)}
          className="w-full rounded-md border border-border-default bg-bg-surface px-3.5 py-2.5 text-sm text-text-primary outline-none transition focus:border-primary-500 focus:ring-4 focus:ring-primary-100 sm:w-64"
        >
          <option value="BusinessLicense">Business license</option>
          <option value="FoodSafetyCertificate">Food safety certificate</option>
          <option value="Other">Other document</option>
        </select>

        <label
          className={`inline-flex flex-1 cursor-pointer items-center justify-center gap-2 rounded-md border border-primary-200 bg-bg-surface px-4 py-2.5 text-sm font-semibold text-primary-700 transition hover:bg-primary-50 ${focusRing}`}
        >
          <Upload size={16} />
          Choose files
          <CustomerActionFileInput
            multiple
            accept="application/pdf,image/jpeg,image/png,image/webp"
            className="sr-only"
            onChange={(event) => {
              const fileList = event.target.files;
              if (fileList && fileList.length > 0) {
                onAdd(type, Array.from(fileList));
              }

              event.currentTarget.value = "";
            }}
          />
        </label>
      </div>

      <p className="mt-3 text-xs text-text-secondary">
        PDF, JPG, PNG or WebP. Up to 10 documents in total across all types. Select multiple files
        for any document type. Adding files keeps your current selection.
      </p>
    </div>
  );
}

export function ShopApplicationForm(props: Parameters<typeof ShopApplicationFormContent>[0]) {
  return (
    <UploadTaskGuard scope={{ purpose: "shop-application" }}>
      <ShopApplicationFormContent {...props} />
    </UploadTaskGuard>
  );
}
