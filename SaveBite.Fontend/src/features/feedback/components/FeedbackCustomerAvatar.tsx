import { useEffect, useState } from "react";

interface FeedbackCustomerAvatarProps {
  avatarUrl: string | null;
  customerName: string;
  size?: "sm" | "md";
}

export function FeedbackCustomerAvatar({
  avatarUrl,
  customerName,
  size = "sm",
}: FeedbackCustomerAvatarProps) {
  const [imageError, setImageError] = useState(false);

  useEffect(() => {
    setImageError(false);
  }, [avatarUrl]);

  const showImage = Boolean(avatarUrl) && !imageError;

  const sizeClass = size === "md" ? "size-10 text-sm" : "size-9 text-xs";

  return (
    <div
      className={`flex shrink-0 items-center justify-center overflow-hidden rounded-full border border-border-default bg-primary-50 font-semibold uppercase text-primary-700 ${sizeClass}`}
    >
      {showImage ? (
        <img
          src={avatarUrl!}
          alt={`${customerName}'s avatar`}
          className="h-full w-full object-cover"
          onError={() => setImageError(true)}
        />
      ) : (
        customerName.trim().charAt(0) || "?"
      )}
    </div>
  );
}
