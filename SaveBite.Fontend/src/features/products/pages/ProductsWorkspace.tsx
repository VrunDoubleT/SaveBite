import { useCallback } from "react";
import { Outlet, useLocation } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { productApi } from "../api/productApi";
import { useProductResource } from "../hooks/useProductResource";
import { Empty, ErrorState, Loading } from "../components/ProductUi";
import { ProductListSkeleton } from "../components/ProductListSkeleton";
import type { ProductWorkspaceContext } from "../hooks/useProductWorkspace";

export function ProductsWorkspace() {
  const { pathname } = useLocation();
  const load = useCallback((signal: AbortSignal) => productApi.options(signal), []);
  const { data, error, loading, reload } = useProductResource("product-options", load);
  if (loading)
    return pathname.replace(/\/$/, "") === APP_PATHS.STORE_OWNER_PRODUCTS ? (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold tracking-tight text-gray-900">Product management</h1>
        <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
          <ProductListSkeleton />
        </div>
      </div>
    ) : (
      <Loading />
    );
  if (error || !data)
    return <ErrorState error={error ?? "Unable to load shop information."} retry={reload} />;
  const shop = data.shop;
  if (!shop)
    return (
      <Empty
        title="No shop available"
        description="Products will appear here once your shop is activated."
      />
    );
  const context: ProductWorkspaceContext = {
    shop,
    categories: data.categories,
    productPath: (suffix = "") => `${APP_PATHS.STORE_OWNER_PRODUCTS}${suffix}`,
  };
  return (
    <div className="space-y-6">
      <Outlet key={shop.id} context={context} />
    </div>
  );
}
