import type { ReactNode } from "react";

interface FormFieldProps {
  label: string;
  htmlFor: string;
  error?: string;
  hint?: string;
  children: ReactNode;
}

export function FormField({ label, htmlFor, error, hint, children }: FormFieldProps) {
  return (
    <div>
      <label htmlFor={htmlFor} className="mb-1.5 block text-sm font-semibold text-text-primary">
        {label}
      </label>
      {children}
      {hint && !error && <p className="mt-1.5 text-xs text-text-secondary">{hint}</p>}
      {error && <p className="mt-1.5 text-xs font-medium text-danger">{error}</p>}
    </div>
  );
}

interface ModalActionsProps {
  onCancel: () => void;
  isSubmitting: boolean;
  submitLabel: string;
  serverError?: string | null;
}

export function ModalActions({
  onCancel,
  isSubmitting,
  submitLabel,
  serverError,
}: ModalActionsProps) {
  return (
    <>
      {serverError && (
        <p role="alert" className="rounded-md bg-danger/10 p-3 text-sm text-danger">
          {serverError}
        </p>
      )}
      <div className="flex justify-end gap-3 pt-2">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-md border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-secondary transition-colors hover:bg-neutral-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-md bg-primary-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-700 disabled:opacity-60"
        >
          {isSubmitting ? "Saving..." : submitLabel}
        </button>
      </div>
    </>
  );
}
