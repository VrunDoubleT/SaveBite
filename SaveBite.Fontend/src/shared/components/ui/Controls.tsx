import { authInputStyle } from "./inputStyles";
import { Link, type LinkProps } from "react-router-dom";
import { LoaderCircle } from "lucide-react";
import { useId, type ComponentProps, type ReactNode } from "react";

const variants = {
  primary: "border-primary-700 bg-primary-700 text-text-inverse hover:bg-primary-800",
  secondary: "border-border-default bg-bg-surface text-text-primary hover:bg-neutral-100",
  ghost: "border-transparent bg-transparent text-text-secondary hover:bg-neutral-100",
  danger: "border-danger-strong bg-danger-strong text-text-inverse hover:bg-danger-strong/90",
};
const sizes = {
  icon: "size-10 p-0 text-body",
  sm: "min-h-8 px-3 text-caption",
  md: "min-h-10 px-4 text-body",
  lg: "min-h-12 px-6 text-body",
};
type ButtonProps = ComponentProps<"button"> & {
  variant?: keyof typeof variants;
  size?: keyof typeof sizes;
  loading?: boolean;
};
export function Button({
  variant = "primary",
  size = "md",
  loading = false,
  disabled,
  type = "button",
  className = "",
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      {...props}
      type={type}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={`inline-flex shrink-0 items-center justify-center gap-2 rounded-md border font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${variants[variant]} ${sizes[size]} ${className}`}
    >
      {loading && <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />}
      {children}
    </button>
  );
}
/** Navigation action with the same appearance and focus treatment as Button. */
export function ButtonLink({
  variant = "primary",
  size = "md",
  className = "",
  ...props
}: LinkProps & { variant?: keyof typeof variants; size?: keyof typeof sizes }) {
  return (
    <Link
      {...props}
      className={`inline-flex shrink-0 items-center justify-center gap-2 rounded-md border font-semibold transition-colors ${variants[variant]} ${sizes[size]} ${className}`}
    />
  );
}
export function IconButton({
  children,
  className = "",
  ...props
}: Omit<ButtonProps, "children" | "size"> & { "aria-label": string; children: ReactNode }) {
  return (
    <Button variant="ghost" {...props} size="icon" className={className}>
      {children}
    </Button>
  );
}
const fieldClass =
  "w-full rounded-md border border-border-default bg-bg-surface px-3 py-2 text-body text-text-primary placeholder:text-text-muted transition-colors focus:border-border-focus disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-text-muted aria-invalid:border-danger-strong";
export function Input({
  variant = "default",
  className = "",
  ...props
}: ComponentProps<"input"> & { variant?: "default" | "auth" }) {
  const style = variant === "auth"
    ? `${authInputStyle} disabled:cursor-not-allowed disabled:bg-neutral-100 disabled:text-text-muted`
    : fieldClass;
  return <input {...props} className={`${style} min-h-10 ${className}`} />;
}
export function Textarea({ className = "", ...props }: ComponentProps<"textarea">) {
  return <textarea rows={4} {...props} className={`${fieldClass} resize-y ${className}`} />;
}
export function Select({ className = "", ...props }: ComponentProps<"select">) {
  return <select {...props} className={`${fieldClass} min-h-10 ${className}`} />;
}
export function Checkbox({ className = "", ...props }: Omit<ComponentProps<"input">, "type">) {
  return (
    <input
      {...props}
      type="checkbox"
      className={`size-4 rounded-sm accent-primary-700 disabled:cursor-not-allowed ${className}`}
    />
  );
}
export function Switch({
  className = "",
  ...props
}: Omit<ComponentProps<"input">, "type" | "role">) {
  return (
    <span className={`relative inline-flex h-6 w-11 shrink-0 ${className}`}>
      <input
        {...props}
        type="checkbox"
        role="switch"
        className="peer absolute inset-0 z-10 h-full w-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
      />
      <span
        aria-hidden="true"
        className="pointer-events-none h-full w-full rounded-full bg-neutral-400 transition-colors peer-checked:bg-primary-700 peer-focus-visible:shadow-focus peer-focus-visible:outline-2 peer-focus-visible:outline-offset-3 peer-focus-visible:outline-primary-700 peer-disabled:opacity-50"
      />
      <span
        aria-hidden="true"
        className="pointer-events-none absolute left-1 top-1 size-4 rounded-full bg-bg-surface shadow-sm transition-transform peer-checked:translate-x-5"
      />
    </span>
  );
}
export function FormField({
  label,
  hint,
  error,
  children,
}: {
  label: string;
  hint?: string;
  error?: string;
  children: (props: {
    id: string;
    "aria-describedby"?: string;
    "aria-invalid": boolean;
  }) => ReactNode;
}) {
  const id = useId();
  return (
    <div className="space-y-2">
      <label htmlFor={id} className="block text-body font-medium text-text-primary">
        {label}
      </label>
      {children({
        id,
        "aria-describedby": error || hint ? `${id}-message` : undefined,
        "aria-invalid": !!error,
      })}
      {(error || hint) && (
        <p
          id={`${id}-message`}
          role={error ? "alert" : undefined}
          className={`text-caption ${error ? "text-danger-strong" : "text-text-secondary"}`}
        >
          {error || hint}
        </p>
      )}
    </div>
  );
}
