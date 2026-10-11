import { useEffect, useState } from "react";

/**
 * Custom hook to debounce any fast-changing value (e.g. search input).
 * @param value The value to debounce.
 * @param delay Delay in milliseconds (default: 350ms).
 * @returns The debounced value.
 */
export function useDebounce<T>(value: T, delay: number = 350): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}
