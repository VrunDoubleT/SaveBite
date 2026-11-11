import { ShieldAlert } from "lucide-react";
import { useAuthStore } from "@/shared/stores/authStore";

export function CustomerSuspensionBanner() {
  const suspended = useAuthStore(
    (state) => state.isAuthenticated && state.user?.customerStatus === "suspended",
  );
  if (!suspended) return null;
  return (
    <div
      role="alert"
      className="flex items-start gap-3 rounded-xl border border-rose-200 bg-rose-50/80 p-4 text-rose-900 shadow-2xs"
    >
      <span
        aria-hidden="true"
        className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-rose-100 text-rose-700"
      >
        <ShieldAlert className="size-5" />
      </span>
      <div className="min-w-0 text-sm">
        <h2 className="font-bold">Your customer features have been suspended</h2>
        <p className="mt-1 leading-normal text-rose-800">
          An administrator has restricted your customer access. Customer actions are currently
          unavailable. You can still access your account.
        </p>
      </div>
    </div>
  );
}
