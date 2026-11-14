import type { Dispatch, SetStateAction } from "react";

interface PickupSchedule {
  end: Date;
  closingTime: Date;
  minimumPickup: Date;
  latestPickup: Date;
}

interface PickupDeadlineFieldProps {
  mode: "deal-end" | "custom";
  setMode: Dispatch<SetStateAction<"deal-end" | "custom">>;
  minutes: number | "";
  setMinutes: Dispatch<SetStateAction<number | "">>;
  schedule: PickupSchedule;
  operatingHours: { openingTime: string | null; closingTime: string | null } | null;
  minimumOrderDeadline?: string | null;
}

function formatTime(date: Date) {
  return `${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
}

export function PickupDeadlineField({
  mode: pickupMode,
  setMode: setPickupMode,
  minutes: pickupMinutes,
  setMinutes: setPickupMinutes,
  schedule,
  operatingHours,
  minimumOrderDeadline,
}: PickupDeadlineFieldProps) {
  const minMinutes = Math.max(
    0,
    Math.ceil((schedule.minimumPickup.getTime() - schedule.end.getTime()) / 60_000),
  );
  const maxMinutes = Math.max(
    0,
    Math.min(60, Math.floor((schedule.latestPickup.getTime() - schedule.end.getTime()) / 60_000)),
  );
  const shopClosingLabel =
    !operatingHours?.openingTime || !operatingHours.closingTime
      ? "Not configured"
      : operatingHours.openingTime.slice(0, 5) === operatingHours.closingTime.slice(0, 5)
        ? "Open 24/7"
        : operatingHours.closingTime.slice(0, 5);
  return (
    <fieldset className="space-y-3 rounded-lg border border-neutral-200 p-3 sm:p-4">
      <legend className="px-1 text-sm font-semibold text-neutral-900">Final pickup deadline</legend>
      <div className="flex flex-wrap gap-x-5 gap-y-1">
        <label className="flex min-h-11 cursor-pointer items-center gap-2 text-xs">
          <input
            type="radio"
            name="pickup-cutoff"
            checked={pickupMode === "deal-end"}
            onChange={() => setPickupMode("deal-end")}
          />
          Same as deal end
        </label>
        <label className="flex min-h-11 cursor-pointer items-center gap-2 text-xs">
          <input
            type="radio"
            name="pickup-cutoff"
            checked={pickupMode === "custom"}
            onChange={() => {
              setPickupMode("custom");
              setPickupMinutes(
                Math.max(
                  minMinutes,
                  Math.min(typeof pickupMinutes === "number" ? pickupMinutes : 15, maxMinutes),
                ),
              );
            }}
          />
          Choose a later pickup time
        </label>
      </div>
      {pickupMode === "custom" && (
        <div className="space-y-3 border-t border-neutral-100 pt-3">
          <div className="flex flex-wrap gap-x-6 gap-y-1 text-xs text-neutral-600">
            <p>
              Shop closes: <strong className="text-neutral-900">{shopClosingLabel}</strong>
            </p>
            <p>
              Max pickup deadline:{" "}
              <strong className="text-emerald-700">
                {formatTime(schedule.latestPickup)}
                {schedule.latestPickup.toDateString() !== schedule.end.toDateString()
                  ? " (+1 day)"
                  : ""}
              </strong>{" "}
              (+
              {maxMinutes} min)
            </p>
          </div>
          <div className="flex flex-wrap items-end gap-3">
            <div className="flex gap-1.5" role="group" aria-label="Extra pickup time presets">
              {[15, 30, 45, 60].map((minutes) => {
                const cutoff = schedule.end.getTime() + minutes * 60_000;
                const disabled =
                  cutoff > schedule.latestPickup.getTime() ||
                  cutoff < schedule.minimumPickup.getTime();
                return (
                  <button
                    key={minutes}
                    type="button"
                    disabled={disabled}
                    aria-pressed={pickupMinutes === minutes}
                    onClick={() => setPickupMinutes(minutes)}
                    className={`min-h-11 rounded-md border px-3 text-xs font-semibold transition disabled:cursor-not-allowed disabled:opacity-40 ${pickupMinutes === minutes ? "border-emerald-600 bg-emerald-600 text-white" : "border-neutral-200 bg-white text-neutral-600 hover:border-emerald-400"}`}
                  >
                    +{minutes}m
                  </button>
                );
              })}
            </div>
            <div className="space-y-1">
              <label
                htmlFor="pickup-minutes"
                className="block text-xs font-medium text-neutral-600"
              >
                Custom minutes
              </label>
              <input
                id="pickup-minutes"
                type="number"
                step="1"
                min={minMinutes}
                max={maxMinutes}
                value={pickupMinutes}
                onChange={(event) => {
                  const value = event.target.value;
                  if (value === "") {
                    setPickupMinutes("");
                    return;
                  }
                  const minutes = Number(value);
                  if (Number.isInteger(minutes) && minutes >= 0 && minutes <= maxMinutes) {
                    setPickupMinutes(minutes);
                  }
                }}
                className="min-h-11 w-28 rounded-md border border-neutral-300 px-3 text-sm"
              />
            </div>
            <p className="py-3 text-xs text-neutral-600">
              Pickup by{" "}
              <strong className="text-neutral-900">
                {Number.isFinite(schedule.closingTime.getTime())
                  ? formatTime(schedule.closingTime)
                  : "—"}
              </strong>
            </p>
          </div>
        </div>
      )}
      {minimumOrderDeadline && (
        <p className="text-xs text-amber-700">
          Existing order deadline: {new Date(minimumOrderDeadline).toLocaleString()}.
        </p>
      )}
    </fieldset>
  );
}
