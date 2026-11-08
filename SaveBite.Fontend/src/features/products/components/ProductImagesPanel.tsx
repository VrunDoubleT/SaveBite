import { isUploadCancelled } from "@/shared/uploads/uploadErrors";
import { toast } from "@/shared/stores/toastStore";
import { UploadDraftChoice } from "@/shared/uploads/UploadDraftChoice";
import { UploadTaskGuard } from "@/shared/uploads/UploadTaskGuard";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useLocation } from "react-router-dom";
import { FieldError } from "./FieldError";
import { Save, Upload } from "lucide-react";
import { productApi } from "../api/productApi";
import type { Product, ProductValue, ProductImage } from "../types/product.types";
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
import { useAuthStore } from "@/shared/stores/authStore";
import { getUploadDraft, removeUploadDraft } from "@/shared/uploads/uploadDraftStore";
import { startOrResumeUploadDraft, uploadReferences } from "@/shared/uploads/uploadDrafts";

function ProductImagesPanelContent({
  product,

  onSaved,
}: {
  product: Product;

  onSaved: (product: Product) => void;
}) {
  const location = useLocation();
  const userId = useAuthStore((state) => state.user?.id);
  const [activeDraftId, setActiveDraftId] = useState<string>();
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<{ gallery: { existing: ProductImage[]; files: File[] } }>({
    defaultValues: {
      gallery: { existing: product.images.filter((i) => i.source === "product"), files: [] },
    },
    mode: "onChange",
  });
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  useEffect(() => {
    const draftId = (location.state as { uploadDraftId?: string } | null)?.uploadDraftId;
    if (!draftId || !userId) return;
    let active = true;
    void getUploadDraft(draftId).then((draft) => {
      if (!active || !draft || draft.userId !== userId) return;
      const saved = draft.formData as {
        operation?: string;
        retainedImageIds?: string[];
        files?: File[];
      };
      if (saved.operation === "gallery") {
        setActiveDraftId(draft.id);
        const byId = new Map(
          product.images
            .filter((image) => image.source === "product")
            .map((image) => [image.id, image]),
        );
        reset({
          gallery: {
            existing: (saved.retainedImageIds ?? [])
              .map((id) => byId.get(id))
              .filter((image): image is ProductImage => Boolean(image)),
            files: saved.files ?? [],
          },
        });
      }
    });
    return () => {
      active = false;
    };
  }, [location.state, product.images, reset, userId]);
  return (
    <div className="space-y-5">
      <Panel
        title="Product images"
        description="Use the arrows to reorder images. New images follow retained images."
      >
        <form
          className="space-y-4"
          noValidate
          onSubmit={handleSubmit(async ({ gallery: { existing, files } }) => {
            if (busy) return;
            setBusy(true);
            setError("");
            const form = new FormData();
            let completedDraftId = activeDraftId;
            existing.forEach((image, i) => form.append(`RetainedImageIds[${i}]`, image.id));
            try {
              if (files.length) {
                if (!userId) throw new Error("Sign in before uploading product images.");
                completedDraftId ??= crypto.randomUUID();
                setActiveDraftId(completedDraftId);
                const draft = await startOrResumeUploadDraft(
                  {
                    draftId: completedDraftId,
                    userId,
                    purpose: "product",
                    targetId: product.id,
                    title: `Update gallery: ${product.name}`,
                    path: `${location.pathname}?tab=images`,
                    formData: {
                      operation: "gallery",
                      retainedImageIds: existing.map((image) => image.id),
                      files,
                    },
                    files: files.map((file) => ({ field: "Images", file })),
                  },
                  completedDraftId,
                );
                completedDraftId = draft.id;
                setActiveDraftId(draft.id);
                form.append("__uploadAssets", JSON.stringify(uploadReferences(draft.files)));
              }
              const saved = await productApi.images(product.id, form);
              if (completedDraftId) await removeUploadDraft(completedDraftId);
              setActiveDraftId(undefined);
              reset({
                gallery: {
                  existing: saved.images.filter((i) => i.source === "product"),
                  files: [],
                },
              });
              onSaved(saved);
            } catch (err) {
              if (isUploadCancelled(err))
                toast.info(
                  "Upload stopped. Your draft is saved; edit it or submit again when ready.",
                );
              else setError(describeError(err));
            } finally {
              setBusy(false);
            }
          })}
        >
          <fieldset disabled={busy}>
            <Controller
              control={control}
              name="gallery"
              rules={{
                validate: ({ existing, files }) =>
                  existing.length + files.length === 0
                    ? "Product must have at least 1 gallery image."
                    : existing.length + files.length > 10
                      ? "At most 10 gallery images are allowed."
                      : fileError(files) || true,
              }}
              render={({ field }) => (
                <GalleryEditor
                  existing={field.value.existing}
                  files={field.value.files}
                  onExistingChange={(existing) => field.onChange({ ...field.value, existing })}
                  onFilesChange={(files) => field.onChange({ ...field.value, files })}
                />
              )}
            />
          </fieldset>
          <FieldError id="product-gallery-error" message={errors.gallery?.message} />
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
                const body = new FormData();
                body.append("Image", file);
                onSaved(await productApi.valueImage(productId, value.id, body));
                setFile(undefined);
              } catch (err) {
                if (isUploadCancelled(err))
                  toast.info(
                    "Upload stopped. Your draft is saved; edit it or submit again when ready.",
                  );
                else setError(describeError(err));
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

export function ProductImagesPanel(props: Parameters<typeof ProductImagesPanelContent>[0]) {
  return (
    <UploadTaskGuard
      scope={{ purpose: "product", targetIds: [props.product.id], operation: "gallery" }}
    >
      <UploadDraftChoice
        scope={{ purpose: "product", targetIds: [props.product.id], operation: "gallery" }}
      >
        {(draftId) => <ProductImagesPanelContent key={draftId ?? "new"} {...props} />}
      </UploadDraftChoice>
    </UploadTaskGuard>
  );
}
