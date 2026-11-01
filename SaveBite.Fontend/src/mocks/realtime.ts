import type { RealtimeEvent } from "@/shared/realtime/realtime.types";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
const id = (index: number) => `00000000-0000-4000-8000-${String(index + 1).padStart(12, "0")}`;
type Listener = (event: RealtimeEvent) => void;
const sessions = new Set<(type: string, data: unknown) => void>();
let sequence = 0;
export function emit(type: string, data: unknown) {
  sessions.forEach((session) => session(type, data));
}
export function createMockRealtimeClient() {
  const listeners = new Map<string, Set<Listener>>(),
    connected = new Set<(reconnected: boolean) => void>();
  let timer: ReturnType<typeof setInterval> | undefined;
  let started = false;
  const receive = (type: string, data: unknown) => {
    const event: RealtimeEvent = {
      eventId: `mock-event-${++sequence}`,
      type,
      version: 1,
      occurredAt: new Date().toISOString(),
      data,
    };
    listeners.get(type)?.forEach((listener) => listener(event));
  };
  return {
    on(type: string, listener: Listener) {
      const set = listeners.get(type) ?? new Set<Listener>();
      set.add(listener);
      listeners.set(type, set);
      return () => {
        set.delete(listener);
      };
    },
    onConnected(listener: (reconnected: boolean) => void) {
      connected.add(listener);
      return () => {
        connected.delete(listener);
      };
    },
    async start() {
      if (started) return;
      started = true;
      sessions.add(receive);
      connected.forEach((listener) => listener(false));
      timer = setInterval(() => {
        receive(REALTIME_EVENTS.SHOP_PENDING_ORDERS_CHANGED, { shopId: id(0) });
        receive(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, { orderId: id(0) });
      }, 30000);
    },
    async stop() {
      started = false;
      clearInterval(timer);
      sessions.delete(receive);
      listeners.clear();
      connected.clear();
    },
  };
}
