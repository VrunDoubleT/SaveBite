import { useState } from "react";
import { Save, Upload } from "lucide-react";
import { productApi } from "../api/productApi";
import type { Product, ProductValue } from "../types/product.types";
import { describeError, fileError } from "../utils/productForm";
import { GalleryEditor } from "./GalleryEditor";
import {
  Alert,
  FilePreview,
  Panel,
  primaryButton,
  ProductPhoto,
  secondaryButton,
} from "./ProductUi";

export function ProductImagesPanel({
  product,

  onSaved,
}: {
  product: Product;

  onSaved: (product: Product) => void;
}) {
  const [existing, setExisting] = useState(product.images.filter((i) => i.source === "product"));
  const [files, setFiles] = useState<File[]>([]);
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  return (
    <div className="space-y-5">
      <Panel
        title="Product images"
        description="Use the arrows to reorder images. New images follow retained images."
      >
        <form
          className="space-y-4"
          onSubmit={async (e) => {
            e.preventDefault();
            if (busy) return;
            const message = fileError(files);
            if (message) {
              setError(message);
              return;
            }
            setBusy(true);
            setError("");
            const form = new FormData();
            existing.forEach((image, i) => form.append(`RetainedImageIds[${i}]`, image.id));
            files.forEach((file) => form.append("Images", file));
            try {
              const saved = await productApi.images(product.id, form);
              setExisting(saved.images.filter((i) => i.source === "product"));
              setFiles([]);
              onSaved(saved);
            } catch (err) {
              setError(describeError(err));
            } finally {
              setBusy(false);
            }
          }}
        >
          <fieldset disabled={busy}>
            <GalleryEditor
              existing={existing}
              onExistingChange={setExisting}
              files={files}
              onFilesChange={setFiles}
            />
          </fieldset>
          {!existing.length && !files.length && (
            <p className="text-sm text-amber-700">
              Saving now will remove all gallery images from this product.
            </p>
          )}
          {error && <Alert>{error}</Alert>}
          <div className="flex justify-end">
            <button className={primaryButton} disabled={busy}>
              <Save className="size-4" />
              {busy ? "Saving images…" : "Save product images"}
            </button>
          </div>
        </form>
      </Panel>
      <Panel
        title="Attribute value images"
        description="Each value has one image shared by its variants. Value images do not count toward the 10 gallery image limit."
      >
        <div className="space-y-6">
          {product.attributes.map((attribute) => (
            <div key={attribute.id}>
              <h3 className="mb-3 text-sm font-semibold">{attribute.name}</h3>
              <div className="grid gap-3 sm:grid-cols-2">
                {attribute.values.map((value) => (
                  <ValueImageEditor
                    key={value.id}
                    value={value}
                    productId={product.id}
                    onSaved={onSaved}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      </Panel>
      <Panel title="Saved image order" description="Product images first, then variant images.">
        {product.images.length ? (
          <div className="flex gap-3 overflow-x-auto pb-2">
            {product.images.map((image) => (
              <div key={`${image.source}-${image.id}`} className="w-28 shrink-0">
                <ProductPhoto
                  className="aspect-square w-28"
                  src={image.url}
                  alt={image.value ?? product.name}
                />
                <p className="mt-2 truncate text-xs text-neutral-500">
                  {image.source === "product"
                    ? "Product images"
                    : `${image.attributeName}: ${image.value}`}
                </p>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-neutral-500">No images yet.</p>
        )}
      </Panel>
    </div>
  );
}
function ValueImageEditor({
  value,
  productId,

  onSaved,
}: {
  value: ProductValue;
  productId: string;

  onSaved: (product: Product) => void;
}) {
  const [file, setFile] = useState<File>(),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  return (
    <div className="space-y-3 rounded-lg border border-neutral-200 p-3">
      <div className="flex items-center gap-3">
        {file ? (
          <div className="size-14 shrink-0 overflow-hidden rounded-lg">
            <FilePreview file={file} />
          </div>
        ) : (
          <ProductPhoto src={value.imageUrl} alt={value.value} />
        )}
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium">{value.value}</p>
          <label className="relative mt-1 inline-flex cursor-pointer items-center gap-1 text-xs font-semibold text-primary-700 focus-within:ring-2 focus-within:ring-primary-400">
            <Upload className="size-3" />
            {value.imageUrl ? "Change image" : "Choose image"}
            <input
              type="file"
              disabled={busy}
              accept="image/jpeg,image/png,image/webp"
              aria-label={`Choose image for ${value.value}`}
              className="absolute inset-0 w-full cursor-pointer opacity-0"
              onChange={(e) => {
                const next = e.target.files?.[0];
                e.target.value = "";
                if (!next) return;
                const message = fileError([next]);
                setError(message);
                if (!message) setFile(next);
              }}
            />
          </label>
        </div>
      </div>
      {error && <Alert>{error}</Alert>}
      {file && (
        <div className="flex justify-end gap-2">
          <button
            type="button"
            disabled={busy}
            className={secondaryButton}
            onClick={() => {
              setFile(undefined);
              setError("");
            }}
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={busy}
            className={primaryButton}
            onClick={async () => {
              if (busy) return;
              setBusy(true);
              setError("");
              try {
                onSaved(await productApi.valueImage(productId, value.id, file));
                setFile(undefined);
              } catch (err) {
                setError(describeError(err));
              } finally {
                setBusy(false);
              }
            }}
          >
            {busy ? "Saving…" : "Save image"}
          </button>
        </div>
      )}
    </div>
  );
}
