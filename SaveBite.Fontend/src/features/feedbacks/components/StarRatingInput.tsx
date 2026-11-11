import { useState } from "react";
import { Star } from "lucide-react";

const RATING_LABELS: Record<number, string> = {
  1: "Terrible",
  2: "Poor",
  3: "Okay",
  4: "Good",
  5: "Excellent",
};

interface StarRatingInputProps {
  value: number;
  onChange: (rating: number) => void;
  disabled?: boolean;
  size?: "sm" | "md" | "lg";
}

export function StarRatingInput({
  value,
  onChange,
  disabled = false,
  size = "md",
}: StarRatingInputProps) {
  const [hoverRating, setHoverRating] = useState(0);
  const displayRating = hoverRating || value;

  const sizeClass =
    size === "sm" ? "size-5" : size === "lg" ? "size-7" : "size-6";

  return (
    <div className="inline-flex max-w-full flex-wrap items-center gap-2">
      <div className="flex items-center gap-1">
        {[1, 2, 3, 4, 5].map((star) => {
          const isFilled = star <= displayRating;
          return (
            <button
              key={star}
              type="button"
              disabled={disabled}
              onMouseEnter={() => !disabled && setHoverRating(star)}
              onMouseLeave={() => !disabled && setHoverRating(0)}
              onClick={() => !disabled && onChange(star)}
              aria-label={`Rate ${star} out of 5`}
              aria-pressed={value === star}
              className="flex size-11 shrink-0 items-center justify-center rounded-md transition-colors hover:bg-accent-50 focus-visible:outline-none focus-visible:shadow-focus disabled:pointer-events-none disabled:opacity-50"
            >
              <Star
                className={`${sizeClass} ${
                  isFilled
                    ? "fill-accent-300 text-accent-700"
                    : "text-neutral-300"
                }`}
              />
            </button>
          );
        })}
      </div>
      {displayRating > 0 && (
        <span className="text-xs font-medium text-text-secondary">
          {displayRating}/5 · {RATING_LABELS[displayRating]}
        </span>
      )}
    </div>
  );
}