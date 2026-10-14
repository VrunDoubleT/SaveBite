import { useState } from "react";
import { ArrowLeft, ArrowRight, ImagePlus, X } from "lucide-react";
import type { ProductImage } from "../types/product.types";
import { fileError, LIMITS } from "../utils/productForm";
import { Alert, FilePreview, iconButton } from "./ProductUi";

interface Props {
  existing?: ProductImage[];
  onExistingChange?: (images: ProductImage[]) => void;
  files: File[];
  onFilesChange: (files: File[]) => void;
}
export function GalleryEditor({ existing = [], onExistingChange, files, onFilesChange }: Props) {
  const [error, setError] = useState("");
  const count = existing.length + files.length;
  function move(index: number, direction: number) {
    const list = [...existing];
    [list[index], list[index + direction]] = [list[index + direction], list[index]];
    onExistingChange?.(list);
  }
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between text-sm">
        <span className="text-text-secondary">Product images appear before variant images.</span>
        <span className="whitespace-nowrap font-semibold text-primary-700">
          {count} / 10 images
        </span>
      </div>
      {error && <Alert>{error}</Alert>}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-5">
        {existing.map((image, index) => (
          <div key={image.id} className="overflow-hidden rounded-lg border border-neutral-200">
            <img
              src={image.url}
              alt={`Product images ${index + 1}`}
              className="aspect-square w-full object-cover"
            />
            <div className="flex items-center justify-between p-1">
              <div className="flex">
                <button
                  type="button"
                  disabled={index === 0}
                  title="Move earlier"
                  aria-label={`Move image ${index + 1} earlier`}
                  className={iconButton}
                  onClick={() => move(index, -1)}
                >
                  <ArrowLeft className="size-4" />
                </button>
                <button
                  type="button"
                  disabled={index === existing.length - 1}
                  title="Move later"
                  aria-label={`Move image ${index + 1} later`}
                  className={iconButton}
                  onClick={() => move(index, 1)}
                >
                  <ArrowRight className="size-4" />
                </button>
              </div>
              <button
                type="button"
                className={iconButton}
                aria-label={`Remove image ${index + 1}`}
                onClick={() => onExistingChange?.(existing.filter((x) => x.id !== image.id))}
              >
                <X className="size-4" />
              </button>
            </div>
          </div>
        ))}
        {files.map((file, index) => (
          <div
            key={`${file.name}-${index}`}
            className="overflow-hidden rounded-lg border border-primary-200"
          >
            <div className="aspect-square">
              <FilePreview file={file} />
            </div>
            <div className="flex items-center justify-between p-1">
              <div className="flex">
                <button
                  type="button"
                  className={iconButton}
                  disabled={index === 0}
                  aria-label={`Move new image ${index + 1} earlier`}
                  onClick={() => {
                    const next = [...files];
                    [next[index - 1], next[index]] = [next[index], next[index - 1]];
                    onFilesChange(next);
                  }}
                >
                  <ArrowLeft className="size-4" />
                </button>
                <button
                  type="button"
                  className={iconButton}
                  disabled={index === files.length - 1}
                  aria-label={`Move new image ${index + 1} later`}
                  onClick={() => {
                    const next = [...files];
                    [next[index + 1], next[index]] = [next[index], next[index + 1]];
                    onFilesChange(next);
                  }}
                >
                  <ArrowRight className="size-4" />
                </button>
              </div>
              <button
                type="button"
                className={iconButton}
                aria-label={`Remove new image ${index + 1}`}
                onClick={() => onFilesChange(files.filter((_, i) => i !== index))}
              >
                <X className="size-4" />
              </button>
            </div>
          </div>
        ))}
        {count < LIMITS.images && (
          <label className="relative flex min-h-36 cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-primary-300 bg-primary-50/50 p-3 text-center text-sm font-medium text-primary-700 transition hover:bg-primary-50 focus-within:ring-3 focus-within:ring-primary-200">
            <ImagePlus className="size-7" />
            <span>Add images</span>
            <span className="text-xs font-normal text-neutral-500">JPG, PNG, WebP · ≤ 10 MB</span>
            <input
              type="file"
              multiple
              accept="image/jpeg,image/png,image/webp"
              aria-label="Add product images"
              className="absolute inset-0 size-full cursor-pointer opacity-0"
              onChange={(event) => {
                const incoming = Array.from(event.target.files ?? []);
                event.target.value = "";
                const message =
                  count + incoming.length > LIMITS.images
                    ? "Each product can have up to 10 gallery images."
                    : fileError([...files, ...incoming]);
                setError(message);
                if (!message) onFilesChange([...files, ...incoming]);
              }}
            />
          </label>
        )}
      </div>
    </div>
  );
}
