import { useCallback, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Images, Info, Layers3, Trash2, Zap } from "lucide-react";
import { productApi } from "../api/productApi";
import { useProductResource } from "../hooks/useProductResource";
import { useProductWorkspace } from "../hooks/useProductWorkspace";
import {
  Alert,
  ErrorState,
  Loading,
  Modal,
  ProductPhoto,
  secondaryButton,
  StatusBadge,
} from "../components/ProductUi";
import { ProductInfoEditor } from "../components/ProductInfoEditor";
import { ProductImagesPanel } from "../components/ProductImagesPanel";
import { ProductVariantsPanel } from "../components/ProductVariantsPanel";
import { ProductDealsPanel } from "../components/ProductDealsPanel";
import { dateTime, describeError } from "../utils/productForm";
import type { Product } from "../types/product.types";
import type { ProductWorkspaceContext } from "../hooks/useProductWorkspace";

const tabs = [
  { id: "info", label: "Information", icon: Info },
  { id: "images", label: "Images", icon: Images },
  { id: "variants", label: "Variants", icon: Layers3 },
  { id: "deals", label: "Flash deal history", icon: Zap },
] as const;
export function ProductDetailPage() {
  const { productId = "" } = useParams();
  const workspace = useProductWorkspace();
  return (
    <Detail key={`${workspace.shop.id}:${productId}`} productId={productId} workspace={workspace} />
  );
}
function Detail({
  productId,
  workspace,
}: {
  productId: string;
  workspace: ProductWorkspaceContext;
}) {
  const { shop, categories, productPath } = workspace;
  const location = useLocation();
  const navigate = useNavigate();
  const load = useCallback((signal: AbortSignal) => productApi.get(productId, signal), [productId]);
  const {
    data: product,
    error,
    loading,
    reload,
    setData,
  } = useProductResource(`${shop.id}:${productId}`, load);
  const [tab, setTab] = useState<string>("info");
  const [notice, setNotice] = useState<string>(
    typeof location.state?.message === "string" ? location.state.message : "",
  );
  const [deleting, setDeleting] = useState(false),
    [busy, setBusy] = useState(false),
    [deleteError, setDeleteError] = useState("");
  const saved = (result: Product) => {
    setData(result);
    setNotice("Product changes saved.");
  };
  if (loading) return <Loading />;
  if (error || !product)
    return (
      <div>
        <Link to={productPath()} className="text-sm text-primary-700">
          ← Product list
        </Link>
        <ErrorState error={error ?? "Product not found."} retry={reload} />
        <Link className={secondaryButton} to={productPath(`/${productId}/flash-deals`)}>
          View flash deal history
        </Link>
      </div>
    );
  return (
    <div className="space-y-6">
      <Link
        to={productPath()}
        className="inline-flex items-center gap-2 text-sm text-neutral-500 hover:text-primary-700"
      >
        <ArrowLeft className="size-4" />
        Product list
      </Link>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex min-w-0 items-center gap-4">
          <ProductPhoto src={product.images[0]?.url} alt={product.name} className="size-20" />
          <div className="min-w-0">
            <p className="mb-1 text-xs font-medium text-primary-700">{product.categoryName}</p>
            <h1 className="break-words text-2xl font-bold tracking-tight md:text-3xl">
              {product.name}
            </h1>
            <div className="mt-2 flex flex-wrap items-center gap-3">
              <StatusBadge hidden={product.status === 1} />
              <span className="text-xs text-neutral-500">
                {product.variants.length} variants · Updated {dateTime(product.updatedAt)}
              </span>
            </div>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            disabled={busy}
            className={secondaryButton}
            onClick={async () => {
              if (busy) return;
              const newStatus = product.status === 0 ? 1 : 0;
              setBusy(true);
              try {
                const res = await productApi.updateStatus(product.id, newStatus);
                saved(res);
                setNotice(`Product status updated to ${newStatus === 0 ? "Active" : "Hidden"}.`);
              } catch (err) {
                setNotice(describeError(err));
              } finally {
                setBusy(false);
              }
            }}
          >
            {product.status === 0 ? "Hide product" : "Activate product"}
          </button>
          <button
            type="button"
            className={`${secondaryButton} border-red-200! text-red-600! hover:bg-red-50!`}
            onClick={() => {
              setDeleting(true);
              setDeleteError("");
            }}
          >
            <Trash2 className="size-4" />
            Delete product
          </button>
        </div>
      </div>
      {notice && <Alert success>{notice}</Alert>}
      {product.activeWarning && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          <p className="font-semibold">
            {product.activeWarning.dealBlocked
              ? "Opening deals is blocked pending correction approval"
              : "Administrator requested corrections"}
          </p>
          <p className="mt-1 whitespace-pre-wrap">{product.activeWarning.requiredCorrection}</p>
          <p className="mt-2">
            Edit deadline: {new Date(product.activeWarning.dueAt).toLocaleString("vi-VN")}
          </p>
          {product.activeWarning.firstEditedAt && (
            <p>Corrections submitted. Further edits remain part of this warning until approved.</p>
          )}
        </div>
      )}
      {!product.activeWarning &&
        product.latestReview &&
        [3, 4].includes(product.latestReview.status) && (
          <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
            <p className="font-semibold">
              {product.latestReview.status === 4
                ? "Product flagged for review"
                : "Content needs changes"}
            </p>
            <p className="mt-1">
              {product.latestReview.note ??
                "Please review the product information as requested by the administrator."}
            </p>
          </div>
        )}
      <div
        role="tablist"
        aria-label="Product management"
        className="flex gap-1 overflow-x-auto border-b border-neutral-200"
      >
        {tabs.map((item, index) => (
          <button
            type="button"
            key={item.id}
            role="tab"
            id={`tab-${item.id}`}
            aria-controls={`panel-${item.id}`}
            aria-selected={tab === item.id}
            tabIndex={tab === item.id ? 0 : -1}
            onClick={() => setTab(item.id)}
            onKeyDown={(e) => {
              if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(e.key)) return;
              e.preventDefault();
              const next =
                e.key === "Home"
                  ? 0
                  : e.key === "End"
                    ? tabs.length - 1
                    : (index + (e.key === "ArrowRight" ? 1 : -1) + tabs.length) % tabs.length;
              setTab(tabs[next].id);
              document.getElementById(`tab-${tabs[next].id}`)?.focus();
            }}
            className={`flex shrink-0 items-center gap-2 border-b-2 px-4 py-3 text-sm font-medium outline-none transition focus-visible:bg-primary-50 ${tab === item.id ? "border-primary-600 text-primary-700" : "border-transparent text-neutral-500 hover:text-neutral-800"}`}
          >
            <item.icon className="size-4" />
            {item.label}
          </button>
        ))}
      </div>
      <div
        role="tabpanel"
        id={`panel-${tab}`}
        aria-labelledby={`tab-${tab}`}
        tabIndex={0}
        className="outline-none focus-visible:ring-2 focus-visible:ring-primary-200"
      >
        {tab === "info" && (
          <ProductInfoEditor product={product} categories={categories} onSaved={saved} />
        )}
        {tab === "images" && <ProductImagesPanel product={product} onSaved={saved} />}
        {tab === "variants" && <ProductVariantsPanel product={product} onSaved={saved} />}
        {tab === "deals" && (
          <>
            <ProductDealsPanel productId={product.id} />
            <Link
              className="mt-4 inline-block text-sm font-medium text-primary-700"
              to={productPath(`/${product.id}/flash-deals`)}
            >
              Open history page →
            </Link>
          </>
        )}
      </div>
      {deleting && (
        <Modal title="Delete product?" busy={busy} onClose={() => setDeleting(false)}>
          <p className="text-sm leading-6 text-neutral-600">
            Product <strong className="text-neutral-900">{product.name}</strong> will be removed
            from your product list. Flash deal history is preserved and existing sales are not
            cancelled.
          </p>
          {deleteError && (
            <div className="mt-4">
              <Alert>{deleteError}</Alert>
            </div>
          )}
          <div className="mt-6 flex justify-end gap-3">
            <button disabled={busy} className={secondaryButton} onClick={() => setDeleting(false)}>
              Keep product
            </button>
            <button
              disabled={busy}
              className="inline-flex items-center gap-2 rounded-lg bg-red-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50"
              onClick={async () => {
                if (busy) return;
                setBusy(true);
                setDeleteError("");
                try {
                  await productApi.remove(product.id);
                  navigate(productPath(`/${product.id}/flash-deals`), {
                    replace: true,
                    state: {
                      message: "Product deleted. You can still view its flash deal history below.",
                      productName: product.name,
                    },
                  });
                } catch (err) {
                  setDeleteError(describeError(err));
                } finally {
                  setBusy(false);
                }
              }}
            >
              <Trash2 className="size-4" />
              {busy ? "Deleting…" : "Delete product"}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
