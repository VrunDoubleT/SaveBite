import { useState, type ComponentProps } from "react";
import { ImageOff, type LucideIcon } from "lucide-react";

type MediaImageProps = ComponentProps<"img"> & {
  fallbackIcon?: LucideIcon;
  showFallbackLabel?: boolean;
};

/** Reserves its caller's image area and shows an honest fallback for missing media. */
export function MediaImage({
  src,
  alt = "",
  className = "",
  fallbackIcon: FallbackIcon = ImageOff,
  showFallbackLabel = true,
  loading = "lazy",
  onError,
  ...props
}: MediaImageProps) {
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  return (
    <span
      className={`flex items-center justify-center overflow-hidden bg-neutral-100 ${className}`}
    >
      {src && src !== failedSrc ? (
        <img
          {...props}
          src={src}
          alt={alt}
          loading={loading}
          className="h-full w-full object-cover"
          onError={(event) => {
            setFailedSrc(src);
            onError?.(event);
          }}
        />
      ) : (
        <span
          role={alt ? "img" : undefined}
          aria-label={alt ? `${alt} — image unavailable` : undefined}
          className="flex flex-col items-center justify-center gap-2 p-2 text-text-muted"
        >
          <FallbackIcon className="size-6" aria-hidden="true" />
          {showFallbackLabel && <span className="text-caption">Image unavailable</span>}
        </span>
      )}
    </span>
  );
}
