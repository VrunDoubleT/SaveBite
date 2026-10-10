import { useEffect, useRef, useState, type ChangeEvent, type ReactNode } from "react";
import {
  Camera,
  CircleCheck,
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
import type { UpdateUserProfileInput } from "@/features/profiles/types/profile.types";
import { useAuthStore } from "@/shared/stores/authStore";

const STATUS_CONFIG: Record<
  CustomerStatus,
  { label: string; icon: LucideIcon; dotClass: string; badgeClass: string; textClass: string }
> = {
  active: {
    label: "Active",
    icon: CircleCheck,
    dotClass: "bg-primary-500",
    badgeClass: "bg-primary-50 text-primary-700 ring-primary-200",
    textClass: "text-primary-700",
  },
  suspended: {
    label: "Suspended",
    icon: ShieldAlert,
    dotClass: "bg-danger",
    badgeClass: "bg-red-50 text-danger ring-red-200",
    textClass: "text-danger",
  },
};

const MAX_AVATAR_SIZE = 10 * 1024 * 1024;
const ALLOWED_AVATAR_TYPES = ["image/jpeg", "image/png", "image/webp"];

function getInitial(fullName: string): string {
  return fullName.trim().charAt(0).toUpperCase() || "U";
}

export function UserProfileCard() {
  const user = useAuthStore((state) => state.user);
  const refreshUser = useAuthStore((state) => state.refreshUser);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);

  // Revokes the previous object URL whenever the preview changes or the card unmounts.
  useEffect(() => {
    return () => {
      if (avatarPreviewUrl) URL.revokeObjectURL(avatarPreviewUrl);
    };
  }, [avatarPreviewUrl]);

  if (!user) {
    return (
      <div className="rounded-2xl border border-border-default bg-bg-surface p-6">
        <p className="text-sm text-text-secondary">Unable to load your profile.</p>
      </div>
    );
  }

  const status = STATUS_CONFIG[user.customerStatus];
  const displayedAvatarUrl = avatarPreviewUrl ?? user.avatarUrl;

  function resetAvatarSelection() {
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
    } catch {
      // Keep the form open so the user can retry without losing their input.
    } finally {
      setIsSavingProfile(false);
    }
  }

  function handleAvatarChange(file: File) {
    const isValidType = ALLOWED_AVATAR_TYPES.includes(file.type);
    const isValidSize = file.size > 0 && file.size <= MAX_AVATAR_SIZE;

    if (!isValidType || !isValidSize) return;

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
    <section
      aria-labelledby="profile-heading"
      className="rounded-2xl border border-border-default bg-bg-surface p-6 sm:p-8"
    >
      <div className="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex min-w-0 items-center gap-5">
          <div className="relative shrink-0">
            {displayedAvatarUrl ? (
              <img
                src={displayedAvatarUrl}
                alt={`${user.fullName}'s avatar`}
                className="size-20 rounded-full object-cover ring-4 ring-primary-50"
              />
            ) : (
              <div
                role="img"
                aria-label={`${user.fullName}'s avatar`}
                className="flex size-20 items-center justify-center rounded-full bg-primary-100 text-3xl font-bold text-primary-700 ring-4 ring-primary-50"
              >
                {getInitial(user.fullName)}
              </div>
            )}

            {isEditing && (
              <>
                <button
                  type="button"
                  onClick={openAvatarPicker}
                  disabled={isSavingProfile}
                  aria-label="Change avatar"
                  className="absolute -bottom-1 -right-1 flex size-8 items-center justify-center rounded-full border border-primary-200 bg-primary-50 text-primary-700 ring-2 ring-bg-surface transition-colors hover:bg-primary-100 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  <Camera className="size-4" aria-hidden="true" />
                </button>

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
              className="truncate text-2xl font-semibold tracking-tight text-text-primary"
            >
              {user.fullName}
            </h2>
            <p className="mt-0.5 truncate text-sm text-text-secondary">{user.email}</p>

            <span
              className={`mt-3 inline-flex items-center gap-2 rounded-full px-3 py-1 text-xs font-semibold tracking-wide ring-1 ring-inset ${status.badgeClass}`}
            >
              <span className={`size-1.5 rounded-full ${status.dotClass}`} aria-hidden="true" />
              {status.label}
            </span>
          </div>
        </div>

        {!isEditing && (
          <button
            type="button"
            onClick={handleEditToggle}
            className="inline-flex w-fit shrink-0 items-center gap-2 rounded-full border border-primary-200 bg-primary-50 px-5 py-2.5 text-sm font-semibold text-primary-700 transition-colors hover:bg-primary-100"
          >
            <Pencil className="size-4" aria-hidden="true" />
            Edit profile
          </button>
        )}
      </div>

      {isEditing ? (
        <EditProfileForm
          user={user}
          isSubmitting={isSavingProfile}
          selectedAvatarFile={selectedAvatarFile}
          onSubmit={handleProfileSubmit}
          onCancel={handleEditToggle}
        />
      ) : (
        <div className="mt-8 border-t border-border-default pt-6">
          <h3 className="text-base font-semibold tracking-tight text-text-primary">
            Personal information
          </h3>
          <p className="mt-1 text-sm text-text-secondary">
            These details come from your signed-in account.
          </p>

          <dl className="mt-5 grid gap-4 min-[560px]:grid-cols-2">
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
    </section>
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
    container:
      "border-border-default bg-bg-surface hover:border-primary-200 hover:bg-primary-50/40",
    icon: "bg-primary-50 text-primary-600 group-hover:bg-primary-100",
    label: "text-text-muted",
  },
  danger: {
    container:
      "border-border-default bg-bg-surface hover:border-primary-200 hover:bg-primary-50/40",
    icon: "bg-red-100 text-danger",
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
      className={`group flex items-center gap-4 rounded-xl border px-4 py-3.5 transition-colors ${toneClasses.container}`}
    >
      <span
        className={`flex size-10 shrink-0 items-center justify-center rounded-lg transition-colors ${toneClasses.icon}`}
        aria-hidden="true"
      >
        <Icon className="size-5" />
      </span>

      <div className="min-w-0">
        <dt className={`text-[11px] font-medium uppercase tracking-wider ${toneClasses.label}`}>
          {label}
        </dt>
        <dd
          className={`mt-0.5 truncate text-sm ${
            hasValue ? `font-semibold ${valueClassName}` : "font-normal text-text-muted"
          }`}
        >
          {hasValue ? value : fallback}
        </dd>
      </div>
    </div>
  );
}
