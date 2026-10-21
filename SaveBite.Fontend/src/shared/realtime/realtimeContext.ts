import { createContext, useContext } from "react";
import type { RealtimeSubscriptions } from "./realtimeSubscriptions";

export const RealtimeContext = createContext<RealtimeSubscriptions | null>(null);

export function useRealtimeSubscriptions() {
  const subscriptions = useContext(RealtimeContext);
  if (!subscriptions) throw new Error("RealtimeProvider is required.");
  return subscriptions;
}
