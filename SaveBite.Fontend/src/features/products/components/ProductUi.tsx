import { useEffect, useId, useRef, type ReactNode } from "react";
import { AlertCircle, LoaderCircle, Package, X } from "lucide-react";

export const inputClass =
  "mt-1.5 w-full rounded-lg border border-border-default bg-white px-3.5 py-2.5 text-sm text-text-primary outline-none transition focus:border-primary-400 focus:ring-3 focus:ring-primary-100 disabled:bg-neutral-50 disabled:text-neutral-500";
export const primaryButton =
  "inline-flex min-h-10 items-center justify-center gap-2 rounded-lg bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-700 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-200 disabled:cursor-not-allowed disabled:opacity-50";
export const secondaryButton =
  "inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-neutral-700 transition hover:bg-neutral-50 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-200 disabled:cursor-not-allowed disabled:opacity-50";
export const iconButton =
  "inline-flex size-8 shrink-0 items-center justify-center rounded-md text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900 focus-visible:ring-2 focus-visible:ring-primary-400 disabled:opacity-30";
export function Panel({
  title,
  description,
  children,
  aside,
}: {
  title: string;
  description?: string;
  children: ReactNode;
  aside?: ReactNode;
}) {
  return (
    <section className="rounded-xl border border-border-default bg-white shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-neutral-100 px-5 py-4">
        <div>
          <h2 className="font-semibold text-neutral-900">{title}</h2>
          {description && <p className="mt-1 text-sm text-text-secondary">{description}</p>}
        </div>
        {aside}
      </div>
      <div className="p-5">{children}</div>
    </section>
  );
}
export function Alert({ children, success = false }: { children: ReactNode; success?: boolean }) {
  return (
    <div
      role={success ? "status" : "alert"}
      className={`flex gap-3 rounded-lg border px-4 py-3 text-sm ${success ? "border-primary-200 bg-primary-50 text-primary-800" : "border-red-200 bg-red-50 text-red-700"}`}
    >
      <AlertCircle className="mt-0.5 size-4 shrink-0" />
      <div>{children}</div>
    </div>
  );
}
export function Loading() {
  return (
    <div
      role="status"
      className="flex min-h-64 items-center justify-center gap-3 text-sm text-neutral-500"
    >
      <LoaderCircle className="size-5 animate-spin" />
      Loading…
    </div>
  );
}
export function ErrorState({ error, retry }: { error: string; retry: () => void }) {
  return (
    <div className="space-y-4 py-8">
      <Alert>{error}</Alert>
      <button className={secondaryButton} onClick={retry}>
        Try again
      </button>
    </div>
  );
}
export function Empty({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center rounded-xl border border-dashed border-neutral-300 bg-white px-6 py-14 text-center">
      <span className="mb-5 rounded-2xl bg-primary-50 p-4 text-primary-600">
        <Package className="size-8" />
      </span>
      <h2 className="text-lg font-semibold">{title}</h2>
      <p className="mt-2 max-w-md text-sm leading-6 text-text-secondary">{description}</p>
      {children && <div className="mt-6">{children}</div>}
    </div>
  );
}
export function ProductPhoto({
  src,
  alt,
  className = "size-14",
}: {
  src?: string;
  alt: string;
  className?: string;
}) {
  return (
    <div className={`${className} shrink-0 overflow-hidden rounded-lg bg-neutral-100`}>
      {src ? (
        <img
          src={src}
          alt={alt}
          className="size-full object-cover"
          loading="lazy"
          onError={(e) => {
            e.currentTarget.style.display = "none";
          }}
        />
      ) : (
        <div className="flex size-full items-center justify-center text-neutral-400">
          <Package className="size-6" aria-label="No image available" />
        </div>
      )}
    </div>
  );
}
export function FilePreview({
  file,
  className = "size-full object-cover",
}: {
  file: File;
  className?: string;
}) {
  const ref = useRef<HTMLImageElement>(null);
  useEffect(() => {
    const url = URL.createObjectURL(file);
    if (ref.current) ref.current.src = url;
    return () => URL.revokeObjectURL(url);
  }, [file]);
  return <img ref={ref} alt={file.name} className={className} />;
}
export function StatusBadge({ hidden }: { hidden: boolean }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-medium ${hidden ? "bg-neutral-100 text-neutral-600" : "bg-primary-50 text-primary-700"}`}
    >
      <span className={`size-1.5 rounded-full ${hidden ? "bg-neutral-400" : "bg-primary-500"}`} />
      {hidden ? "Hidden" : "Active"}
    </span>
  );
}
export function Modal({
  title,
  children,
  onClose,
  busy = false,
}: {
  title: string;
  children: ReactNode;
  onClose: () => void;
  busy?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const id = useId();
  useEffect(() => {
    const dialog = ref.current!;
    dialog.showModal();
    return () => dialog.close();
  }, []);
  return (
    <dialog
      ref={ref}
      aria-labelledby={id}
      onCancel={(e) => {
        e.preventDefault();
        if (!busy) onClose();
      }}
      className="fixed inset-0 m-auto w-[calc(100%_-_2rem)] max-w-md rounded-xl border-0 bg-white p-0 shadow-lg backdrop:bg-neutral-900/50"
    >
      <div className="flex items-center justify-between border-b border-neutral-100 px-5 py-4">
        <h2 id={id} className="font-semibold">
          {title}
        </h2>
        <button
          type="button"
          disabled={busy}
          onClick={onClose}
          className={iconButton}
          aria-label="Close"
        >
          <X className="size-5" />
        </button>
      </div>
      <div className="p-5">{children}</div>
    </dialog>
  );
}
