import { useEffect, type PropsWithChildren } from "react";
import { useAuthStore } from "@/shared/stores/authStore";
import { StoreProvider } from "./StoreProvider";

import { ToastViewport } from "@/shared/components/feedback/ToastViewport";
export function AppProvider({ children }: PropsWithChildren) {
  useEffect(() => {
    const handleUnauthorized = () => useAuthStore.getState().clearSession();
    window.addEventListener("auth:unauthorized", handleUnauthorized);
    void useAuthStore.getState().initializeSession();
    return () => window.removeEventListener("auth:unauthorized", handleUnauthorized);
  }, []);

  return <StoreProvider>{children}
      <ToastViewport />
      </StoreProvider>;
}
