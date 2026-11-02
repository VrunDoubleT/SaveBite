import type { PropsWithChildren, ReactNode } from "react";
import { authInputStyle } from "@/shared/components/ui/inputStyles";

interface AuthFormCardProps extends PropsWithChildren {
  eyebrow: string;
  title: string;
  description: string;
  footer?: ReactNode;
}

export function AuthFormCard({ eyebrow, title, description, footer, children }: AuthFormCardProps) {
  return (
    <section className="mx-auto grid min-h-[calc(100vh-10rem)] w-full max-w-md place-items-center py-10">
      <div className="w-full rounded-2xl border border-border-default bg-bg-surface p-6 shadow-md sm:p-8">
        <p className="text-sm font-semibold text-primary-600">{eyebrow}</p>
        <h1 className="mt-2 text-3xl font-black text-text-primary">{title}</h1>
        <p className="mt-2 text-sm leading-relaxed text-text-secondary">{description}</p>
        {children}
        {footer && <div className="mt-6 text-center text-sm text-text-secondary">{footer}</div>}
      </div>
    </section>
  );
}

export const authInputClass = `mt-2 ${authInputStyle}`;

export const authSubmitClass =
  "w-full rounded-md bg-primary-600 px-5 py-3 font-bold text-white transition hover:bg-primary-700 focus-visible:outline-none focus-visible:shadow-focus disabled:cursor-not-allowed disabled:opacity-60";

export function FieldError({ message }: { message?: string }) {
  if (!message) return null;
  return (
    <p role="alert" className="mt-1.5 text-xs font-medium text-danger">
      {message}
    </p>
  );
}

export function FormAlert({
  children,
  tone = "error",
}: PropsWithChildren<{ tone?: "error" | "info" }>) {
  const className = tone === "error" ? "bg-red-50 text-danger" : "bg-primary-50 text-primary-700";

  return (
    <p
      role={tone === "error" ? "alert" : "status"}
      className={`rounded-md px-4 py-3 text-sm font-medium ${className}`}
    >
      {children}
    </p>
  );
}
