import { setupWorker } from "msw/browser";
import { handlers, matchedRequests, unhandledRequests } from "./handlers";
import { installMockSession } from "./auth";
import { reset } from "./db";
import { seedNavigationState } from "./navigation";
const worker = setupWorker(...handlers);
export async function startMocks() {
  installMockSession();
  seedNavigationState();
  // Deterministic browser coordinates; production geolocation is untouched.
  navigator.geolocation.getCurrentPosition = (success) => {
    queueMicrotask(() =>
      success({
        timestamp: Date.now(),
        coords: {
          latitude: 10.7769,
          longitude: 106.7009,
          accuracy: 5,
          altitude: null,
          altitudeAccuracy: null,
          heading: null,
          speed: null,
        },
      } as GeolocationPosition),
    );
  };
  await worker.start({
    onUnhandledFrame: "bypass",
    serviceWorker: { url: `${import.meta.env.BASE_URL}mockServiceWorker.js` },
  });
  Object.assign(window, {
    __savebiteMock: {
      matchedRequests,
      unhandledRequests,
      reset() {
        reset();
        sessionStorage.removeItem("savebite-mock-core-v1");
        location.reload();
      },
    },
  });
  console.info(
    "[SaveBite] Mock backend enabled. ?role=user|storeOwner|staff|admin|guest; ?mock=empty|error|slow.",
  );
}
