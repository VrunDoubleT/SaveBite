import { createContext, useContext } from "react";
export interface RestockWorkspace {
  shopId: string;
  pendingCount: number | null;
  revision: number;
  error: string;
  refresh: () => void;
}
export const RestockContext = createContext<RestockWorkspace | null>(null);
export function useRestockWorkspace() {
  const value = useContext(RestockContext);
  if (!value) throw new Error("Restock workspace is required");
  return value;
}
