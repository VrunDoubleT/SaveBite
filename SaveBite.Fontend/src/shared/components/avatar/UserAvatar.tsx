import { useState } from "react";

interface UserAvatarProps {
  fullName: string;
  avatarUrl?: string | null;
}

export function UserAvatar({ fullName, avatarUrl }: UserAvatarProps) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const src = avatarUrl?.trim();
  const initial = fullName.trim().charAt(0).toUpperCase() || "U";

  return (
    <span className="flex size-9 shrink-0 items-center justify-center overflow-hidden rounded-full border border-border-default bg-primary-100 text-sm font-semibold text-primary-700">
      {src && src !== failedUrl ? (
        <img
          src={src}
          alt={`${fullName}'s avatar`}
          className="h-full w-full object-cover"
          onError={() => setFailedUrl(src)}
        />
      ) : (
        <span role="img" aria-label={`${fullName}'s avatar`}>
          {initial}
        </span>
      )}
    </span>
  );
}
