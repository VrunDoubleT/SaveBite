import {
  Avatar,
  Badge,
  Button,
  Card,
  ErrorState,
  IconButton,
  Skeleton,
} from "@/shared/components/ui";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { useEffect, useRef, useState, type ChangeEvent, type ReactNode } from "react";
import {
  Camera,
  CircleCheck,
  LockKeyhole,
  Mail,
  Pencil,
  Phone,
  ShieldAlert,
  UserRound,
  type LucideIcon,
} from "lucide-react";
import type { CustomerStatus } from "@/features/auth/types/auth.types";
import { profileApi } from "@/features/profiles/api/profileApi";
import { EditProfileForm } from "@/features/profiles/components/EditProfileForm";
import { ChangePasswordSection } from "@/features/profiles/components/ChangePasswordSection";
import type { UpdateUserProfileInput } from "@/features/profiles/types/profile.types";
import { useAuthStore } from "@/shared/stores/authStore";

const STATUS_CONFIG: Record<
  CustomerStatus,
  { label: string; icon: LucideIcon; textClass: string }
> = {
  active: {
    label: "Active",
    icon: CircleCheck,
    textClass: "text-primary-700",
  },
  suspended: {
    label: "Suspended",
    icon: ShieldAlert,
    textClass: "text-danger-strong",
  },
};

const MAX_AVATAR_SIZE = 10 * 1024 * 1024;
const ALLOWED_AVATAR_TYPES = ["image/jpeg", "image/png", "image/webp"];

export function UserProfileCard() {
  const user = useAuthStore((state) => state.user);
  const isInitializing = useAuthStore((state) => state.isInitializing);
  const [avatarError, setAvatarError] = useState<string | null>(null);
  const [profileLoadError, setProfileLoadError] = useState<string | null>(null);
  const refreshUser = useAuthStore((state) => state.refreshUser);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const passwordButtonRef = useRef<HTMLButtonElement>(null);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);

  // Revokes the previous object URL whenever the preview changes or the card unmounts.
  useEffect(() => {
    return () => {
      if (avatarPreviewUrl) URL.revokeObjectURL(avatarPreviewUrl);
    };
  }, [avatarPreviewUrl]);

  if (isInitializing)
    return (
      <Card aria-label="Loading profile" role="status" className="space-y-4">
        <div className="flex gap-4">
          <Skeleton className="size-20 rounded-full" />
          <div className="flex-1 space-y-3">
            <Skeleton className="h-6 w-40" />
            <Skeleton className="h-4 w-48" />
          </div>
        </div>
        <Skeleton className="h-40" />
      </Card>
    );
  if (!user)
    return (
      <ErrorState
        title="Couldn't load your profile"
        description={
          profileLoadError ?? "Your account information is unavailable. Try loading it again."
        }
        onRetry={() =>
          void refreshUser().catch((error: unknown) =>
            setProfileLoadError(getApiErrorMessage(error)),
          )
        }
      />
    );

  const status = STATUS_CONFIG[user.customerStatus];
  const displayedAvatarUrl = avatarPreviewUrl ?? user.avatarUrl;

  function resetAvatarSelection() {
    setAvatarError(null);
    setSelectedAvatarFile(null);
    setAvatarPreviewUrl(null);
  }

  async function handleProfileSubmit(input: UpdateUserProfileInput, avatarFile: File | null) {
    setIsSavingProfile(true);

    try {
      await profileApi.updateProfile(input);

      if (avatarFile) {
        await profileApi.uploadAvatar(avatarFile);
      }

      await refreshUser();
      resetAvatarSelection();
      setIsEditing(false);
      toast.success("Profile updated successfully.");
    } finally {
      setIsSavingProfile(false);
    }
  }

  function handleAvatarChange(file: File) {
    const isValidType = ALLOWED_AVATAR_TYPES.includes(file.type);
    const isValidSize = file.size > 0 && file.size <= MAX_AVATAR_SIZE;

    if (!isValidType || !isValidSize) {
      setAvatarError(
        !isValidType
          ? "Choose a JPG, PNG or WebP image."
          : "Choose an image larger than 0 bytes and no larger than 10 MB.",
      );
      return;
    }
    setAvatarError(null);

    setSelectedAvatarFile(file);
    setAvatarPreviewUrl(URL.createObjectURL(file));
  }

  function openAvatarPicker() {
    if (!isEditing || isSavingProfile) return;
    fileInputRef.current?.click();
  }

  function handleFileInputChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (file) handleAvatarChange(file);
  }

  function handleEditToggle() {
    if (isEditing) {
      resetAvatarSelection();
    }

    setIsEditing((current) => !current);
  }

  return (
    <Card aria-labelledby="profile-heading" className="!p-0 overflow-hidden">
      <div className="flex flex-col gap-4 border-b border-border-default bg-primary-50/50 p-4 sm:p-6 xl:flex-row xl:items-center xl:justify-between">
        <div className="flex min-w-0 items-center gap-4">
          <div className="relative shrink-0">
            <Avatar
              fullName={user.fullName}
              avatarUrl={displayedAvatarUrl}
              className="!size-20 !text-2xl ring-4 ring-bg-surface"
            />

            {isEditing && (
              <>
                <IconButton
                  variant="secondary"
                  type="button"
                  onClick={openAvatarPicker}
                  disabled={isSavingProfile}
                  aria-label="Change avatar"
                  className="absolute -bottom-1 -right-1 flex !size-11 items-center justify-center rounded-full border border-primary-200 bg-primary-50 text-primary-700 ring-2 ring-bg-surface transition-colors hover:bg-primary-100 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  <Camera className="size-4" aria-hidden="true" />
                </IconButton>

                <input
                  ref={fileInputRef}
                  type="file"
                  accept={ALLOWED_AVATAR_TYPES.join(",")}
                  className="hidden"
                  onChange={handleFileInputChange}
                />
              </>
            )}
          </div>

          <div className="min-w-0">
            <h2
              id="profile-heading"
              title={user.fullName}
              className="line-clamp-2 text-page font-semibold text-text-primary [overflow-wrap:anywhere]"
            >
              {user.fullName}
            </h2>
            <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-2">
              <p
                title={user.email}
                className="min-w-0 line-clamp-2 text-body text-text-secondary [overflow-wrap:anywhere]"
              >
                {user.email}
              </p>
              <Badge
                tone={user.customerStatus === "suspended" ? "danger" : "primary"}
                className="shrink-0"
              >
                <status.icon className="size-3" aria-hidden="true" />
                {status.label}
              </Badge>
            </div>
          </div>
        </div>

        {!isEditing && (
          <div className="flex flex-wrap items-center gap-2 xl:justify-end">
            <Button
              type="button"
              onClick={handleEditToggle}
              disabled={isChangingPassword}
              className="min-h-11"
            >
              <Pencil className="size-4" aria-hidden="true" />
              Edit profile
            </Button>
            <Button
              variant="secondary"
              ref={passwordButtonRef}
              type="button"
              onClick={() => setIsChangingPassword(true)}
              aria-expanded={isChangingPassword}
              aria-controls={isChangingPassword ? "change-password-form" : undefined}
              disabled={isChangingPassword}
              className="min-h-11"
            >
              <LockKeyhole className="size-4" aria-hidden="true" />
              Change password
            </Button>
          </div>
        )}
      </div>

      <div className="p-4 sm:p-6">
        {isEditing && (
          <div className="mb-5">
            <p className="text-caption text-text-secondary">
              Choose the camera icon to update your photo. JPG, PNG or WebP · up to 10 MB.
            </p>
            {avatarError && (
              <p role="alert" className="mt-2 text-body text-danger-strong">
                {avatarError}
              </p>
            )}
          </div>
        )}
        {isChangingPassword ? (
          <ChangePasswordSection
            onClose={() => {
              setIsChangingPassword(false);
              requestAnimationFrame(() => passwordButtonRef.current?.focus());
            }}
          />
        ) : isEditing ? (
          <EditProfileForm
            user={user}
            isSubmitting={isSavingProfile}
            selectedAvatarFile={selectedAvatarFile}
            onSubmit={handleProfileSubmit}
            onCancel={handleEditToggle}
          />
        ) : (
          <div className="space-y-4">
            <h3 className="text-section font-semibold text-text-primary">Personal information</h3>
            <p className="mt-1 text-sm text-text-secondary">
              These details come from your signed-in account.
            </p>

            <dl className="grid gap-3 sm:grid-cols-2">
              <ProfileField icon={UserRound} label="Full name" value={user.fullName} />
              <ProfileField
                icon={Phone}
                label="Phone number"
                value={user.phone}
                fallback="Not provided"
              />
              <ProfileField icon={Mail} label="Email" value={user.email} />
              <ProfileField
                icon={status.icon}
                label="Account status"
                value={status.label}
                valueClassName={status.textClass}
                tone={user.customerStatus === "suspended" ? "danger" : "default"}
              />
            </dl>
          </div>
        )}
      </div>
    </Card>
  );
}

interface ProfileFieldProps {
  icon: LucideIcon;
  label: string;
  value: string | null;
  fallback?: string;
  valueClassName?: string;
  tone?: "default" | "danger";
}

const FIELD_TONE_CLASSES = {
  default: {
    container: "border-border-default bg-bg-surface",
    icon: "bg-primary-50 text-primary-700",
    label: "text-text-muted",
  },
  danger: {
    container: "border-border-default bg-bg-surface",
    icon: "bg-danger-soft text-danger-strong",
    label: "text-text-muted",
  },
} as const;

function ProfileField({
  icon: Icon,
  label,
  value,
  fallback = "-",
  valueClassName = "text-text-primary",
  tone = "default",
}: ProfileFieldProps): ReactNode {
  const hasValue = Boolean(value);
  const toneClasses = FIELD_TONE_CLASSES[tone];

  return (
    <div
      className={`group flex items-center gap-4 min-w-0 rounded-md border p-3 transition-colors ${toneClasses.container}`}
    >
      <span
        className={`flex size-10 shrink-0 items-center justify-center rounded-lg transition-colors ${toneClasses.icon}`}
        aria-hidden="true"
      >
        <Icon className="size-5" />
      </span>

      <div className="min-w-0">
        <dt className={`text-caption font-medium ${toneClasses.label}`}>{label}</dt>
        <dd
          title={value || fallback}
          className={`mt-1 line-clamp-2 text-body [overflow-wrap:anywhere] ${
            hasValue ? `font-semibold ${valueClassName}` : "font-normal text-text-muted"
          }`}
        >
          {hasValue ? value : fallback}
        </dd>
      </div>
    </div>
  );
}
