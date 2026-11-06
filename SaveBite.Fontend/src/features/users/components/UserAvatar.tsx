import { useEffect, useState } from "react";

interface UserAvatarProps {
  avatarUrl: string | null;
  fullName: string;
  size?: "sm" | "lg";
}

export function UserAvatar({ avatarUrl, fullName, size = "sm" }: UserAvatarProps) {
  const [hasImageError, setHasImageError] = useState(false);

  useEffect(() => {
    setHasImageError(false);
  }, [avatarUrl]);

  const sizeClass = size === "lg" ? "size-16 text-2xl" : "size-10 text-sm";

  const initial = fullName.trim().charAt(0).toUpperCase() || "?";

  const showImage = Boolean(avatarUrl) && !hasImageError;

  return (
    <div
      className={`flex shrink-0 items-center justify-center overflow-hidden rounded-full border border-primary-100 bg-primary-50 font-bold text-primary-600 shadow-sm ${sizeClass}`}
    >
      {showImage ? (
        <img
          src={avatarUrl!}
          alt={`${fullName}'s avatar`}
          className="h-full w-full object-cover"
          onError={() => setHasImageError(true)}
        />
      ) : (
        initial
      )}
    </div>
  );
}
