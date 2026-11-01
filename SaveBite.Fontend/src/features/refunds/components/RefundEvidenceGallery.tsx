import { useEffect, useState } from "react";
import { SharedModal } from "@/shared/components/modal/BaseModal";

export function RefundEvidenceGallery({ urls, label }: { urls: string[]; label: string }) {
  const [selected, setSelected] = useState("");
  useEffect(() => {
    if (!selected) return;
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setSelected("");
    };
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [selected]);
  const images = urls.filter((url) => {
    try {
      return new URL(url).protocol === "https:";
    } catch {
      return false;
    }
  });
  if (!images.length) return null;
  return (
    <div className="mt-3">
      <p className="mb-2 text-xs font-medium text-neutral-500">{label}</p>
      <div className="flex flex-wrap gap-2">
        {images.map((url, index) => (
          <button
            key={`${url}:${index}`}
            type="button"
            aria-label={`View ${label.toLowerCase()} ${index + 1}`}
            onClick={() => {
              setSelected(url);
            }}
            className="overflow-hidden rounded-lg border border-neutral-200 focus-visible:outline-2 focus-visible:outline-primary-500"
          >
            <img
              src={url}
              alt={`${label} ${index + 1}`}
              loading="lazy"
              className="size-20 object-cover sm:size-24"
            />
          </button>
        ))}
      </div>
      <SharedModal
        isOpen={!!selected}
        onClose={() => setSelected("")}
        title={label}
        maxWidthClass="max-w-4xl"
      >
        {selected && (
          <img
            src={selected}
            alt={label}
            className="mx-auto max-h-[70vh] max-w-full rounded-lg object-contain"
          />
        )}
      </SharedModal>
    </div>
  );
}
