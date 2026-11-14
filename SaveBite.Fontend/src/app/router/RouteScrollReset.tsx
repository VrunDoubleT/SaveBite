import { useLayoutEffect } from "react";
import { useLocation } from "react-router-dom";

/** A new page starts at the top; filtering or refreshing the current page keeps its position. */
export function RouteScrollReset() {
  const { pathname } = useLocation();

  useLayoutEffect(() => {
    const previous = window.history.scrollRestoration;
    window.history.scrollRestoration = "manual";
    return () => {
      window.history.scrollRestoration = previous;
    };
  }, []);

  useLayoutEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: "instant" });
  }, [pathname]);

  return null;
}
