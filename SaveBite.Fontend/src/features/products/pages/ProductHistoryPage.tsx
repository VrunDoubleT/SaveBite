import { Link, useLocation, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { ProductDealsPanel } from "../components/ProductDealsPanel";
import { Alert } from "../components/ProductUi";
import { useProductWorkspace } from "../hooks/useProductWorkspace";

export function ProductHistoryPage() {
  const { productPath } = useProductWorkspace();
  const { productId = "" } = useParams();
  const location = useLocation();
  return (
    <div className="space-y-6">
      <Link
        to={productPath()}
        className="inline-flex items-center gap-2 text-sm text-neutral-500 hover:text-primary-700"
      >
        <ArrowLeft className="size-4" />
        Product list
      </Link>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Flash deal history</h1>
        {typeof location.state?.productName === "string" && (
          <p className="mt-2 text-neutral-500">{location.state.productName}</p>
        )}
      </div>
      {typeof location.state?.message === "string" && (
        <Alert success>{location.state.message}</Alert>
      )}
      <ProductDealsPanel productId={productId} />
    </div>
  );
}
