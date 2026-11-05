import { CheckCircle2, CircleAlert, Info, X } from "lucide-react";
import { useToastStore } from "@/shared/stores/toastStore";

const toastStyles = {
  success: {
    title: "Success",
    container: "border-border-default bg-bg-surface",
    icon: "bg-primary-50 text-primary-700",
    heading: "text-text-primary",
    Icon: CheckCircle2,
  },
  error: {
    title: "Something went wrong",
    container: "border-border-default bg-bg-surface",
    icon: "bg-danger-soft text-danger-strong",
    heading: "text-text-primary",
    Icon: CircleAlert,
  },
  info: {
    title: "Just so you know",
    container: "border-border-default bg-bg-surface",
    icon: "bg-info-soft text-info-strong",
    heading: "text-text-primary",
    Icon: Info,
  },
};

export function ToastViewport() {
  const toasts = useToastStore((state) => state.toasts);
  const removeToast = useToastStore((state) => state.removeToast);
  return (
    <section
      aria-label="Notifications"
      className="pointer-events-none fixed right-4 top-[max(5rem,env(safe-area-inset-top))] z-[100] flex max-h-[calc(100dvh-6rem)] w-[calc(100%-2rem)] max-w-[340px] flex-col gap-2 overflow-y-auto overscroll-contain sm:right-6"
    >
      {toasts.map((toast) => {
        const style = toastStyles[toast.type];
        const Icon = style.Icon;
        return (
          <div
            key={toast.id}
            role={toast.type === "error" ? "alert" : "status"}
            aria-atomic="true"
            className={`pointer-events-auto grid shrink-0 grid-cols-[28px_minmax(0,1fr)_28px] items-start gap-2.5 rounded-lg border p-3 shadow-md ${style.container}`}
          >
            <span className={`flex size-7 items-center justify-center rounded-md ${style.icon}`}>
              <Icon className="size-4" aria-hidden="true" />
            </span>
            <div className="min-w-0 space-y-0.5">
              <p className={`text-sm font-semibold leading-5 ${style.heading}`}>{style.title}</p>
              <p className="whitespace-pre-line text-sm leading-5 text-text-primary [overflow-wrap:anywhere]">
                {toast.message}
              </p>
            </div>
            <button
              type="button"
              onClick={() => removeToast(toast.id)}
              className="flex size-7 items-center justify-center rounded-md text-text-secondary transition-colors hover:bg-neutral-200 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
              aria-label="Dismiss notification"
            >
              <X className="size-4" aria-hidden="true" />
            </button>
          </div>
        );
      })}
    </section>
  );
}
