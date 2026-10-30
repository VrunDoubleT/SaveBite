import { useId, useState, type FormEvent } from "react";
import { AlertCircle, ArrowRight, Ban, CheckCircle2, Loader2, PauseCircle } from "lucide-react";
import type { LucideIcon } from "lucide-react";

import { shopApi } from "../api/shopApi";
import type { ShopStatus } from "../types/ownerShop.types";

const REASON_MIN = 5;
const MAX_LENGTH = 1000;

type StatusOption = {
  value: ShopStatus;
  label: string;
  icon: LucideIcon;
  summary: string;
  pill: string;
  text: string;
  iconBox: string;
  checkedTile: string;
  submit: string;
};

const STATUSES: StatusOption[] = [
  {
    value: "Active",
    label: "Active",
    icon: CheckCircle2,
    summary: "The shop can operate normally.",
    pill: "bg-green-50 text-green-800 ring-1 ring-inset ring-green-200",
    text: "text-green-700",
    iconBox: "bg-green-100 text-green-700",
    checkedTile:
      "peer-checked:border-green-500 peer-checked:bg-green-50 peer-checked:ring-1 peer-checked:ring-green-500",
    submit: "bg-green-600 text-white hover:bg-green-700 focus-visible:ring-green-500",
  },
  {
    value: "Paused",
    label: "Paused",
    icon: PauseCircle,
    summary: "Operations stop temporarily. Can be reversed.",
    pill: "bg-amber-50 text-amber-900 ring-1 ring-inset ring-amber-200",
    text: "text-amber-700",
    iconBox: "bg-amber-100 text-amber-700",
    checkedTile:
      "peer-checked:border-amber-500 peer-checked:bg-amber-50 peer-checked:ring-1 peer-checked:ring-amber-500",
    submit: "bg-amber-400 text-amber-950 hover:bg-amber-500 focus-visible:ring-amber-500",
  },
  {
    value: "Suspended",
    label: "Suspended",
    icon: Ban,
    summary: "The shop is blocked for a policy violation.",
    pill: "bg-red-50 text-red-800 ring-1 ring-inset ring-red-200",
    text: "text-red-700",
    iconBox: "bg-red-100 text-red-700",
    checkedTile:
      "peer-checked:border-red-500 peer-checked:bg-red-50 peer-checked:ring-1 peer-checked:ring-red-500",
    submit: "bg-red-600 text-white hover:bg-red-700 focus-visible:ring-red-500",
  },
];

const fieldClass =
  "mt-2 block w-full resize-y rounded-lg border border-border-default bg-white px-3 py-2.5 text-sm text-text-primary placeholder:text-text-muted transition focus:border-primary-500 focus:outline-none focus:ring-4 focus:ring-primary-500/15 disabled:cursor-not-allowed disabled:bg-neutral-50";

interface ShopModerationActionsProps {
  shopId: string;
  currentStatus: ShopStatus;
  currentStatusReason?: string | null;
  onUpdated: (message?: string) => void;
}

function getAllowedStatuses(currentStatus: ShopStatus): ShopStatus[] {
  switch (currentStatus) {
    case "Active":
      return ["Paused", "Suspended"];

    case "Paused":
      return ["Active", "Suspended"];

    case "Suspended":
      return ["Active"];

    default:
      return [];
  }
}

function StatusPill({ option, label }: { option?: StatusOption; label: string }) {
  const Icon = option?.icon;

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-md px-2 py-0.5 text-xs font-semibold ${
        option?.pill ?? "bg-neutral-100 text-text-secondary ring-1 ring-inset ring-border-default"
      }`}
    >
      {Icon && <Icon className="size-3.5" aria-hidden="true" />}
      {label}
    </span>
  );
}

export function ShopModerationActions({
  shopId,
  currentStatus,
  currentStatusReason,
  onUpdated,
}: ShopModerationActionsProps) {
  const id = useId();

  const [selectedStatus, setSelectedStatus] = useState<ShopStatus | null>(null);
  const [reason, setReason] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const allowedStatuses = getAllowedStatuses(currentStatus);
  const availableOptions = STATUSES.filter((item) => allowedStatuses.includes(item.value));
  const currentOption = STATUSES.find((item) => item.value === currentStatus);
  const selectedOption = STATUSES.find((item) => item.value === selectedStatus);

  const requiresReason = selectedStatus === "Paused" || selectedStatus === "Suspended";
  const reasonLength = reason.trim().length;
  const validReason = !requiresReason || (reasonLength >= REASON_MIN && reasonLength <= MAX_LENGTH);
  const valid = selectedStatus !== null && allowedStatuses.includes(selectedStatus) && validReason;
  const tooShort = requiresReason && reasonLength > 0 && reasonLength < REASON_MIN;

  const showCurrentReason = currentStatus === "Paused" || currentStatus === "Suspended";

  const changeStatus = (next: ShopStatus) => {
    setSelectedStatus(next);
    setReason("");
    setError("");
    setMessage("");
  };

  const perform = async () => {
    if (pending || !valid || selectedStatus === null) {
      return;
    }

    setPending(true);
    setError("");
    setMessage("");

    try {
      const result = await shopApi.updateModerationStatus(
        shopId,
        selectedStatus,
        requiresReason ? reason.trim() : undefined,
      );

      setMessage(result.message);
      setSelectedStatus(null);
      setReason("");

      onUpdated(result.message);
    } catch {
      setError("Unable to update shop status.");
    } finally {
      setPending(false);
    }
  };

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (pending || !valid) {
      return;
    }

    void perform();
  };

  return (
    <form onSubmit={submit} noValidate className="flex min-h-0 flex-1 flex-col">
      <div className="min-h-0 flex-1 space-y-7 overflow-y-auto px-6 py-6">
        <div className="space-y-2">
          <p className="text-sm text-text-secondary">Currently</p>

          <div className="flex items-center gap-2">
            <StatusPill option={currentOption} label={currentStatus} />
          </div>

          {showCurrentReason && (
            <p className="whitespace-pre-wrap break-words border-l-2 border-neutral-300 pl-3 text-sm text-text-secondary">
              {currentStatusReason?.trim() || "The status reason is not available."}
            </p>
          )}
        </div>

        <fieldset disabled={pending} className="space-y-3">
          <legend className="mb-3 text-sm font-semibold text-text-primary">Change status to</legend>

          {availableOptions.length === 0 ? (
            <p className="rounded-lg bg-neutral-50 px-3 py-3 text-sm text-text-secondary">
              No status changes are available for this shop.
            </p>
          ) : (
            <div
              className={`grid gap-3 ${availableOptions.length > 1 ? "grid-cols-2" : "grid-cols-1"}`}
            >
              {availableOptions.map(
                ({ value, label, icon: Icon, summary, iconBox, checkedTile }) => (
                  <label key={value} className="relative block cursor-pointer">
                    <input
                      type="radio"
                      name={`${id}-status`}
                      value={value}
                      checked={selectedStatus === value}
                      onChange={() => changeStatus(value)}
                      className="peer sr-only"
                    />

                    <span
                      className={`flex h-full flex-col gap-3 rounded-xl border border-border-default bg-white p-4 transition hover:border-neutral-300 peer-focus-visible:ring-2 peer-focus-visible:ring-primary-500 peer-focus-visible:ring-offset-2 peer-disabled:cursor-not-allowed peer-disabled:opacity-60 ${checkedTile}`}
                    >
                      <span
                        className={`flex size-9 items-center justify-center rounded-lg ${iconBox}`}
                      >
                        <Icon className="size-5" aria-hidden="true" />
                      </span>

                      <span>
                        <span className="block text-sm font-semibold text-text-primary">
                          {label}
                        </span>
                        <span className="mt-1 block text-xs leading-relaxed text-text-secondary">
                          {summary}
                        </span>
                      </span>
                    </span>
                  </label>
                ),
              )}
            </div>
          )}
        </fieldset>

        {requiresReason && (
          <div>
            <label htmlFor={`${id}-reason`} className="text-sm font-semibold text-text-primary">
              Reason <span className="text-red-600">*</span>
            </label>

            <textarea
              id={`${id}-reason`}
              value={reason}
              maxLength={MAX_LENGTH}
              rows={5}
              required
              disabled={pending}
              onChange={(event) => {
                setReason(event.target.value);
              }}
              placeholder="Explain why the shop is being paused or suspended."
              className={fieldClass}
            />

            <div className="mt-1.5 flex justify-between gap-3 text-xs">
              <span className="text-red-600">
                {tooShort ? `Write at least ${REASON_MIN} characters.` : ""}
              </span>

              <span className="tabular-nums text-text-muted">
                {reason.length}/{MAX_LENGTH}
              </span>
            </div>
          </div>
        )}

        {selectedStatus === "Active" && (
          <p className="flex items-center gap-2 rounded-lg bg-green-50 px-3 py-2.5 text-sm text-green-800">
            <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
            Reactivating this shop does not require a reason.
          </p>
        )}
      </div>

      <div className="space-y-3 border-t border-border-default bg-white px-6 py-4">
        <div className="min-h-5 text-sm" aria-live="polite">
          {error ? (
            <p role="alert" className="flex items-center gap-2 text-red-700">
              <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
              {error}
            </p>
          ) : message ? (
            <p role="status" className="flex items-center gap-2 text-green-700">
              <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
              {message}
            </p>
          ) : selectedOption ? (
            <p className="flex flex-wrap items-center gap-2 text-text-secondary">
              <StatusPill option={currentOption} label={currentStatus} />
              <ArrowRight className="size-4 text-text-muted" aria-hidden="true" />
              <StatusPill option={selectedOption} label={selectedOption.label} />
            </p>
          ) : (
            <p className="text-text-muted">Pick a status to continue.</p>
          )}
        </div>

        <button
          type="submit"
          disabled={pending || !valid}
          className={`inline-flex w-full items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-semibold transition focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${
            selectedOption?.submit ?? "bg-neutral-900 text-white"
          }`}
        >
          {pending && (
            <Loader2
              className="size-4 animate-spin motion-reduce:animate-none"
              aria-hidden="true"
            />
          )}

          {pending
            ? "Updating…"
            : selectedOption
              ? `Set to ${selectedOption.label.toLowerCase()}`
              : "Set status"}
        </button>
      </div>
    </form>
  );
}
