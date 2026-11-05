import { create } from "zustand";

interface AccountSuspensionState {
  isOpen: boolean;
  reason?: string;
  isCustomerProfile?: boolean;
  showSuspensionModal: (payload?: { reason?: string; isCustomerProfile?: boolean }) => void;
  closeSuspensionModal: () => void;
}

export const useAccountSuspensionStore = create<AccountSuspensionState>((set) => ({
  isOpen: false,
  reason: undefined,
  isCustomerProfile: false,
  showSuspensionModal: (payload) =>
    set({
      isOpen: true,
      reason: payload?.reason,
      isCustomerProfile: payload?.isCustomerProfile ?? false,
    }),
  closeSuspensionModal: () =>
    set({
      isOpen: false,
      reason: undefined,
      isCustomerProfile: false,
    }),
}));
