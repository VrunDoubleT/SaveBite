import React from "react";
import ReactDOM from "react-dom/client";

import "./index.css";

async function bootstrap() {
  if (import.meta.env.DEV && import.meta.env.VITE_USE_MOCK === "true") {
    const { startMocks } = await import("./mocks");
    await startMocks();
  }
  const { default: App } = await import("./App");
  ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
  );
}
void bootstrap();
