import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { getPageTitle } from "./pageTitles";

export function DocumentTitle() {
  const { pathname } = useLocation();
  useEffect(() => {
    document.title = getPageTitle(pathname);
  }, [pathname]);
  return null;
}
