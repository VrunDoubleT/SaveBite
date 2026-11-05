import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Clock3, KeyRound, RefreshCw, Save, ShieldCheck } from "lucide-react";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";
import type {
  PlatformPayOsConfigHistoryItem,
  PlatformPayOsConfigResponse,
} from "@/features/platform-fees/types/platformFee.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";

const fieldLabels: Record<string, string> = {
  ClientId: "Client ID",
  ApiKey: "API key",
  ChecksumKey: "Checksum key",
  ConfigurationSource: "Initial database setup",
};

const inputClassName =
  "mt-1.5 min-h-11 w-full rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-xs outline-none transition focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20";

const formatDate = (value: string) =>
  new Date(value).toLocaleString("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  });

export function AdminPlatformPayOsConfig() {
  const [config, setConfig] = useState<PlatformPayOsConfigResponse | null>(null);
  const [history, setHistory] = useState<PlatformPayOsConfigHistoryItem[]>([]);
  const [clientId, setClientId] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [checksumKey, setChecksumKey] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      const [current, changes] = await Promise.all([
        platformFeeApi.getPayOsConfig(),
        platformFeeApi.getPayOsConfigHistory(),
      ]);
      setConfig(current);
      setHistory(changes);
      setClientId("");
      setApiKey("");
      setChecksumKey("");
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load PayOS settings.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    try {
      const updated = await platformFeeApi.updatePayOsConfig({
        clientId: clientId.trim() || undefined,
        apiKey: apiKey.trim() || undefined,
        checksumKey: checksumKey.trim() || undefined,
      });
      setConfig(updated);
      setClientId("");
      setApiKey("");
      setChecksumKey("");
      toast.success("PayOS settings updated successfully.");
      try {
        const changes = await platformFeeApi.getPayOsConfigHistory();
        setHistory(changes);
      } catch (error) {
        toast.error(getApiErrorMessage(error) || "Settings were saved, but change history could not be loaded.");
      }
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to update PayOS settings.");
    } finally {
      setSaving(false);
    }
  };

  const configured = Boolean(config?.hasClientId && config.hasApiKey && config.hasChecksumKey);

  return (
    <section className="rounded-3xl border border-slate-200/80 bg-white p-5 shadow-xs sm:p-7">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex items-start gap-3">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-700">
            <KeyRound className="h-5 w-5" aria-hidden="true" />
          </span>
          <div>
            <h2 className="text-lg font-bold text-slate-900">Payment credentials</h2>
            <p className="mt-1 max-w-2xl text-sm leading-6 text-slate-600">
              Manage the PayOS credentials used to create platform-fee payment links and verify webhooks.
              Credentials are encrypted at rest and secret values are never returned by the API.
            </p>
          </div>
        </div>
        <button
          type="button"
          onClick={() => {
            setLoading(true);
            void load();
          }}
          disabled={loading || saving}
          className="inline-flex min-h-11 items-center justify-center gap-2 self-start rounded-xl border border-slate-200 px-3.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <RefreshCw className={`h-4 w-4 ${loading ? "animate-spin" : ""}`} aria-hidden="true" />
          Refresh
        </button>
      </div>

      <div
        className={`mt-5 flex flex-wrap items-center gap-x-4 gap-y-2 rounded-xl border px-4 py-3 text-sm ${
          configured
            ? "border-emerald-200 bg-emerald-50 text-emerald-900"
            : "border-amber-200 bg-amber-50 text-amber-950"
        }`}
        role="status"
      >
        <span className="inline-flex items-center gap-2 font-semibold">
          <ShieldCheck className="h-4 w-4" aria-hidden="true" />
          {configured ? "PayOS is ready" : "PayOS credentials are incomplete"}
        </span>
        {config?.maskedClientId && (
          <span className="font-mono text-slate-700">Client ID: {config.maskedClientId}</span>
        )}
        {config?.updatedAt && (
          <span className="text-slate-700">
            Updated {config.updatedByName ? `by ${config.updatedByName} ` : ""}
            on {formatDate(config.updatedAt)}
          </span>
        )}
      </div>

      <form className="mt-5 space-y-4" onSubmit={handleSubmit}>
        <div className="grid gap-4 md:grid-cols-2">
          <label className="block text-sm font-semibold text-slate-800">
            Client ID
            <input
              className={inputClassName}
              autoComplete="off"
              value={clientId}
              onChange={(event) => setClientId(event.target.value)}
              placeholder={config?.hasClientId ? "Enter a new value to replace it" : "Enter your PayOS Client ID"}
            />
            <span className="mt-1 block text-xs font-normal leading-5 text-slate-500">
              {config?.hasClientId ? `Current value: ${config.maskedClientId}` : "Required if no value is configured."}
            </span>
          </label>
          <label className="block text-sm font-semibold text-slate-800">
            API key
            <input
              className={inputClassName}
              type="password"
              autoComplete="new-password"
              value={apiKey}
              onChange={(event) => setApiKey(event.target.value)}
              placeholder={config?.hasApiKey ? "Leave blank to keep the current key" : "Enter your PayOS API key"}
            />
            <span className="mt-1 block text-xs font-normal leading-5 text-slate-500">
              Secret values cannot be viewed after saving. Leave blank to keep the current key.
            </span>
          </label>
          <label className="block text-sm font-semibold text-slate-800">
            Checksum key
            <input
              className={inputClassName}
              type="password"
              autoComplete="new-password"
              value={checksumKey}
              onChange={(event) => setChecksumKey(event.target.value)}
              placeholder={config?.hasChecksumKey ? "Leave blank to keep the current key" : "Enter your PayOS checksum key"}
            />
            <span className="mt-1 block text-xs font-normal leading-5 text-slate-500">
              Used to verify PayOS responses and payment webhooks.
            </span>
          </label>
        </div>
        <div className="flex flex-col gap-3 border-t border-slate-100 pt-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="max-w-2xl text-xs leading-5 text-slate-500">
            Every change is recorded in the change history and audit log. History stores a masked Client ID and
            changed field names only; API and checksum keys are never stored there.
          </p>
          <button
            type="submit"
            disabled={loading || saving}
            className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-emerald-700 px-5 text-sm font-semibold text-white shadow-xs transition hover:bg-emerald-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-700 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Save className="h-4 w-4" aria-hidden="true" />
            {saving ? "Saving..." : "Save PayOS settings"}
          </button>
        </div>
      </form>

      <div className="mt-7 border-t border-slate-100 pt-5">
        <h3 className="flex items-center gap-2 text-sm font-bold text-slate-900">
          <Clock3 className="h-4 w-4 text-slate-500" aria-hidden="true" />
          Recent changes
        </h3>
        {history.length === 0 ? (
          <p className="mt-3 text-sm text-slate-500">No configuration changes yet.</p>
        ) : (
          <ol className="mt-3 divide-y divide-slate-100">
            {history.map((item) => (
              <li key={item.id} className="flex flex-col gap-2 py-3 text-sm sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="font-semibold text-slate-800">{item.changedByName}</p>
                  <p className="mt-1 text-slate-600">
                    {item.changedFields.map((field) => fieldLabels[field] ?? field).join(", ")}
                  </p>
                  <p className="mt-1 text-xs text-slate-500">
                    Client ID: {item.previousMaskedClientId ?? "—"} → {item.maskedClientId}
                  </p>
                </div>
                <time className="shrink-0 text-xs text-slate-500" dateTime={item.changedAt}>
                  {formatDate(item.changedAt)}
                </time>
              </li>
            ))}
          </ol>
        )}
      </div>
    </section>
  );
}
