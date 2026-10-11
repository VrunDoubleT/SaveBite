import { useCallback, useEffect, useRef, useState } from "react";
import {
  geolocationErrorMessage,
  parseCoordinates,
  reverseGeocodeVietmap,
  type AddressCoordinates,
  type ResolvedAddress,
} from "@/features/addresses/utils/addressLocation";

export function useAddressLocation(
  onPoint: (point: AddressCoordinates) => void,
  onAddress: (address: ResolvedAddress) => void,
) {
  const [phase, setPhase] = useState<"idle" | "locating" | "resolving">("idle");
  const [error, setError] = useState<string | null>(null);
  const [accuracy, setAccuracy] = useState<number | null>(null);
  const requestId = useRef(0);
  const active = useRef(true);
  const controller = useRef<AbortController | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const cancelPending = useCallback(() => {
    requestId.current += 1;
    controller.current?.abort();
    if (timer.current !== null) clearTimeout(timer.current);
  }, []);

  useEffect(() => {
    active.current = true;
    return () => {
      active.current = false;
      cancelPending();
    };
  }, [cancelPending]);

  const resolvePoint = useCallback(
    (point: AddressCoordinates, delay: number) => {
      cancelPending();
      const id = requestId.current;
      onPoint(point);
      setError(null);
      setPhase("resolving");
      timer.current = setTimeout(async () => {
        const abort = new AbortController();
        controller.current = abort;
        const timeout = setTimeout(() => abort.abort(), 12000);
        try {
          const result = await reverseGeocodeVietmap(point, abort.signal);
          if (active.current && id === requestId.current) onAddress(result);
        } catch (cause) {
          if (active.current && id === requestId.current) {
            setError(
              abort.signal.aborted
                ? "Address lookup timed out. Your coordinates are kept; enter the address manually or try again."
                : cause instanceof Error
                  ? cause.message
                  : "Unable to look up the address. Enter it manually.",
            );
          }
        } finally {
          clearTimeout(timeout);
          if (active.current && id === requestId.current) setPhase("idle");
        }
      }, delay);
    },
    [cancelPending, onPoint, onAddress],
  );

  const selectPoint = useCallback(
    (point: AddressCoordinates) => {
      if (!parseCoordinates(String(point.latitude), String(point.longitude))) return;
      setAccuracy(null);
      resolvePoint(point, 350);
    },
    [resolvePoint],
  );

  const locate = useCallback(() => {
    cancelPending();
    setError(null);
    if (!window.isSecureContext || !navigator.geolocation) {
      setPhase("idle");
      setError(
        "Location access requires HTTPS or localhost and a supported browser. Choose a point on the map instead.",
      );
      return;
    }
    const id = requestId.current;
    setPhase("locating");
    navigator.geolocation.getCurrentPosition(
      ({ coords }) => {
        if (!active.current || id !== requestId.current) return;
        const point = parseCoordinates(String(coords.latitude), String(coords.longitude));
        if (!point) {
          setPhase("idle");
          setError("The browser returned invalid coordinates. Choose a point on the map instead.");
          return;
        }
        setAccuracy(Number.isFinite(coords.accuracy) ? coords.accuracy : null);
        resolvePoint(point, 0);
      },
      (cause) => {
        if (!active.current || id !== requestId.current) return;
        setPhase("idle");
        setError(geolocationErrorMessage(cause.code));
      },
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 },
    );
  }, [cancelPending, resolvePoint]);

  return { phase, error, accuracy, locate, selectPoint };
}
