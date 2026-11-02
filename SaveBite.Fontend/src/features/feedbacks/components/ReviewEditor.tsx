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
}) {
  const id = useId();
  const input = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const busy = saving || uploading;
  return (
    <>
      <form
        aria-label="Edit review"
        className="space-y-5"
        onSubmit={(event) => {
          event.preventDefault();
          if (!busy) void onSave();
        }}
      >
        <fieldset disabled={busy} className="min-w-0 space-y-5 disabled:opacity-60">
          <div className="rounded-md bg-accent-50/60 p-4">
            <p className="mb-3 text-body font-semibold text-text-primary">Your rating</p>
            <StarRatingInput value={rating} onChange={onRating} disabled={busy} size="lg" />
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
                void onUpload(event.target.files).finally(() => {
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
    </>
  );
}
