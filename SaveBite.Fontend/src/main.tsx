import React from "react";
import ReactDOM from "react-dom/client";

import "./index.css";

// Retire the old preview worker on existing installations before starting real API requests.
async function retirePreviewWorker(): Promise<boolean> {
  if (!("serviceWorker" in navigator)) return false;
  const workerUrl = new URL(
    "mockServiceWorker.js",
    new URL(import.meta.env.BASE_URL, window.location.origin),
  ).href;
  const isPreviewWorker = (worker?: ServiceWorker | null) => worker?.scriptURL === workerUrl;
  const registrations = await navigator.serviceWorker.getRegistrations();
  for (const registration of registrations) {
    if (
      isPreviewWorker(registration.active) ||
      isPreviewWorker(registration.waiting) ||
      isPreviewWorker(registration.installing)
    ) {
      await registration.unregister();
    }
  }
  if (isPreviewWorker(navigator.serviceWorker.controller)) {
    window.location.reload();
    return true;
  }
  return false;
}

async function bootstrap() {
  if (await retirePreviewWorker()) return;
  const { default: App } = await import("./App");
  ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
  );
}
void bootstrap();
