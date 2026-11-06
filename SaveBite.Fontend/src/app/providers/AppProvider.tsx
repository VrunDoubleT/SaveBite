import { useEffect, type PropsWithChildren } from "react";
import { useAuthStore } from "@/shared/stores/authStore";
import { RealtimeProvider } from "./RealtimeProvider";
import { StoreProvider } from "./StoreProvider";

import { ToastViewport } from "@/shared/components/feedback/ToastViewport";
import { AccountSuspendedModal } from "@/shared/components/modal/AccountSuspendedModal";

export function AppProvider({ children }: PropsWithChildren) {
  useEffect(() => {
    const handleUnauthorized = () => useAuthStore.getState().clearSession();
    window.addEventListener("auth:unauthorized", handleUnauthorized);
    void useAuthStore.getState().initializeSession();
    return () => window.removeEventListener("auth:unauthorized", handleUnauthorized);
  }, []);

  return (
    <StoreProvider>
      <RealtimeProvider>{children}</RealtimeProvider>
      <ToastViewport />
      <AccountSuspendedModal />
    </StoreProvider>
  );
}
