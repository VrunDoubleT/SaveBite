import {
  CheckCircle2,
  CircleAlert,
  Info,
  X,
} from "lucide-react";

import {
  type ToastType,
  useToastStore,
} from "@/shared/stores/toastStore";

const toastStyles: Record<
  ToastType,
  {
    container: string;
    icon: string;
  }
> = {
  success: {
    container: "border-emerald-200 bg-white",
    icon: "text-emerald-600",
  },

  error: {
    container: "border-red-200 bg-white",
    icon: "text-red-600",
  },

  info: {
    container: "border-blue-200 bg-white",
    icon: "text-blue-600",
  },
};

function ToastIcon({
  type,
}: {
  type: ToastType;
}) {
  const className =
    `mt-0.5 shrink-0 ${toastStyles[type].icon}`;

  if (type === "success") {
    return (
      <CheckCircle2
        size={20}
        className={className}
      />
    );
  }

  if (type === "error") {
    return (
      <CircleAlert
        size={20}
        className={className}
      />
    );
  }

  return (
    <Info
      size={20}
      className={className}
    />
  );
}

export function ToastViewport() {
  const toasts = useToastStore(
    (state) => state.toasts,
  );

  const removeToast = useToastStore(
    (state) => state.removeToast,
  );

  return (
    <div
      className="
        pointer-events-none
        fixed
        right-4
        top-4
        z-[100]
        flex
        w-[calc(100%-2rem)]
        max-w-sm
        flex-col
        gap-3
        sm:right-6
        sm:top-6
      "
      aria-live="polite"
      aria-atomic="true"
    >
      {toasts.map((toast) => (
        <div
          key={toast.id}
          role={
            toast.type === "error"
              ? "alert"
              : "status"
          }
          className={`
            pointer-events-auto
            flex
            items-start
            gap-3
            rounded-2xl
            border
            px-4
            py-3.5
            shadow-lg
            ${toastStyles[toast.type].container}
          `}
        >
          <ToastIcon type={toast.type} />

          <p
            className="
              min-w-0
              flex-1
              text-sm
              font-medium
              leading-5
              text-gray-800
            "
          >
            {toast.message}
          </p>

          <button
            type="button"
            onClick={() =>
              removeToast(toast.id)
            }
            className="
              shrink-0
              rounded-lg
              p-1
              text-gray-400
              transition
              hover:bg-gray-100
              hover:text-gray-700
            "
            aria-label="Dismiss notification"
          >
            <X size={16} />
          </button>
        </div>
      ))}
    </div>
  );
}