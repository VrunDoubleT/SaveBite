import { LogOut, ShieldAlert } from "lucide-react";
import { SharedModal } from "./BaseModal";
import { useAccountSuspensionStore } from "@/shared/stores/accountSuspensionStore";
import { APP_PATHS } from "@/app/router/paths";

export function AccountSuspendedModal() {
  const { isOpen, reason, isCustomerProfile, closeSuspensionModal } =
    useAccountSuspensionStore();

  const handleAcknowledge = () => {
    closeSuspensionModal();
    window.location.href = APP_PATHS.LOGIN;
  };

  return (
    <SharedModal
      isOpen={isOpen}
      onClose={handleAcknowledge}
      closeOnClickOutside={false}
      maxWidthClass="max-w-md"
    >
      <div className="p-2 text-center">
        <div className="mx-auto mb-4 flex size-14 items-center justify-center rounded-full bg-rose-100 text-rose-600">
          <ShieldAlert className="size-8" />
        </div>
        <h3 className="text-lg font-bold text-neutral-900">
          {isCustomerProfile ? "Customer Privileges Suspended" : "Account Suspended"}
        </h3>
        <p className="mt-2 text-sm text-neutral-600 leading-relaxed">
          {isCustomerProfile
            ? "Your customer profile privileges have been suspended by an administrator."
            : "Your account has been suspended by an administrator. You have been logged out of your session."}
        </p>

        {reason && (
          <div className="mt-4 rounded-lg border border-rose-200 bg-rose-50/70 p-3 text-left">
            <span className="block text-xs font-bold uppercase tracking-wider text-rose-800">
              Reason provided:
            </span>
            <p className="mt-1 text-xs text-rose-950 font-medium whitespace-pre-wrap">
              {reason}
            </p>
          </div>
        )}

        <div className="mt-6 flex flex-col gap-2">
          <button
            type="button"
            onClick={handleAcknowledge}
            className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-rose-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-rose-700 transition"
          >
            <LogOut className="size-4" />
            <span>Understood & Sign In</span>
          </button>
        </div>
      </div>
    </SharedModal>
  );
}
