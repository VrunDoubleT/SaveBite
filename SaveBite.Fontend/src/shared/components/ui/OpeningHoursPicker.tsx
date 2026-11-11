import { useId, useRef } from "react";
import { Clock3, Moon, Sun } from "lucide-react";

type Hours = { openingTime: string; closingTime: string };
type Props = Hours & {
  onChange: (hours: Hours) => void;
  onBlur?: (field: keyof Hours) => void;
  openingError?: string;
  closingError?: string;
  disabled?: boolean;
  allowClear?: boolean;
};
const options = (length: number) =>
  Array.from({ length }, (_, index) => String(index).padStart(2, "0"));
const hours = options(24);
const minutes = options(60);
const selectClass =
  "min-h-12 w-full min-w-0 rounded-xl border border-border-default bg-bg-surface px-3 text-base font-semibold tabular-nums text-text-primary outline-none transition focus:border-primary-500 focus:ring-4 focus:ring-primary-100 disabled:cursor-not-allowed disabled:opacity-50 aria-[invalid=true]:border-danger";

export function OpeningHoursPicker({
  openingTime,
  closingTime,
  onChange,
  onBlur,
  openingError,
  closingError,
  disabled,
  allowClear,
}: Props) {
  const id = useId();
  const lastSchedule = useRef<Hours>({ openingTime: "08:00", closingTime: "22:00" });
  const allDay = openingTime === "00:00" && closingTime === "00:00";
  const complete =
    /^([01]\d|2[0-3]):[0-5]\d$/.test(openingTime) && /^([01]\d|2[0-3]):[0-5]\d$/.test(closingTime);
  const overnight = complete && closingTime <= openingTime;
  const openingMinutes = complete
    ? Number(openingTime.slice(0, 2)) * 60 + Number(openingTime.slice(3, 5))
    : null;
  const closingMinutes = complete
    ? Number(closingTime.slice(0, 2)) * 60 + Number(closingTime.slice(3, 5))
    : null;
  const duration =
    openingMinutes !== null && closingMinutes !== null
      ? (closingMinutes - openingMinutes + 1440) % 1440 || 1440
      : null;
  const durationText =
    duration !== null
      ? `${Math.floor(duration / 60)}h${duration % 60 ? ` ${duration % 60}m` : ""}`
      : "";
  const summary = complete
    ? `${openingTime} today – ${closingTime} ${overnight ? "next day" : "today"}`
    : "Choose an opening time and a closing time.";
  return (
    <fieldset
      disabled={disabled}
      className="min-w-0 space-y-5 rounded-2xl border border-primary-100 bg-primary-50/40 p-4 sm:p-5"
    >
      <legend className="sr-only">Shop opening hours</legend>
      <div className="flex flex-wrap gap-2" role="group" aria-label="Opening hours mode">
        {[
          {
            label: "Set hours",
            active: !allDay,
            value: { openingTime: "08:00", closingTime: "22:00" },
          },
          {
            label: "Open 24/7",
            active: allDay,
            value: { openingTime: "00:00", closingTime: "00:00" },
          },
        ].map((mode) => (
          <button
            key={mode.label}
            type="button"
            aria-pressed={mode.active}
            onClick={() => {
              if (mode.active) return;
              if (mode.label === "Open 24/7") {
                if (complete) lastSchedule.current = { openingTime, closingTime };
                onChange(mode.value);
              } else {
                onChange(lastSchedule.current);
              }
            }}
            className={`min-h-11 rounded-xl border px-4 py-2 text-sm font-semibold transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500 ${mode.active ? "border-primary-600 bg-primary-600 text-white" : "border-border-default bg-bg-surface text-text-secondary hover:border-primary-400"}`}
          >
            {mode.label}
          </button>
        ))}
      </div>
      {!allDay && (
        <>
          <p className="text-xs text-text-secondary">
            24-hour format · Choose a closing time on the same day or the next day, up to 24 hours
            after opening.
          </p>
          <div className="grid gap-5 sm:grid-cols-2">
            {(
              [
                {
                  field: "openingTime",
                  label: "Opening time",
                  value: openingTime,
                  error: openingError,
                  Icon: Sun,
                },
                {
                  field: "closingTime",
                  label: "Closing time",
                  value: closingTime,
                  error: closingError,
                  Icon: Moon,
                },
              ] as const
            ).map(({ field, label, value, error, Icon }) => {
              const [hour = "", minute = ""] = value.split(":");
              const errorId = `${id}-${field}-error`;
              return (
                <div key={field} className="min-w-0">
                  <p
                    id={`${id}-${field}-label`}
                    className="mb-2 flex items-center gap-2 text-sm font-semibold text-text-primary"
                  >
                    <Icon size={16} aria-hidden="true" />
                    {label}
                  </p>
                  {field === "closingTime" && openingTime && (
                    <label className="mb-3 block text-xs text-text-secondary">
                      Closing day
                      <select
                        aria-label="Closing day"
                        className={`${selectClass} mt-1 text-sm`}
                        value={overnight ? "next" : "today"}
                        onChange={(event) =>
                          onChange({
                            openingTime,
                            closingTime: event.target.value === "next" ? "00:00" : "23:59",
                          })
                        }
                      >
                        <option value="today" disabled={openingTime === "23:59"}>
                          Same day
                        </option>
                        <option value="next">Next day</option>
                      </select>
                    </label>
                  )}
                  {field === "openingTime" && (
                    <p className="mb-3 flex min-h-16 items-center text-xs text-text-secondary">
                      Opens today · Closing time must be within the next 24 hours.
                    </p>
                  )}
                  <div
                    role="group"
                    aria-labelledby={`${id}-${field}-label`}
                    className="grid grid-cols-[1fr_auto_1fr] items-center gap-2"
                  >
                    <label className="min-w-0 text-xs text-text-secondary">
                      Hour
                      <select
                        className={`${selectClass} mt-1`}
                        value={hour}
                        aria-invalid={!!error}
                        aria-describedby={error ? errorId : undefined}
                        aria-label={`${label}: hour`}
                        onBlur={() => onBlur?.(field)}
                        onChange={(event) =>
                          onChange({
                            openingTime,
                            closingTime,
                            [field]: `${event.target.value}:${minute || "00"}`,
                            ...(field === "closingTime" &&
                            openingTime &&
                            ((overnight &&
                              `${event.target.value}:${minute || "00"}` > openingTime) ||
                              (!overnight &&
                                `${event.target.value}:${minute || "00"}` <= openingTime))
                              ? { closingTime: `${event.target.value}:${overnight ? "00" : "59"}` }
                              : {}),
                          })
                        }
                      >
                        <option value="" disabled>
                          HH
                        </option>
                        {hours.map((option) => (
                          <option
                            key={option}
                            value={option}
                            disabled={
                              field === "closingTime" &&
                              !!openingTime &&
                              (overnight
                                ? `${option}:00` > openingTime
                                : `${option}:59` <= openingTime)
                            }
                          >
                            {option}
                          </option>
                        ))}
                      </select>
                    </label>
                    <span aria-hidden="true" className="mt-5 text-lg font-semibold text-text-muted">
                      :
                    </span>
                    <label className="min-w-0 text-xs text-text-secondary">
                      Minute
                      <select
                        className={`${selectClass} mt-1`}
                        value={minute}
                        disabled={!hour || disabled}
                        aria-invalid={!!error}
                        aria-describedby={error ? errorId : undefined}
                        aria-label={`${label}: minute`}
                        onBlur={() => onBlur?.(field)}
                        onChange={(event) =>
                          onChange({
                            openingTime,
                            closingTime,
                            [field]: `${hour}:${event.target.value}`,
                          })
                        }
                      >
                        <option value="" disabled>
                          MM
                        </option>
                        {minutes.map((option) => (
                          <option
                            key={option}
                            value={option}
                            disabled={
                              field === "closingTime" &&
                              !!openingTime &&
                              (overnight
                                ? `${hour}:${option}` > openingTime
                                : `${hour}:${option}` <= openingTime)
                            }
                          >
                            {option}
                          </option>
                        ))}
                      </select>
                    </label>
                  </div>
                  {error && (
                    <p id={errorId} role="alert" className="mt-2 text-xs text-danger">
                      {error}
                    </p>
                  )}
                </div>
              );
            })}
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-xs text-text-secondary">Quick setup:</span>
            {[
              ["08:00", "22:00"],
              ["09:00", "21:00"],
              ["18:00", "02:00"],
            ].map(([open, close]) => (
              <button
                key={open}
                type="button"
                onClick={() => onChange({ openingTime: open, closingTime: close })}
                className="min-h-11 rounded-lg border border-border-default bg-bg-surface px-3 py-2 text-xs font-medium tabular-nums text-text-secondary hover:border-primary-400 hover:text-primary-700 focus-visible:outline-2 focus-visible:outline-primary-500"
              >
                {open} – {close}
                {close < open ? " (+1 day)" : ""}
              </button>
            ))}
          </div>
        </>
      )}
      <div
        role="status"
        className="flex items-start gap-2 rounded-xl bg-bg-surface p-3 text-sm text-text-primary"
      >
        <Clock3 size={18} aria-hidden="true" className="mt-0.5 shrink-0 text-primary-600" />
        <div>
          <p className="font-semibold">{summary}</p>
          {complete && (
            <p className="mt-1 text-xs text-text-secondary">
              Open for {durationText} · Maximum 24 hours
            </p>
          )}
          {overnight && (
            <p className="mt-1 text-xs text-text-secondary">
              Closing time is on the following day, no later than {openingTime} (24 hours after
              opening).
            </p>
          )}
        </div>
      </div>
      {allDay && (openingError || closingError) && (
        <p role="alert" className="text-xs text-danger">
          {openingError || closingError}
        </p>
      )}
      {allowClear && (
        <button
          type="button"
          onClick={() => onChange({ openingTime: "", closingTime: "" })}
          className="min-h-11 text-sm font-medium text-text-secondary underline underline-offset-4 hover:text-primary-700 focus-visible:outline-2 focus-visible:outline-primary-500"
        >
          Clear hours — my hours vary
        </button>
      )}
    </fieldset>
  );
}
