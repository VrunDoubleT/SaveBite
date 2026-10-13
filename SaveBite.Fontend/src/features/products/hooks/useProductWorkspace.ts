import { useOutletContext } from "react-router-dom";
import type { Option } from "../types/product.types";
export interface ProductWorkspaceContext {
  shop: Option;
  categories: Option[];
  productPath: (suffix?: string) => string;
}
export const useProductWorkspace = () => useOutletContext<ProductWorkspaceContext>();
