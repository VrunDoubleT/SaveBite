import type { PropsWithChildren } from "react";

// Zustand does not require a Context provider. This boundary lets the app add
// SSR hydration or isolated store instances later without changing App.tsx.
export function StoreProvider({ children }: PropsWithChildren) {
  return children;
}
