import { useEffect, useId, useState } from "react";
import {
  AlertCircle,
  Check,
  CheckCircle2,
  Eye,
  EyeOff,
  Info,
  KeyRound,
  Loader2,
  Pencil,
  ShieldCheck,
  X,
} from "lucide-react";

import {
  createPayOSSchema,
  getFieldErrors,
  type FieldErrors,
  type PayOSValues,
} from "../schemas/ownerShop.schema";

type PayOSInitialValues = {
  payosClientId: string;
  hasPayosApiKey: boolean;
  hasPayosChecksumKey: boolean;
};

type PayOSSectionProps = {
  initial: PayOSInitialValues;
  saving: boolean;
  onSave: (values: PayOSValues) => Promise<void>;
};

const EMPTY_VALUES: PayOSValues = {
  payosClientId: "",
  payosApiKey: "",
  payosChecksumKey: "",
};

const ghostButton =
  "inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-3.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/20 disabled:cursor-not-allowed disabled:opacity-50";

const softButton =
  "inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-teal-50 px-3.5 text-sm font-medium text-teal-800 ring-1 ring-inset ring-teal-700/20 transition-colors hover:bg-teal-100 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/25 disabled:cursor-not-allowed disabled:opacity-50";

const primaryButton =
  "inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-teal-800 px-4 text-sm font-semibold text-white transition-colors hover:bg-teal-900 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/30 disabled:cursor-not-allowed disabled:opacity-60";

function StatusBadge({
  configured,
  configuredLabel = "Configured",
  missingLabel = "Not configured",
}: {
  configured: boolean;
  configuredLabel?: string;
  missingLabel?: string;
}) {
  return configured ? (
    <span className="inline-flex items-center gap-1.5 rounded-full bg-teal-50 px-2.5 py-1 text-xs font-medium text-teal-800 ring-1 ring-inset ring-teal-700/20">
      <CheckCircle2 size={13} />
      {configuredLabel}
    </span>
  ) : (
    <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-50 px-2.5 py-1 text-xs font-medium text-amber-800 ring-1 ring-inset ring-amber-300/60">
      <AlertCircle size={13} />
      {missingLabel}
    </span>
  );
}

function PayOSField({
  id,
  label,
  value,
  error,
  placeholder,
  disabled,
  secret = false,
  configured,
  visible = false,
  onToggleVisibility,
  onChange,
  onBlur,
}: {
  id: string;
  label: string;
  value: string;
  error?: string;
  placeholder: string;
  disabled?: boolean;
  secret?: boolean;
  configured?: boolean;
  visible?: boolean;
  onToggleVisibility?: () => void;
  onChange: (value: string) => void;
  onBlur: () => void;
}) {
  return (
    <div className="min-w-0">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <label htmlFor={id} className="block text-sm font-medium text-slate-800">
          {label}
        </label>

        {secret && configured !== undefined && <StatusBadge configured={configured} />}
      </div>

      <div className="relative mt-1.5">
        {secret && (
          <KeyRound
            size={16}
            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400"
          />
        )}

        <input
          id={id}
          type={secret && !visible ? "password" : "text"}
          value={value}
          placeholder={placeholder}
          disabled={disabled}
          spellCheck={false}
          onChange={(event) => onChange(event.target.value)}
          onBlur={onBlur}
          autoComplete={secret ? "new-password" : "off"}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${id}-error` : undefined}
          className={[
            "block h-11 w-full rounded-lg border bg-white px-3.5 font-mono text-sm text-slate-900 placeholder:font-sans placeholder:text-slate-400",
            "transition-shadow focus:outline-none focus:ring-4 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500",
            secret ? "pl-10 pr-11" : "",
            error
              ? "border-red-500 focus:border-red-600 focus:ring-red-600/15"
              : "border-slate-300 focus:border-teal-700 focus:ring-teal-700/15",
          ].join(" ")}
        />

        {secret && (
          <button
            type="button"
            onClick={onToggleVisibility}
            disabled={disabled}
            aria-label={visible ? `Hide ${label}` : `Show ${label}`}
            aria-pressed={visible}
            className="absolute right-1.5 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-teal-50 hover:text-teal-800 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/20 disabled:opacity-50"
          >
            {visible ? <EyeOff size={16} /> : <Eye size={16} />}
          </button>
        )}
      </div>

      {error && (
        <p
          id={`${id}-error`}
          role="alert"
          className="mt-1.5 flex items-start gap-1.5 text-xs leading-5 text-red-600"
        >
          <AlertCircle size={13} className="mt-0.5 shrink-0" />
          <span>{error}</span>
        </p>
      )}
    </div>
  );
}

export default function PayOSSection({ initial, saving, onSave }: PayOSSectionProps) {
  const id = useId();

  const [values, setValues] = useState<PayOSValues>({
    ...EMPTY_VALUES,
    payosClientId: initial.payosClientId ?? "",
  });

  const [errors, setErrors] = useState<FieldErrors>({});
  const [editing, setEditing] = useState(false);
  const [visibleSecrets, setVisibleSecrets] = useState({
    apiKey: false,
    checksumKey: false,
  });
  const [saveError, setSaveError] = useState("");
  const [saved, setSaved] = useState(false);

  const schema = createPayOSSchema({
    hasPayosApiKey: initial.hasPayosApiKey,
    hasPayosChecksumKey: initial.hasPayosChecksumKey,
  });

  useEffect(() => {
    setValues({
      ...EMPTY_VALUES,
      payosClientId: initial.payosClientId ?? "",
    });
    setErrors({});
    setEditing(false);
    setSaveError("");
    setSaved(false);
    setVisibleSecrets({ apiKey: false, checksumKey: false });
  }, [initial.payosClientId, initial.hasPayosApiKey, initial.hasPayosChecksumKey]);

  const isConfigured =
    Boolean(initial.payosClientId) && initial.hasPayosApiKey && initial.hasPayosChecksumKey;

  const updateField = (field: keyof PayOSValues, value: string) => {
    const nextValues = { ...values, [field]: value };

    setValues(nextValues);
    setSaveError("");
    setSaved(false);

    if (Object.keys(errors).length > 0) {
      setErrors(getFieldErrors(schema, nextValues));
    }
  };

  const handleBlur = () => {
    setErrors(getFieldErrors(schema, values));
  };

  const startEditing = () => {
    setErrors({});
    setSaveError("");
    setSaved(false);
    setEditing(true);
  };

  const handleCancel = () => {
    setValues({
      ...EMPTY_VALUES,
      payosClientId: initial.payosClientId ?? "",
    });
    setErrors({});
    setSaveError("");
    setEditing(false);
    setVisibleSecrets({ apiKey: false, checksumKey: false });
  };

  const handleSave = async () => {
    const nextErrors = getFieldErrors(schema, values);

    setErrors(nextErrors);
    setSaveError("");
    setSaved(false);

    if (Object.keys(nextErrors).length > 0) return;

    try {
      await onSave({
        payosClientId: values.payosClientId.trim(),
        payosApiKey: values.payosApiKey.trim(),
        payosChecksumKey: values.payosChecksumKey.trim(),
      });

      setEditing(false);
      setErrors({});
      setSaved(true);

      // Do not keep secrets in state after saving.
      setValues((current) => ({
        ...current,
        payosApiKey: "",
        payosChecksumKey: "",
      }));
    } catch {
      setSaveError("Unable to save PayOS settings. Please try again.");
    }
  };

  const rows = [
    {
      label: "API Key",
      configured: initial.hasPayosApiKey,
    },
    {
      label: "Checksum Key",
      configured: initial.hasPayosChecksumKey,
    },
  ];

  return (
    <section className="rounded-2xl border border-slate-200 bg-white">
      {/* Header */}
      <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-5 py-4 sm:px-6">
        <div className="flex min-w-0 items-center gap-3">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-700">
            <ShieldCheck size={20} />
          </span>

          <div className="min-w-0">
            <h2 className="text-base font-semibold text-slate-900">PayOS Integration</h2>
            <p className="mt-0.5 text-sm text-slate-500">
              The credentials used to process payments through PayOS.
            </p>
          </div>
        </div>

        {!editing && (
          <button
            type="button"
            onClick={startEditing}
            className={isConfigured ? softButton : primaryButton}
          >
            <Pencil size={15} />
            {isConfigured ? "Edit PayOS Settings" : "Set up PayOS"}
          </button>
        )}
      </header>

      <div className="px-5 py-6 sm:px-6">
        {/* View */}
        {!editing && (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-sm font-semibold text-slate-900">
                {isConfigured ? "PayOS is configured" : "PayOS setup incomplete"}
              </p>
            </div>

            <dl className="divide-y divide-slate-200 overflow-hidden rounded-xl border border-slate-200">
              <div className="flex flex-col gap-1.5 px-4 py-3.5 sm:flex-row sm:items-center sm:justify-between sm:gap-6">
                <dt className="text-sm text-slate-500">Client ID</dt>
                <dd className="min-w-0 text-sm font-medium text-slate-900">
                  {initial.payosClientId ? (
                    <span className="break-all font-mono">{initial.payosClientId}</span>
                  ) : (
                    <span className="font-normal text-slate-400">Not provided</span>
                  )}
                </dd>
              </div>

              {rows.map(({ label, configured }) => (
                <div
                  key={label}
                  className="flex flex-col gap-1.5 px-4 py-3.5 sm:flex-row sm:items-center sm:justify-between sm:gap-6"
                >
                  <dt className="text-sm text-slate-500">{label}</dt>
                  <dd className="flex flex-wrap items-center gap-3">
                    {configured && (
                      <span
                        aria-hidden="true"
                        className="font-mono text-sm tracking-widest text-slate-400"
                      >
                        ••••••••••••
                      </span>
                    )}
                    <StatusBadge configured={configured} />
                  </dd>
                </div>
              ))}
            </dl>

            {saved && (
              <p role="status" className="flex items-center gap-1.5 text-sm text-teal-800">
                <Check size={16} strokeWidth={2.5} className="shrink-0" />
                PayOS settings saved.
              </p>
            )}
          </div>
        )}

        {/* Edit */}
        {editing && (
          <div className="space-y-5">
            <div>
              <h3 className="text-sm font-semibold text-slate-900">
                {isConfigured ? "Edit PayOS settings" : "Set up PayOS"}
              </h3>
              <p className="mt-1 flex items-start gap-1.5 text-sm leading-6 text-slate-500">
                <Info size={15} className="mt-1 shrink-0 text-slate-400" />
                <span>
                  Saved keys are never displayed. Leave a key blank to keep the current value, or
                  enter a new one to replace it.
                </span>
              </p>
            </div>

            <PayOSField
              id={`${id}-client-id`}
              label="Client ID"
              value={values.payosClientId}
              error={errors.payosClientId}
              placeholder="Enter PayOS Client ID"
              disabled={saving}
              onChange={(value) => updateField("payosClientId", value)}
              onBlur={handleBlur}
            />

            <PayOSField
              id={`${id}-api-key`}
              label="API Key"
              value={values.payosApiKey}
              error={errors.payosApiKey}
              placeholder={
                initial.hasPayosApiKey
                  ? "Leave blank to keep the current key"
                  : "Enter PayOS API Key"
              }
              disabled={saving}
              secret
              configured={initial.hasPayosApiKey}
              visible={visibleSecrets.apiKey}
              onToggleVisibility={() =>
                setVisibleSecrets((current) => ({
                  ...current,
                  apiKey: !current.apiKey,
                }))
              }
              onChange={(value) => updateField("payosApiKey", value)}
              onBlur={handleBlur}
            />

            <PayOSField
              id={`${id}-checksum-key`}
              label="Checksum Key"
              value={values.payosChecksumKey}
              error={errors.payosChecksumKey}
              placeholder={
                initial.hasPayosChecksumKey
                  ? "Leave blank to keep the current key"
                  : "Enter PayOS Checksum Key"
              }
              disabled={saving}
              secret
              configured={initial.hasPayosChecksumKey}
              visible={visibleSecrets.checksumKey}
              onToggleVisibility={() =>
                setVisibleSecrets((current) => ({
                  ...current,
                  checksumKey: !current.checksumKey,
                }))
              }
              onChange={(value) => updateField("payosChecksumKey", value)}
              onBlur={handleBlur}
            />

            {saveError && (
              <div
                role="alert"
                className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3.5 py-3 text-sm text-red-700"
              >
                <AlertCircle size={16} className="mt-0.5 shrink-0" />
                <span>{saveError}</span>
              </div>
            )}

            <div className="flex flex-col-reverse gap-2 border-t border-slate-200 pt-5 sm:flex-row sm:justify-end">
              <button
                type="button"
                disabled={saving}
                onClick={handleCancel}
                className={ghostButton}
              >
                <X size={15} />
                Cancel
              </button>

              <button
                type="button"
                disabled={saving}
                onClick={() => void handleSave()}
                className={primaryButton}
              >
                {saving && <Loader2 size={15} className="animate-spin" />}
                {saving ? "Saving..." : "Save settings"}
              </button>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
