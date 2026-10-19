import { HubConnectionBuilder, HubConnectionState, HttpTransportType, LogLevel } from "@microsoft/signalr";
import { API_URL, getRealtimeAccessToken } from "@/shared/api/httpClient";
import type { RealtimeEvent } from "./realtime.types";

type Listener = (event: RealtimeEvent) => void;

// One connection carries all feature events; feature code only subscribes to event names.
export function createRealtimeClient() {
  const connection = new HubConnectionBuilder()
    .withUrl(`${API_URL.replace(/\/$/, "")}/realtime`, { accessTokenFactory: getRealtimeAccessToken, transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents })
    .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (context) => Math.min(30000, 1000 * 2 ** Math.min(context.previousRetryCount, 5)) })
    .configureLogging(LogLevel.Error)
    .build();
  const listeners = new Map<string, Set<Listener>>();
  const connected = new Set<(reconnected: boolean) => void>();
  const seen = new Set<string>();
  let stopped = false;
  let hasConnected = false;
  let retry: ReturnType<typeof setTimeout> | undefined;
  let failures = 0;
  connection.on("ReceiveEvent", (event: RealtimeEvent) => {
    if (stopped || event.version !== 1 || !event.eventId || seen.has(event.eventId)) return;
    seen.add(event.eventId);
    if (seen.size > 256) seen.delete(seen.values().next().value!);
    listeners.get(event.type)?.forEach((listener) => listener(event));
  });
  const reconcile = (reconnected: boolean) => {
    if (!stopped) connected.forEach((listener) => listener(reconnected));
  };
  connection.onreconnected(() => reconcile(true));
  async function start() {
    if (stopped || connection.state !== HubConnectionState.Disconnected) return;
    try {
      await connection.start();
      const reconnected = hasConnected || failures > 0;
      failures = 0;
      hasConnected = true;
      reconcile(reconnected);
    } catch {
      // Automatic reconnect does not retry an initial start failure.
      if (!stopped) retry = setTimeout(() => { void start(); }, Math.min(30000, 1000 * 2 ** Math.min(failures++, 5)));
    }
  }
  connection.onclose(() => { if (!stopped) void start(); });
  return {
    on(type: string, listener: Listener) {
      const handlers = listeners.get(type) ?? new Set<Listener>();
      handlers.add(listener);
      listeners.set(type, handlers);
      return () => { handlers.delete(listener); };
    },
    onConnected(listener: (reconnected: boolean) => void) {
      connected.add(listener);
      return () => { connected.delete(listener); };
    },
    start,
    stop() {
      stopped = true;
      clearTimeout(retry);
      listeners.clear();
      connected.clear();
      return connection.stop();
    },
  };
}
