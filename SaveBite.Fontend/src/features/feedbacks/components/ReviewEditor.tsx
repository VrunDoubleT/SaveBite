import { useId, useRef, useState } from "react";
import { Camera, MessageSquareQuote, Trash2 } from "lucide-react";
import { Button, IconButton, MediaImage, Modal } from "@/shared/components/ui";
import { authInputStyle } from "@/shared/components/ui/inputStyles";
import { StarRatingInput } from "./StarRatingInput";
import type { FeedbackReply } from "../types/feedback.types";

export function ReviewEditor({
  rating,
  comment,
  images,
  saving,
  uploading,
  maxLength,
  error,
  onRating,
  onComment,
  onRemove,
  onUpload,
  onSave,
  onCancel,
  reply,
  editCount,
  canEdit,
}: {
  rating: number;
  comment: string;
  images: string[];
  saving: boolean;
  uploading: boolean;
  maxLength: number;
  error?: string;
  onRating: (value: number) => void;
  onComment: (value: string) => void;
  onRemove: (index: number) => void;
  onUpload: (files: FileList | null) => Promise<void>;
  onSave: () => Promise<void>;
  onCancel: () => void;
  reply?: FeedbackReply | null;
  editCount?: number;
  canEdit?: boolean;
}) {
  const id = useId();
  const input = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [photoWarning, setPhotoWarning] = useState<{
    isOpen: boolean;
    selectedCount: number;
    maxAllowed: number;
  }>({ isOpen: false, selectedCount: 0, maxAllowed: 0 });
  const busy = saving || uploading;
  return (
    <>
      <form
        aria-label="Edit review"
        className="space-y-4"
        onSubmit={(event) => {
          event.preventDefault();
          if (!busy) void onSave();
        }}
      >
        {canEdit === false && (
          <div className="rounded-md border border-amber-300 bg-amber-50 p-3 text-xs text-amber-900">
            This review can no longer be edited because it has reached the limit of 3 edits or the 30-day edit period has expired.
          </div>
        )}
        {typeof editCount === "number" && (
          <div className="flex items-center justify-between rounded-md bg-neutral-50 px-3 py-2 text-xs text-text-secondary">
            <span>Edits used: <strong>{editCount}/3</strong></span>
            <span>{Math.max(0, 3 - editCount)} edit(s) remaining (within 30 days)</span>
          </div>
        )}
        <fieldset disabled={busy || canEdit === false} className="min-w-0 space-y-4 disabled:opacity-60">
          <div className="rounded-md bg-accent-50/60 p-4">
            <p className="mb-3 text-body font-semibold text-text-primary">Your rating</p>
            <StarRatingInput value={rating} onChange={onRating} disabled={busy || canEdit === false} size="lg" />
            {rating <= 2 && (
              <div className="mt-3 rounded-md border border-amber-200 bg-amber-50 p-2.5 text-xs text-amber-800">
                We're sorry your experience wasn't ideal. Please leave detailed feedback so the store can address the issue and improve their service.
              </div>
            )}
          </div>
          <div>
            <div className="mb-2 flex items-center justify-between gap-2">
              <label htmlFor={id} className="text-body font-medium text-text-primary">
                Your feedback
              </label>
              <span className="text-caption text-text-muted">
                {comment.length}/{maxLength}
              </span>
            </div>
            <textarea
              id={id}
              rows={4}
              maxLength={maxLength}
              value={comment}
              onChange={(event) => onComment(event.target.value.slice(0, maxLength))}
              className={`${authInputStyle} resize-y text-body`}
              placeholder="Share your experience with this product…"
            />
          </div>
          <div className="space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <p className="text-body font-medium text-text-primary">
                Photos <span className="text-caption text-text-secondary">{images.length}/5</span>
              </p>
              {images.length < 5 && (
                <Button
                  variant="secondary"
                  loading={uploading}
                  onClick={() => input.current?.click()}
                  className="min-h-11"
                >
                  <Camera size={16} aria-hidden="true" />
                  Add photos
                </Button>
              )}
            </div>
            <p className="text-caption text-text-secondary">
              Up to 5 images · 5 MB each. Selected photos upload immediately.
            </p>
            <input
              ref={input}
              type="file"
              accept="image/*"
              multiple
              className="hidden"
              onChange={(event) => {
                const files = event.target.files;
                if (!files || files.length === 0) return;
                const remainingSlots = 5 - images.length;
                if (files.length > remainingSlots) {
                  setPhotoWarning({
                    isOpen: true,
                    selectedCount: files.length,
                    maxAllowed: Math.max(0, remainingSlots),
                  });
                  if (input.current) input.current.value = "";
                  return;
                }
                void onUpload(files).finally(() => {
                  if (input.current) input.current.value = "";
                });
              }}
            />
            <div className="flex flex-wrap gap-3">
              {images.map((url, index) => (
                <div key={`${url}:${index}`} className="relative pt-2 pr-2">
                  <button
                    type="button"
                    aria-label={`View review photo ${index + 1}`}
                    onClick={() => setPreview(url)}
                    className="overflow-hidden rounded-md border border-border-default"
                  >
                    <MediaImage
                      src={url}
                      alt={`Review photo ${index + 1}`}
                      showFallbackLabel={false}
                      className="size-20"
                    />
                  </button>
                  <IconButton
                    variant="secondary"
                    aria-label={`Remove photo ${index + 1}`}
                    onClick={() => onRemove(index)}
                    className="absolute right-0 top-0 !size-11 !bg-bg-surface !text-danger-strong"
                  >
                    <Trash2 size={16} aria-hidden="true" />
                  </IconButton>
                </div>
              ))}
            </div>
          </div>
        </fieldset>
        {reply && (
          <div className="rounded-md border border-primary-100 bg-primary-50/50 p-4">
            <p className="flex flex-wrap items-center gap-2 text-caption font-semibold text-primary-800">
              <MessageSquareQuote size={16} aria-hidden="true" />
              Shop response{" "}
              {reply.repliedByName && <span className="font-normal">· {reply.repliedByName}</span>}
            </p>
            <p className="mt-2 whitespace-pre-wrap text-body text-text-primary [overflow-wrap:anywhere]">
              {reply.content}
            </p>
          </div>
        )}
        {error && (
          <p role="alert" className="rounded-md bg-danger-soft p-3 text-body text-danger-strong">
            {error}
          </p>
        )}
        <div className="flex flex-wrap justify-end gap-3 border-t border-border-default pt-4">
          <Button variant="secondary" disabled={busy} onClick={onCancel} className="min-h-11">
            Cancel
          </Button>
          <Button type="submit" loading={saving} disabled={busy} className="min-h-11">
            Save changes
          </Button>
        </div>
      </form>
      <Modal
        isOpen={!!preview}
        onClose={() => setPreview(null)}
        title="Review photo"
        maxWidthClass="max-w-4xl"
      >
        {preview && (
          <img
            src={preview}
            alt="Review photo enlarged"
            className="mx-auto max-h-[70vh] max-w-full rounded-md object-contain"
          />
        )}
      </Modal>

      {/* Warning popup when selecting > 5 images (FIX-38) */}
      <Modal
        isOpen={photoWarning.isOpen}
        onClose={() => setPhotoWarning({ isOpen: false, selectedCount: 0, maxAllowed: 0 })}
        title="Photo Limit Exceeded"
        maxWidthClass="max-w-md"
      >
        <div className="space-y-4">
          <p className="text-body text-text-secondary">
            You selected <strong>{photoWarning.selectedCount}</strong> photos, but only <strong>{photoWarning.maxAllowed}</strong> more photo{photoWarning.maxAllowed === 1 ? "" : "s"} can be added (maximum 5 photos per review).
          </p>
          <p className="text-caption text-text-muted">
            Please choose up to {photoWarning.maxAllowed} photo{photoWarning.maxAllowed === 1 ? "" : "s"}.
          </p>
          <div className="flex justify-end">
            <Button
              variant="primary"
              onClick={() => setPhotoWarning({ isOpen: false, selectedCount: 0, maxAllowed: 0 })}
            >
              Understand
            </Button>
          </div>
        </div>
      </Modal>
    </>
  );
}
