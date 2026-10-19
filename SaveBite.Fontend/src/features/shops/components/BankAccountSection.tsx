import { useEffect, useId, useState } from "react";
import { AlertCircle, Check, Copy, Info, Landmark, Loader2, Pencil, Plus, X } from "lucide-react";

import {
  bankAccountSchema,
  getFieldErrors,
  type BankAccountValues,
  type FieldErrors,
} from "../schemas/ownerShop.schema";

type BankAccountSectionProps = {
  initial: BankAccountValues;
  saving: boolean;
  onSave: (values: BankAccountValues) => Promise<void>;
};

/* ------------------------------------------------------------------ */
/* Styles                                                              */
/* ------------------------------------------------------------------ */

const ghostButton =
  "inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-3.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/20 disabled:cursor-not-allowed disabled:opacity-50";

const softButton =
  "inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-teal-50 px-3.5 text-sm font-medium text-teal-800 ring-1 ring-inset ring-teal-700/20 transition-colors hover:bg-teal-100 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/25 disabled:cursor-not-allowed disabled:opacity-50";

const primaryButton =
  "inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-teal-800 px-4 text-sm font-semibold text-white transition-colors hover:bg-teal-900 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/30 disabled:cursor-not-allowed disabled:opacity-60";

/* ------------------------------------------------------------------ */
/* Helpers                                                             */
/* ------------------------------------------------------------------ */

function normalizeValues(values: BankAccountValues): BankAccountValues {
  return {
    bankName: values.bankName ?? "",
    bankAccountNumber: values.bankAccountNumber ?? "",
    bankAccountHolder: values.bankAccountHolder ?? "",
  };
}

/** "970436123456" -> "9704 3612 3456" */
function groupDigits(value: string): string {
  return value
    .replace(/\s+/g, "")
    .replace(/(.{4})/g, "$1 ")
    .trim();
}

/* ------------------------------------------------------------------ */
/* Field                                                               */
/* ------------------------------------------------------------------ */

function Field({
  id,
  label,
  value,
  error,
  placeholder,
  disabled,
  inputMode,
  mono = false,
  onChange,
  onBlur,
}: {
  id: string;
  label: string;
  value: string;
  error?: string;
  placeholder: string;
  disabled?: boolean;
  inputMode?: "numeric";
  mono?: boolean;
  onChange: (value: string) => void;
  onBlur: () => void;
}) {
  return (
    <div className="min-w-0">
      <label htmlFor={id} className="block text-sm font-medium text-slate-800">
        {label}
      </label>

      <input
        id={id}
        value={value}
        placeholder={placeholder}
        disabled={disabled}
        inputMode={inputMode}
        autoComplete="off"
        onChange={(event) => onChange(event.target.value)}
        onBlur={onBlur}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? `${id}-error` : undefined}
        className={[
          "mt-1.5 block h-11 w-full rounded-lg border bg-white px-3.5 text-sm text-slate-900 placeholder:text-slate-400",
          "transition-shadow focus:outline-none focus:ring-4 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500",
          mono ? "font-mono tabular-nums tracking-wider" : "",
          error
            ? "border-red-500 focus:border-red-600 focus:ring-red-600/15"
            : "border-slate-300 focus:border-teal-700 focus:ring-teal-700/15",
        ].join(" ")}
      />

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

/* ------------------------------------------------------------------ */
/* Bank card (saved view + live preview)                               */
/* ------------------------------------------------------------------ */

function BankCard({ bankName, bankAccountNumber, bankAccountHolder }: BankAccountValues) {
  const name = bankName.trim();
  const number = bankAccountNumber.trim();
  const holder = bankAccountHolder.trim();

  const placeholder = "text-white/35";

  return (
    <dl className="relative flex aspect-[1.7/1] w-full max-w-md flex-col justify-between overflow-hidden rounded-2xl bg-teal-900 p-5 text-white shadow-lg shadow-teal-950/20 ring-1 ring-inset ring-white/10 sm:p-6">
      {/* Ring motif, purely decorative */}
      <span
        aria-hidden
        className="pointer-events-none absolute -right-14 -top-14 h-48 w-48 rounded-full border-[28px] border-white/[0.05]"
      />

      <div className="relative flex items-start justify-between gap-4">
        <div className="min-w-0">
          <dt className="sr-only">Bank name</dt>
          <dd
            className={`truncate text-lg font-semibold tracking-tight ${name ? "" : placeholder}`}
          >
            {name || "Bank name"}
          </dd>
        </div>

        {/* Chip */}
        <span
          aria-hidden
          className="mt-0.5 grid h-8 w-11 shrink-0 grid-cols-2 gap-px overflow-hidden rounded-md bg-amber-200/90 p-1"
        >
          <span className="rounded-sm bg-amber-300/70" />
          <span className="rounded-sm bg-amber-300/70" />
          <span className="rounded-sm bg-amber-300/70" />
          <span className="rounded-sm bg-amber-300/70" />
        </span>
      </div>

      <div className="relative">
        <dt className="sr-only">Account number</dt>
        <dd
          className={`break-all font-mono text-xl tabular-nums tracking-[0.12em] sm:text-2xl ${
            number ? "" : placeholder
          }`}
        >
          {number ? groupDigits(number) : "0000 0000 0000"}
        </dd>
      </div>

      <div className="relative min-w-0">
        <dt className="text-xs text-white/55">Account holder</dt>
        <dd className={`mt-0.5 truncate text-sm font-medium ${holder ? "" : placeholder}`}>
          {holder || "Name on the account"}
        </dd>
      </div>
    </dl>
  );
}

/* ------------------------------------------------------------------ */
/* Section                                                             */
/* ------------------------------------------------------------------ */

export default function BankAccountSection({ initial, saving, onSave }: BankAccountSectionProps) {
  const id = useId();

  const [values, setValues] = useState<BankAccountValues>(() => normalizeValues(initial));
  const [errors, setErrors] = useState<FieldErrors>({});
  const [editing, setEditing] = useState(false);
  const [copied, setCopied] = useState(false);
  const [saveError, setSaveError] = useState("");

  useEffect(() => {
    setValues(normalizeValues(initial));
    setErrors({});
    setEditing(false);
  }, [initial]);

  useEffect(() => {
    if (!copied) return;

    const timeoutId = window.setTimeout(() => setCopied(false), 1800);
    return () => window.clearTimeout(timeoutId);
  }, [copied]);

  const updateField = (field: keyof BankAccountValues, value: string) => {
    const nextValues = { ...values, [field]: value };
    setValues(nextValues);
    setSaveError("");

    // Only refresh errors once the form has started showing them.
    if (Object.keys(errors).length > 0) {
      setErrors(getFieldErrors(bankAccountSchema, nextValues));
    }
  };

  const handleBlur = () => {
    setErrors(getFieldErrors(bankAccountSchema, values));
  };

  const startEditing = () => {
    setErrors({});
    setSaveError("");
    setEditing(true);
  };

  const handleCancel = () => {
    setValues(normalizeValues(initial));
    setErrors({});
    setSaveError("");
    setEditing(false);
  };

  const handleSave = async () => {
    const nextErrors = getFieldErrors(bankAccountSchema, values);
    setErrors(nextErrors);
    setSaveError("");

    if (Object.keys(nextErrors).length > 0) return;

    try {
      await onSave({
        bankName: values.bankName.trim(),
        bankAccountNumber: values.bankAccountNumber.trim(),
        bankAccountHolder: values.bankAccountHolder.trim(),
      });

      setEditing(false);
      setErrors({});
    } catch {
      setSaveError("Unable to save bank details. Please try again.");
    }
  };

  const handleCopy = async () => {
    if (!values.bankAccountNumber) return;

    try {
      await navigator.clipboard.writeText(values.bankAccountNumber.trim());
      setSaveError("");
      setCopied(true);
    } catch {
      setSaveError("Unable to copy the account number.");
    }
  };

  const hasBankAccount = Boolean(
    values.bankName.trim() || values.bankAccountNumber.trim() || values.bankAccountHolder.trim(),
  );

  return (
    <section className="rounded-2xl border border-slate-200 bg-white">
      {/* Header */}
      <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-5 py-4 sm:px-6">
        <div className="flex min-w-0 items-center gap-3">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-700">
            <Landmark size={20} />
          </span>

          <div className="min-w-0">
            <h2 className="text-base font-semibold text-slate-900">Bank Account</h2>
            <p className="mt-0.5 text-sm text-slate-500">
              The account used to receive your payouts.
            </p>
          </div>
        </div>

        {!editing && hasBankAccount && (
          <button type="button" onClick={startEditing} className={softButton}>
            <Pencil size={15} />
            Edit Bank Details
          </button>
        )}
      </header>

      <div className="px-5 py-6 sm:px-6">
        {/* View: configured */}
        {!editing && hasBankAccount && (
          <div className="flex flex-col items-center gap-5 py-2">
            <BankCard {...values} />

            {values.bankAccountNumber && (
              <button
                type="button"
                onClick={() => void handleCopy()}
                aria-live="polite"
                className={`inline-flex h-10 items-center justify-center gap-2 rounded-full px-5 text-sm font-medium ring-1 ring-inset transition-all focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-700/25 active:scale-[0.97] ${
                  copied
                    ? "bg-teal-800 text-white ring-teal-800"
                    : "bg-teal-50 text-teal-800 ring-teal-700/25 hover:bg-teal-100 hover:ring-teal-700/40"
                }`}
              >
                {copied ? <Check size={16} strokeWidth={2.5} /> : <Copy size={15} />}
                {copied ? "Copied to clipboard" : "Copy account number"}
              </button>
            )}

            {saveError && (
              <p role="alert" className="flex items-center gap-1.5 text-sm text-red-600">
                <AlertCircle size={15} className="shrink-0" />
                {saveError}
              </p>
            )}
          </div>
        )}

        {/* View: not configured */}
        {!editing && !hasBankAccount && (
          <div className="flex flex-col items-center gap-6 py-2 text-center">
            {/* Blank card placeholder */}
            <div
              aria-hidden
              className="relative flex aspect-[1.7/1] w-full max-w-md flex-col justify-between rounded-2xl border-2 border-dashed border-teal-700/25 bg-teal-50/50 p-5 sm:p-6"
            >
              <div className="flex items-start justify-between gap-4">
                <span className="h-3.5 w-28 rounded-full bg-teal-700/10" />
                <span className="h-8 w-11 rounded-md border-2 border-dashed border-teal-700/25" />
              </div>

              <div className="flex gap-3">
                <span className="h-3.5 w-14 rounded-full bg-teal-700/10" />
                <span className="h-3.5 w-14 rounded-full bg-teal-700/10" />
                <span className="h-3.5 w-14 rounded-full bg-teal-700/10" />
              </div>

              <div className="space-y-1.5 text-left">
                <span className="block h-2 w-16 rounded-full bg-teal-700/10" />
                <span className="block h-3 w-36 rounded-full bg-teal-700/10" />
              </div>

              <span className="absolute inset-0 flex items-center justify-center">
                <span className="flex h-12 w-12 items-center justify-center rounded-full bg-white text-teal-700 shadow-sm ring-1 ring-teal-700/20">
                  <Plus size={22} />
                </span>
              </span>
            </div>

            <div className="max-w-sm">
              <h3 className="text-sm font-semibold text-slate-900">No bank account yet</h3>
              <p className="mt-1 text-sm leading-6 text-slate-500">
                Add your bank name, account number and account holder so you can receive payouts.
              </p>
            </div>

            <button type="button" onClick={startEditing} className={primaryButton}>
              <Plus size={16} />
              Add Bank Account
            </button>
          </div>
        )}

        {/* Edit */}
        {editing && (
          <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_22rem] lg:gap-10">
            <div className="min-w-0 space-y-5">
              <div>
                <h3 className="text-sm font-semibold text-slate-900">
                  {hasBankAccount ? "Edit Bank Account" : "Add Bank Account"}
                </h3>
                <p className="mt-1 flex items-start gap-1.5 text-sm leading-6 text-slate-500">
                  <Info size={15} className="mt-1 shrink-0 text-slate-400" />
                  <span>
                    Fill in all three fields to save an account, or leave all three empty to remove
                    it.
                  </span>
                </p>
              </div>

              <Field
                id={`${id}-bank-name`}
                label="Bank name"
                value={values.bankName}
                error={errors.bankName}
                placeholder="e.g. Vietcombank"
                disabled={saving}
                onChange={(value) => updateField("bankName", value)}
                onBlur={handleBlur}
              />

              <Field
                id={`${id}-account-number`}
                label="Account number"
                value={values.bankAccountNumber}
                error={errors.bankAccountNumber}
                placeholder="Digits only"
                disabled={saving}
                inputMode="numeric"
                mono
                onChange={(value) => updateField("bankAccountNumber", value)}
                onBlur={handleBlur}
              />

              <Field
                id={`${id}-account-holder`}
                label="Account holder"
                value={values.bankAccountHolder}
                error={errors.bankAccountHolder}
                placeholder="Name on the account"
                disabled={saving}
                onChange={(value) => updateField("bankAccountHolder", value)}
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
                  {saving ? "Saving..." : "Save changes"}
                </button>
              </div>
            </div>

            {/* Live preview (decorative duplicate of the form values) */}
            <div aria-hidden className="min-w-0 lg:pt-7">
              <p className="mb-2 text-xs font-medium text-slate-500">Preview</p>
              <BankCard {...values} />
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
