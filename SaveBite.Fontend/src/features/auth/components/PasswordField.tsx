import { forwardRef, useId, useState, type InputHTMLAttributes } from "react";
import { authInputClass, FieldError } from "@/features/auth/components/AuthFormCard";

interface PasswordFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label: string;
  error?: string;
}

export const PasswordField = forwardRef<HTMLInputElement, PasswordFieldProps>(
  ({ label, error, id, className = "", ...inputProps }, ref) => {
    const generatedId = useId();
    const inputId = id ?? generatedId;
    const [isVisible, setIsVisible] = useState(false);

    return (
      <div>
        <label className="block text-sm font-semibold text-text-primary" htmlFor={inputId}>
          {label}
        </label>
        <div className="relative">
          <input
            {...inputProps}
            id={inputId}
            ref={ref}
            type={isVisible ? "text" : "password"}
            className={`${authInputClass} pr-12 ${className}`}
          />
          <button
            type="button"
            aria-label={isVisible ? `Hide ${label.toLowerCase()}` : `Show ${label.toLowerCase()}`}
            aria-pressed={isVisible}
            onClick={() => setIsVisible((visible) => !visible)}
            className="absolute right-2 top-[calc(50%+0.25rem)] grid size-9 -translate-y-1/2 place-items-center rounded-md text-text-muted transition hover:bg-neutral-100 hover:text-primary-600 focus-visible:outline-none focus-visible:shadow-focus"
          >
            {isVisible ? <EyeOffIcon /> : <EyeIcon />}
          </button>
        </div>
        <FieldError message={error} />
      </div>
    );
  },
);

PasswordField.displayName = "PasswordField";

function EyeIcon() {
  return (
    <svg
      className="size-5"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

function EyeOffIcon() {
  return (
    <svg
      className="size-5"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="m3 3 18 18" />
      <path d="M10.6 5.2A11.5 11.5 0 0 1 12 5c6.5 0 10 7 10 7a17.6 17.6 0 0 1-2.1 3.1" />
      <path d="M6.6 6.6C3.7 8.5 2 12 2 12s3.5 7 10 7a9.8 9.8 0 0 0 4.1-.9" />
      <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2" />
    </svg>
  );
}
