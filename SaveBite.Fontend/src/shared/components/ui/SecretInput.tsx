import { useState, type ComponentProps } from "react";
import { Eye, EyeOff } from "lucide-react";

type SecretInputProps = Omit<ComponentProps<"input">, "type"> & { secretLabel: string };

export function SecretInput({ secretLabel, className = "", disabled, ...props }: SecretInputProps) {
  const [visible, setVisible] = useState(false);
  return (
    <span className="relative block">
      <input {...props} disabled={disabled} type={visible ? "text" : "password"}
        className={`${className} pr-12`} />
      <button type="button" disabled={disabled} onClick={() => setVisible((value) => !value)}
        aria-label={`${visible ? "Hide" : "Show"} ${secretLabel}`} aria-pressed={visible}
        className="absolute inset-y-0 right-0 inline-flex min-h-11 w-11 items-center justify-center rounded-r-lg text-slate-500 hover:text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:opacity-50">
        {visible ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}
      </button>
    </span>
  );
}
