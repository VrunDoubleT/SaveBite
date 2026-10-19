import { create } from "zustand";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";
import { cartApi } from "../api/cartApi";
import type { Cart, CartPage, CartShop, CartMutationResponse } from "../types/cart.types";

interface CartState {
  cart: Cart | null;
  cartPageActive: boolean;
  setPageActive: (active: boolean) => void;
  queueRealtimeRefresh: () => void;
  onRealtimeConnected: (reconnected: boolean) => void;
  totalFlashDeals: number | null;
  hasMore: boolean;
  nextCursor: string | null;
  isLoadingMore: boolean;
  refreshPages: () => Promise<void>;
  loadMore: () => Promise<void>;
  refreshSummary: () => Promise<void>;
  isLoading: boolean;
  isMutating: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  addItem: (variantId: string, quantity: number) => Promise<void>;
  updateQuantity: (itemId: string, quantity: number) => Promise<void>;
  removeItem: (itemId: string) => Promise<void>;
}

// Ignore responses from previous sessions and older reads, especially during logout.
let revision = 0;

export const useCartStore = create<CartState>((set, get) => {
  let pageRead: { revision: number; task: Promise<void> } | null = null;
  let realtimePending = false;
  let realtimeReading = false;
  async function flushRealtime() {
    if (realtimeReading || get().isMutating || get().isLoadingMore || get().isLoading) return;
    realtimeReading = true;
    try {
      while (realtimePending && useAuthStore.getState().isAuthenticated) {
        if (get().isMutating || get().isLoadingMore || get().isLoading) break;
        realtimePending = false;
        await get().refreshPages();
      }
    } finally {
      realtimeReading = false;
    }
  }

  async function readPages(target: number) {
    let cursor: string | null = null;
    let shops: CartShop[] = [];
    let response: CartPage;
    do {
      response = await cartApi.getPage(cursor);
      shops = [
        ...new Map([...shops, ...response.page.items].map((shop) => [shop.shopId, shop])).values(),
      ];
      if (
        !response.page.hasMore ||
        !response.page.nextCursor ||
        response.page.nextCursor === cursor
      )
        break;
      cursor = response.page.nextCursor;
    } while (shops.length < target);
    return {
      cart: {
        shops,
        totalQuantity: shops.reduce((n, shop) => n + shop.totalQuantity, 0),
      },
      hasMore: response.page.hasMore,
      nextCursor: response.page.nextCursor,
      totalFlashDeals: response.totalFlashDeals,
    };
  }
  
  async function mutate(action: () => Promise<Cart | CartMutationResponse>) {
    if (get().isMutating) throw new Error("Please wait for your cart to finish updating.");
    if (!useAuthStore.getState().isAuthenticated)
      throw new Error("Please sign in to add items to your cart.");
    const current = ++revision;
    set({ isMutating: true, isLoading: false, isLoadingMore: false });
    try {
      const result = await action();
      if (current !== revision) return;
      if ("summary" in result) {
        // Patch only the loaded shop; null removes its group after deleting its final item.
        const cart = get().cart;
        const shops = cart?.shops.flatMap((shop) => shop.shopId === result.shopId
          ? result.shop ? [result.shop] : [] : [shop]);
        set({
          cart: cart && shops ? { shops, totalQuantity: shops.reduce((sum, shop) => sum + shop.totalQuantity, 0) } : cart,
          totalFlashDeals: result.summary.totalFlashDeals,
          error: null,
        });
      } else {
        set({ cart: result, totalFlashDeals: new Set(result.shops.flatMap(shop => shop.items.map(item => item.flashDealId))).size, error: null });
      }
    } finally {
      if (current === revision) set({ isMutating: false });
      // SignalR can arrive before the HTTP response; reconcile only after applying that response.
      void flushRealtime();
    }
  }

  return {
    cartPageActive: false,
    setPageActive: (active) => set({ cartPageActive: active }),
    queueRealtimeRefresh: () => { realtimePending = true; void flushRealtime(); },
    onRealtimeConnected: (reconnected) => {
      // The cart page owns its initial read; an initial socket connection must not fetch it again.
      if (!reconnected && (get().cartPageActive || get().totalFlashDeals !== null)) return;
      if (reconnected) get().queueRealtimeRefresh();
      else void get().refreshSummary();
    },
    cart: null,
    totalFlashDeals: null,
    hasMore: false,
    nextCursor: null,
    isLoadingMore: false,
    refreshSummary: async () => {
      if (!useAuthStore.getState().isAuthenticated || get().isMutating) return;
      const current = revision;
      try {
        const summary = await cartApi.summary();
        if (current === revision) set({ totalFlashDeals: summary.totalFlashDeals });
      } catch {
        /* A summary failure must not discard the visible cart. */
      }
    },
    refreshPages: async () => {
      if (!useAuthStore.getState().isAuthenticated || get().isMutating || get().isLoadingMore)
        return;
      // StrictMode remounts and concurrent callers share the current session's in-flight read.
      if (pageRead?.revision === revision) return pageRead.task;
      const current = ++revision;
      const target = Math.max(5, get().cart?.shops.length ?? 0);
      set({ isLoading: true });
      const task = (async () => {
        try {
          const page = await readPages(target);
          if (current === revision) set({ ...page, error: null });
        } catch (error) {
          if (current === revision) set({ error: getApiErrorMessage(error) });
        } finally {
          if (pageRead?.revision === current) pageRead = null;
          if (current === revision) set({ isLoading: false });
          // A real change received during this read needs a new snapshot after it completes.
          void flushRealtime();
        }
      })();
      pageRead = { revision: current, task };
      return task;
    },
    loadMore: async () => {
      const state = get();
      if (
        !useAuthStore.getState().isAuthenticated ||
        !state.hasMore ||
        !state.nextCursor ||
        state.isLoading ||
        state.isLoadingMore ||
        state.isMutating
      )
        return;
      const current = ++revision;
      set({ isLoadingMore: true, error: null });
      try {
        const response = await cartApi.getPage(state.nextCursor);
        if (current !== revision) return;
        const shops = [
          ...new Map(
            [...(get().cart?.shops ?? []), ...response.page.items].map((shop) => [
              shop.shopId,
              shop,
            ]),
          ).values(),
        ];
        set({
          cart: {
            shops,
            totalQuantity: shops.reduce((n, shop) => n + shop.totalQuantity, 0),
          },
          nextCursor: response.page.nextCursor,
          hasMore: response.page.hasMore,
          totalFlashDeals: response.totalFlashDeals,
        });
      } catch (error) {
        if (current === revision) set({ error: getApiErrorMessage(error) });
      } finally {
        if (current === revision) set({ isLoadingMore: false });
        void flushRealtime();
      }
    },
    isLoading: false,
    isMutating: false,
    error: null,
    refresh: () => get().refreshPages(),
    addItem: (id, quantity) => mutate(() => cartApi.addItem(id, quantity)),
    updateQuantity: (id, quantity) => mutate(() => cartApi.updateQuantity(id, quantity)),
    removeItem: (id) => mutate(() => cartApi.removeItem(id)),
  };
});

useAuthStore.subscribe((state, previous) => {
  if (state.user?.id !== previous.user?.id || state.isAuthenticated !== previous.isAuthenticated) {
    revision++;
    useCartStore.setState({
      cart: null,
      totalFlashDeals: null,
      hasMore: false,
      nextCursor: null,
      isLoadingMore: false,
      error: null,
      isLoading: false,
      isMutating: false,
    });
  }
});
