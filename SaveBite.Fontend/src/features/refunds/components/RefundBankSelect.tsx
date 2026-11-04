import { authInputStyle } from "@/shared/components/ui/inputStyles";
import { useEffect, useState } from "react";
import { banks, type Bank } from "../api/refundBanks";
// Resolve the bank routing identifier internally; customers only choose a bank by name.
export function RefundBankSelect({
  value,
  disabled,
  onChange,
  validationError,
}: {
  validationError?: string;
  value: string;
  disabled?: boolean;
  onChange: (bin: string, name: string) => void;
}) {
  const [items, setItems] = useState<Bank[]>([]);
  const [error, setError] = useState(false);
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let alive = true;
    void banks()
      .then((data) => {
        if (alive) {
          setItems(data);
          setError(false);
        }
      })
      .catch(() => {
        if (alive) setError(true);
      });
    return () => {
      alive = false;
    };
  }, [attempt]);
  return (
    <label className="block text-caption font-medium">
      Bank
      <select
        required
        value={value}
        disabled={disabled || items.length === 0}
        aria-invalid={!!validationError}
        className={`${authInputStyle} mt-2 min-h-11 disabled:bg-neutral-100`}
        onChange={(event) => {
          const bank = items.find((item) => item.bin === event.target.value);
          onChange(bank?.bin ?? "", bank?.name ?? "");
        }}
      >
        <option value="">
          {error ? "Bank list unavailable" : items.length ? "Select your bank" : "Loading banks…"}
        </option>
        {items.map((bank) => (
          <option key={bank.bin} value={bank.bin}>
            {bank.shortName} — {bank.name}
          </option>
        ))}
      </select>
      {validationError && (
        <p role="alert" className="mt-1 text-caption text-danger-strong">
          {validationError}
        </p>
      )}
      {error && (
        <button
          type="button"
          disabled={disabled}
          className="mt-1 text-caption text-primary-700 underline"
          onClick={() => setAttempt((value) => value + 1)}
        >
          Retry bank list
        </button>
      )}
    </label>
  );
}
