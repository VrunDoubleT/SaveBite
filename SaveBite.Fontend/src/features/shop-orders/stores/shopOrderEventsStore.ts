import { create } from "zustand";

interface ShopOrderEventsState {
  revisions: Record<string, number>;
  orderRevisions: Record<string, number>;
  reconnectRevision: number;
  changed: (shopId: string, orderId?: string) => void;
  reconnected: () => void;
  reset: () => void;
}

// Invalidation is scoped by shop; order data remains owned by the active page and its page/filter.
export const useShopOrderEventsStore = create<ShopOrderEventsState>((set) => ({
  revisions: {},
  orderRevisions: {},
  reconnectRevision: 0,
  changed: (shopId, orderId) => set((state) => ({
    revisions: { ...state.revisions, [shopId]: (state.revisions[shopId] ?? 0) + 1 },
    orderRevisions: orderId ? { ...state.orderRevisions, [`${shopId}:${orderId}`]: (state.orderRevisions[`${shopId}:${orderId}`] ?? 0) + 1 } : state.orderRevisions,
  })),
  reconnected: () => set((state) => ({ reconnectRevision: state.reconnectRevision + 1 })),
  reset: () => set({ revisions: {}, orderRevisions: {}, reconnectRevision: 0 }),
}));
