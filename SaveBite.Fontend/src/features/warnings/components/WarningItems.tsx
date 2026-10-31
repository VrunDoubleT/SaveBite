import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { CheckCircle2, Clock3, Loader2, Pencil, ShieldCheck, TriangleAlert } from "lucide-react";
import type { Warning } from "../types/warning.types";
import { productApi } from "@/features/products/api/productApi";

const date = (value: string) => new Date(value).toLocaleString("vi-VN");

function WarningCard({ warning: w }: { warning: Warning }) {
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const state = w.resolvedAt ? "resolved" : w.firstEditedAt ? "submitted" : "new";
  const meta = {
    new: {
      label: "Needs your action",
      icon: TriangleAlert,
      color: "border-amber-200 bg-amber-50 text-amber-800",
      border: "border-l-amber-400",
      description: "You have not submitted corrections for this warning yet.",
    },
    submitted: {
      label: "Correction submitted",
      icon: CheckCircle2,
      color: "border-blue-200 bg-blue-50 text-blue-800",
      border: "border-l-blue-500",
      description: "Your changes were recorded. The administrator still needs to review them.",
    },
    resolved: {
      label: "Resolved · approved",
      icon: ShieldCheck,
      color: "border-emerald-200 bg-emerald-50 text-emerald-800",
      border: "border-l-emerald-500",
      description: "The administrator approved your corrections. No further action is required.",
    },
  }[state];
  const Icon = meta.icon;
  const edit = async () => {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await productApi.get(w.productId);
      navigate(`/store-owner/products/${w.productId}`);
    } catch {
      setError("This product is unavailable or could not be loaded. Please try again.");
    } finally {
      setBusy(false);
    }
  };
  return (
    <li
      className={`overflow-hidden rounded-2xl border border-border-default border-l-4 bg-white shadow-sm ${meta.border}`}
    >
      <div className="p-5 sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="min-w-0">
            <h2 className="break-words text-lg font-semibold text-text-primary">
              {w.productName || "Product warning"}
            </h2>
            <p className="mt-1 text-xs text-text-secondary">Issued {date(w.createdAt)}</p>
          </div>
          <span
            className={`inline-flex items-center gap-2 rounded-full border px-3 py-1.5 text-xs font-semibold ${meta.color}`}
          >
            <Icon className="size-4" />
            {meta.label}
          </span>
        </div>
        <p className="mt-3 text-sm text-text-secondary">{meta.description}</p>
        <div
          className={`mt-4 flex flex-wrap items-center gap-2 rounded-xl border px-4 py-3 text-sm ${w.dealBlocked && !w.resolvedAt ? "border-red-200 bg-red-50 text-red-800" : "border-border-default bg-neutral-50 text-text-primary"}`}
        >
          <Clock3 className="size-4" />
          <span>Correction deadline:</span>
          <time dateTime={w.dueAt} className="font-semibold">
            {date(w.dueAt)}
          </time>
          {w.dealBlocked && !w.resolvedAt && (
            <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-semibold">
              Overdue
            </span>
          )}
        </div>
        {w.dealBlocked && !w.resolvedAt && (
          <div className="mt-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            <strong>Opening deals is blocked.</strong> The correction deadline has passed. Submit
            your changes and wait for administrator approval to unlock this product.
          </div>
        )}
        <div className="mt-5 grid gap-4 sm:grid-cols-2">
          <div className="rounded-xl bg-neutral-50 p-4">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-text-secondary">
              Why this warning was sent
            </h3>
            <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-text-primary">
              {w.reason || "No reason provided."}
            </p>
          </div>
          <div className="rounded-xl border border-amber-100 bg-amber-50/50 p-4">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-amber-800">
              What you need to fix
            </h3>
            <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-text-primary">
              {w.requiredCorrection || "Review the product content."}
            </p>
          </div>
        </div>
        <div className="mt-5 flex flex-wrap items-center justify-between gap-4 border-t border-border-default pt-4">
          <div className="space-y-1.5 text-xs text-text-secondary">
            {w.firstEditedAt && (
              <p className="font-medium text-blue-700">
                Correction submitted: {date(w.firstEditedAt)}
              </p>
            )}
            {w.resolvedAt && (
              <p className="font-medium text-emerald-700">Approved: {date(w.resolvedAt)}</p>
            )}
          </div>
          {state !== "resolved" && (
            <button
              type="button"
              disabled={busy}
              onClick={() => void edit()}
              className="inline-flex items-center gap-2 rounded-xl bg-primary-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-primary-800 disabled:opacity-50"
            >
              {busy ? <Loader2 className="size-4 animate-spin" /> : <Pencil className="size-4" />}
              {state === "new" ? "Fix product" : "Continue editing"}
            </button>
          )}
        </div>
        {error && (
          <p role="alert" className="mt-3 text-sm text-red-700">
            {error}
          </p>
        )}
      </div>
    </li>
  );
}

export function WarningItems({ items }: { items: Warning[] }) {
  return (
    <ul className="space-y-4">
      {items.map((w) => (
        <WarningCard key={w.id} warning={w} />
      ))}
    </ul>
  );
}
