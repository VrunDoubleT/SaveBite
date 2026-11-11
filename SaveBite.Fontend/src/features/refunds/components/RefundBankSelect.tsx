import { authInputStyle } from "@/shared/components/ui/inputStyles";
import { useEffect, useId, useRef, useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import { banks, type Bank } from "../api/refundBanks";

const normalize = (text: string) =>
  text
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/đ/g, "d");
const bankLabel = (bank: Bank) => `${bank.shortName} — ${bank.name}`;

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
  const id = useId();
  const input = useRef<HTMLInputElement>(null);
  const [items, setItems] = useState<Bank[]>([]);
  const [error, setError] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);
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
  useEffect(() => {
    input.current?.setCustomValidity(value ? "" : "Please choose a bank from the list.");
  }, [value]);
  const selected = items.find((bank) => bank.bin === value);
  const filtered = items.filter((bank) =>
    normalize(`${bank.shortName} ${bank.name} ${bank.code ?? ""} ${bank.bin}`).includes(
      normalize(query.trim()),
    ),
  );
  const unavailable = disabled || items.length === 0;
  const expanded = open && !unavailable;
  const activeIndex = Math.min(active, Math.max(0, filtered.length - 1));
  useEffect(() => {
    if (expanded)
      document.getElementById(`${id}-option-${activeIndex}`)?.scrollIntoView({ block: "nearest" });
  }, [activeIndex, expanded, id, query]);
  function show() {
    setQuery("");
    setActive(0);
    setOpen(true);
  }
  function choose(bank: Bank) {
    onChange(bank.bin, bank.name);
    input.current?.setCustomValidity("");
    setOpen(false);
    setQuery("");
  }
  return (
    <div className="block text-caption font-medium">
      <label htmlFor={id}>Bank</label>
      <div
        className="relative mt-2"
        onBlur={(event) => {
          if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setOpen(false);
        }}
      >
        <input
          ref={input}
          id={id}
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={expanded}
          aria-controls={`${id}-list`}
          aria-activedescendant={
            expanded && filtered.length ? `${id}-option-${activeIndex}` : undefined
          }
          aria-invalid={!!validationError}
          aria-describedby={validationError ? `${id}-error` : undefined}
          autoComplete="off"
          required
          disabled={unavailable}
          value={expanded ? query : selected ? bankLabel(selected) : ""}
          placeholder={
            error
              ? "Bank list unavailable"
              : items.length
                ? "Select or type to search banks…"
                : "Loading banks…"
          }
          className={`${authInputStyle} min-h-11 pr-12 disabled:bg-neutral-100`}
          onFocus={show}
          onClick={() => {
            if (!open) show();
          }}
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
            setOpen(true);
            if (value) onChange("", "");
            input.current?.setCustomValidity("Please choose a bank from the list.");
          }}
          onKeyDown={(event) => {
            if (event.key === "ArrowDown" || event.key === "ArrowUp") {
              event.preventDefault();
              if (!expanded) show();
              else
                setActive((current) =>
                  Math.max(
                    0,
                    Math.min(filtered.length - 1, current + (event.key === "ArrowDown" ? 1 : -1)),
                  ),
                );
            } else if (event.key === "Enter" && expanded) {
              event.preventDefault();
              if (filtered[activeIndex]) choose(filtered[activeIndex]);
            } else if (event.key === "Escape" && expanded) {
              event.preventDefault();
              event.stopPropagation();
              setOpen(false);
            } else if (event.key === "Tab") setOpen(false);
          }}
        />
        <button
          type="button"
          tabIndex={-1}
          disabled={unavailable}
          aria-label={expanded ? "Close bank list" : "Open bank list"}
          className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-neutral-500 disabled:opacity-50"
          onMouseDown={(event) => event.preventDefault()}
          onClick={() => {
            if (expanded) setOpen(false);
            else {
              input.current?.focus();
              show();
            }
          }}
        >
          <ChevronDown
            className={`size-4 transition-transform ${expanded ? "rotate-180" : ""}`}
            aria-hidden
          />
        </button>
        {expanded && (
          <div className="absolute inset-x-0 top-full z-50 mt-1 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-lg">
            <ul
              id={`${id}-list`}
              role="listbox"
              aria-label="Banks"
              className="max-h-64 overflow-y-auto py-1"
            >
              {filtered.map((bank, index) => (
                <li
                  key={bank.bin}
                  id={`${id}-option-${index}`}
                  role="option"
                  aria-selected={bank.bin === value}
                  className={`flex min-h-11 cursor-pointer items-center gap-2 px-3 py-2 text-sm font-normal ${index === activeIndex ? "bg-emerald-50 text-emerald-900" : "text-neutral-700"}`}
                  onMouseDown={(event) => event.preventDefault()}
                  onMouseEnter={() => setActive(index)}
                  onClick={() => choose(bank)}
                >
                  <span className="min-w-0 flex-1">
                    <span className="font-semibold">{bank.shortName}</span>
                    <span className="mt-0.5 block text-xs text-neutral-500">{bank.name}</span>
                  </span>
                  {bank.bin === value && (
                    <Check className="size-4 shrink-0 text-emerald-600" aria-hidden />
                  )}
                </li>
              ))}
            </ul>
            {!filtered.length && (
              <p role="status" className="px-3 py-4 text-sm font-normal text-neutral-500">
                No matching banks.
              </p>
            )}
          </div>
        )}
      </div>
      {validationError && (
        <p id={`${id}-error`} role="alert" className="mt-1 text-caption text-danger-strong">
          {validationError}
        </p>
      )}
      {error && (
        <button
          type="button"
          disabled={disabled}
          className="mt-1 min-h-11 text-caption text-primary-700 underline"
          onClick={() => setAttempt((current) => current + 1)}
        >
          Retry bank list
        </button>
      )}
    </div>
  );
}