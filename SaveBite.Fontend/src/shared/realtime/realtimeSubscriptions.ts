import type { createRealtimeClient } from "./realtimeClient";

export type RealtimeSubscriptions = Pick<ReturnType<typeof createRealtimeClient>, "on" | "onConnected">;

// Let feature listeners mount independently of the session connection, including StrictMode restarts.
export function createRealtimeSubscriptions() {
  let source: RealtimeSubscriptions | undefined;
  const listeners = new Set<{ bind: (client: RealtimeSubscriptions) => () => void; off?: () => void }>();
  function subscribe(bind: (client: RealtimeSubscriptions) => () => void) {
    const listener = { bind, off: source ? bind(source) : undefined };
    listeners.add(listener);
    return () => { listener.off?.(); listeners.delete(listener); };
  }
  return {
    on: (...args: Parameters<RealtimeSubscriptions["on"]>) => subscribe((client) => client.on(...args)),
    onConnected: (...args: Parameters<RealtimeSubscriptions["onConnected"]>) => subscribe((client) => client.onConnected(...args)),
    attach(client: RealtimeSubscriptions) {
      source = client;
      listeners.forEach((listener) => { listener.off?.(); listener.off = listener.bind(client); });
      return () => {
        if (source !== client) return;
        source = undefined;
        listeners.forEach((listener) => { listener.off?.(); listener.off = undefined; });
      };
    },
  };
}
