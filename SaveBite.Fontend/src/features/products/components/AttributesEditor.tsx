import { useState } from "react";
import { ImagePlus, Plus, Trash2, X } from "lucide-react";
import type { DraftAttribute, ProductAttribute } from "../types/product.types";
import { fileError, LIMITS } from "../utils/productForm";
import { Alert, FilePreview, iconButton, inputClass, secondaryButton } from "./ProductUi";

export function AttributesEditor({
  attributes,
  onChange,
  existing = [],
}: {
  attributes: DraftAttribute[];
  onChange: (attributes: DraftAttribute[]) => void;
  existing?: ProductAttribute[];
}) {
  const [error, setError] = useState("");
  const adding = existing.length > 0;
  const update = (id: string, patch: Partial<DraftAttribute>) =>
    onChange(attributes.map((a) => (a.id === id ? { ...a, ...patch } : a)));
  return (
    <div className="space-y-4">
      {error && <Alert>{error}</Alert>}
      {!attributes.length && (
        <p className="rounded-lg bg-neutral-50 p-4 text-sm leading-6 text-text-secondary">
          Products without attributes have one default variant. Add attributes for sizes, flavors,
          or other options.
        </p>
      )}
      {attributes.map((attribute, index) => {
        const saved = existing.find((a) => a.id === attribute.id)?.values ?? [];
        return (
          <div key={attribute.id} className="rounded-lg border border-neutral-200 p-4">
            <div className="mb-4 flex items-center gap-3">
              <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-primary-50 text-xs font-bold text-primary-700">
                {index + 1}
              </span>
              {adding ? (
                <h3 className="flex-1 font-semibold">{attribute.name}</h3>
              ) : (
                <label className="flex-1 text-xs font-medium text-text-secondary">
                  Attribute name
                  <input
                    className={inputClass}
                    placeholder="E.g. Size"
                    maxLength={100}
                    value={attribute.name}
                    onChange={(e) => update(attribute.id, { name: e.target.value })}
                  />
                </label>
              )}
              {!adding && (
                <button
                  type="button"
                  className={iconButton}
                  aria-label={`Remove attribute ${index + 1}`}
                  onClick={() => onChange(attributes.filter((a) => a.id !== attribute.id))}
                >
                  <Trash2 className="size-4" />
                </button>
              )}
            </div>
            {saved.length > 0 && (
              <div className="mb-3 flex flex-wrap gap-2">
                {saved.map((v) => (
                  <span
                    key={v.id}
                    className="rounded-md bg-neutral-100 px-2.5 py-1 text-xs text-neutral-600"
                  >
                    {v.value}
                  </span>
                ))}
              </div>
            )}
            <div className="space-y-2">
              {attribute.values.map((value, vi) => (
                <div key={value.id} className="flex items-center gap-2">
                  <input
                    className={`${inputClass} mt-0!`}
                    aria-label={`Value ${vi + 1} for ${attribute.name || `attribute ${index + 1}`}`}
                    value={value.value}
                    maxLength={100}
                    placeholder="Enter a value, e.g. Small"
                    onChange={(e) =>
                      update(attribute.id, {
                        values: attribute.values.map((v) =>
                          v.id === value.id ? { ...v, value: e.target.value } : v,
                        ),
                      })
                    }
                  />
                  <label
                    className="relative flex size-10 shrink-0 cursor-pointer items-center justify-center overflow-hidden rounded-lg border border-neutral-200 text-neutral-500 hover:bg-neutral-50 focus-within:ring-2 focus-within:ring-primary-400"
                    title="Choose a value image"
                  >
                    {value.image ? (
                      <FilePreview file={value.image} />
                    ) : (
                      <ImagePlus className="size-4" />
                    )}
                    <input
                      type="file"
                      className="absolute inset-0 size-full cursor-pointer opacity-0"
                      aria-label={`Image for ${value.value || `value ${vi + 1}`}`}
                      accept="image/jpeg,image/png,image/webp"
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        e.target.value = "";
                        if (!file) return;
                        const message = fileError([file]);
                        setError(message);
                        if (!message)
                          update(attribute.id, {
                            values: attribute.values.map((v) =>
                              v.id === value.id ? { ...v, image: file } : v,
                            ),
                          });
                      }}
                    />
                  </label>
                  {value.image && (
                    <button
                      type="button"
                      className={iconButton}
                      aria-label={`Remove image for ${value.value}`}
                      onClick={() =>
                        update(attribute.id, {
                          values: attribute.values.map((v) =>
                            v.id === value.id ? { ...v, image: undefined } : v,
                          ),
                        })
                      }
                    >
                      <X className="size-3" />
                    </button>
                  )}
                  <button
                    type="button"
                    className={iconButton}
                    aria-label={`Remove value ${vi + 1} for attribute ${index + 1}`}
                    onClick={() =>
                      update(attribute.id, {
                        values: attribute.values.filter((v) => v.id !== value.id),
                      })
                    }
                  >
                    <Trash2 className="size-4" />
                  </button>
                </div>
              ))}
            </div>
            <button
              type="button"
              className="mt-3 inline-flex items-center gap-1.5 text-sm font-semibold text-primary-700 disabled:opacity-40"
              disabled={saved.length + attribute.values.length >= LIMITS.values}
              onClick={() =>
                update(attribute.id, {
                  values: [...attribute.values, { id: crypto.randomUUID(), value: "" }],
                })
              }
            >
              <Plus className="size-4" />
              Add value{" "}
              <span className="font-normal text-neutral-400">
                ({saved.length + attribute.values.length}/20)
              </span>
            </button>
          </div>
        );
      })}
      {!adding && (
        <button
          type="button"
          className={secondaryButton}
          disabled={attributes.length >= LIMITS.attributes}
          onClick={() =>
            onChange([
              ...attributes,
              {
                id: crypto.randomUUID(),
                name: "",
                values: [{ id: crypto.randomUUID(), value: "" }],
              },
            ])
          }
        >
          <Plus className="size-4" />
          Add attribute ({attributes.length}/3)
        </button>
      )}
      <p className="text-xs leading-5 text-neutral-500">
        Attribute names and values are fixed after creation. Add new values to create more variants.
      </p>
    </div>
  );
}
