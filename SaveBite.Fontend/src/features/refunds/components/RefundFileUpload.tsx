import { useCallback, useRef, useState } from "react";
import { Upload, X } from "lucide-react";

// Keep selected files local; release preview URLs when files change or the picker unmounts.
export function RefundFileUpload({
  files,
  onChange,
  disabled,
  max = 5,
  purpose,
}: {
  files: File[];
  onChange: (files: File[]) => void;
  disabled?: boolean;
  max?: number;
  purpose: "request-evidence" | "destination-qr" | "transfer-proof";
}) {
  const [error, setError] = useState("");
  const ref = useRef<HTMLInputElement>(null);
  return (
    <div className="space-y-2">
      <input
        ref={ref}
        type="file"
        accept="image/png,image/jpeg,image/webp"
        multiple={max > 1}
        className="sr-only"
        aria-label="Choose refund images"
        disabled={disabled}
        onChange={(event) => {
          const selected = Array.from(event.target.files ?? []);
          event.target.value = "";
          if (!selected.length) return;
          if (
            selected.length + files.length > max ||
            selected.some(
              (file) =>
                file.size > 5 * 1024 * 1024 ||
                !["image/png", "image/jpeg", "image/webp"].includes(file.type),
            )
          ) {
            setError(`Choose up to ${max} PNG, JPEG or WebP images, no larger than 5 MB each.`);
            return;
          }
          setError("");
          onChange([...files, ...selected]);
        }}
      />
      <button
        type="button"
        disabled={disabled || files.length >= max}
        onClick={() => ref.current?.click()}
        className="flex w-full items-center justify-center gap-2 rounded-lg border border-dashed border-neutral-300 bg-neutral-50 px-3 py-5 text-xs font-medium text-neutral-600 hover:border-emerald-400 disabled:opacity-50"
      >
        <Upload size={16} />
        {purpose === "destination-qr" ? "Choose your bank QR image" : "Choose evidence images"}
      </button>
      <p className="text-[11px] text-neutral-400">
        PNG, JPEG or WebP · Up to 5 MB each · Uploaded only when you submit
      </p>
      {error && (
        <p role="alert" className="text-xs text-red-700">
          {error}
        </p>
      )}
      <div className="flex flex-wrap gap-2">
        {files.map((file, index) => (
          <div key={index} className="relative">
            <LocalImage file={file} />
            <button
              type="button"
              aria-label="Remove image"
              disabled={disabled}
              className="absolute -right-1 -top-1 rounded-full border bg-white p-0.5"
              onClick={() => {
                onChange(files.filter((_, i) => i !== index));
              }}
            >
              <X size={12} />
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}

function LocalImage({ file }: { file: File }) {
  const attach = useCallback(
    (node: HTMLAnchorElement | null) => {
      if (!node) return;
      const url = URL.createObjectURL(file);
      node.href = url;
      node.querySelector("img")!.src = url;
      return () => URL.revokeObjectURL(url);
    },
    [file],
  );
  return (
    <a ref={attach} target="_blank" rel="noopener noreferrer">
      <img alt={file.name} className="size-20 rounded-lg border border-neutral-200 object-cover" />
    </a>
  );
}
