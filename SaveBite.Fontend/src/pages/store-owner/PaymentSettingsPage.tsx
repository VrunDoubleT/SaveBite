import { useCallback, useEffect, useState } from "react";
import {
  AlertCircle,
  CheckCircle2,
  CircleDashed,
  Landmark,
  RefreshCw,
  ShieldCheck,
} from "lucide-react";

import { ownerShopApi } from "@/features/shops/api/ownerShopApi";

import type {
  UpdateShopPaymentConfigValues,
  ShopPaymentConfigView,
} from "@/features/shops/schemas/ownerShop.schema";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import BankAccountSection from "@/features/shops/components/BankAccountSection";
import PayOSSection from "@/features/shops/components/PayOSSection";

const emptyConfig: ShopPaymentConfigView = {
  bankName: null,
  bankAccountNumber: null,
  bankAccountHolder: null,
  payosClientId: "",
  hasPayosApiKey: false,
  hasPayosChecksumKey: false,
  updatedAt: null,
};

type StatusItemProps = {
  icon: React.ReactNode;
  title: string;
  done: boolean;
  doneText: string;
  todoText: string;
};

function StatusItem({ icon, title, done, doneText, todoText }: StatusItemProps) {
  return (
    <div className="flex items-center gap-3 px-5 py-4">
      <span
        className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${
          done ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"
        }`}
      >
        {icon}
      </span>

      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-slate-900">{title}</p>
        <p className={`text-xs ${done ? "text-emerald-700" : "text-slate-500"}`}>
          {done ? doneText : todoText}
        </p>
      </div>

      {done ? (
        <CheckCircle2 size={20} className="shrink-0 text-emerald-600" />
      ) : (
        <CircleDashed size={20} className="shrink-0 text-slate-400" />
      )}
    </div>
  );
}

function SectionSkeleton() {
  return (
    <div
      className="space-y-5"
      role="status"
      aria-live="polite"
      aria-label="Loading payment settings"
    >
      {[0, 1].map((i) => (
        <div key={i} className="animate-pulse rounded-2xl border border-slate-200 bg-white p-6">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-xl bg-slate-100" />
            <div className="space-y-2">
              <div className="h-3.5 w-36 rounded bg-slate-100" />
              <div className="h-3 w-56 rounded bg-slate-100" />
            </div>
          </div>
          <div className="mt-6 h-28 rounded-xl bg-slate-50" />
        </div>
      ))}
      <span className="sr-only">Loading payment settings…</span>
    </div>
  );
}

export function PaymentSettingsPage() {
  const [config, setConfig] = useState<ShopPaymentConfigView>(emptyConfig);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  const loadConfig = useCallback(async (showLoading = true) => {
    if (showLoading) {
      setLoading(true);
    }

    setLoadError(null);

    try {
      const result = await ownerShopApi.getMyPaymentConfig();
      setConfig(result);
    } catch (error) {
      const message = getApiErrorMessage(error) || "Unable to load payment settings.";

      setLoadError(message);
      toast.error(message);
    } finally {
      if (showLoading) {
        setLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    void loadConfig();
  }, [loadConfig]);

  const handleSave = async (values: UpdateShopPaymentConfigValues): Promise<void> => {
    setSaving(true);

    try {
      await ownerShopApi.updateMyPaymentConfig(values);

      toast.success("Payment settings saved successfully.");

      await loadConfig(false);
    } catch (error) {
      const message = getApiErrorMessage(error) || "Unable to save payment settings.";

      toast.error(message);

      throw error;
    } finally {
      setSaving(false);
    }
  };

  const handleSaveBankAccount = async (bank: {
    bankName: string;
    bankAccountNumber: string;
    bankAccountHolder: string;
  }) => {
    await handleSave({
      ...bank,
      payosClientId: config.payosClientId,
      payosApiKey: "",
      payosChecksumKey: "",
    });
  };

  const handleSavePayOS = async (payos: {
    payosClientId: string;
    payosApiKey: string;
    payosChecksumKey: string;
  }) => {
    await handleSave({
      bankName: config.bankName ?? "",
      bankAccountNumber: config.bankAccountNumber ?? "",
      bankAccountHolder: config.bankAccountHolder ?? "",
      ...payos,
    });
  };

  const bankConfigured = Boolean(
    config.bankName?.trim() && config.bankAccountNumber?.trim() && config.bankAccountHolder?.trim(),
  );

  const payosConfigured =
    Boolean(config.payosClientId) && config.hasPayosApiKey && config.hasPayosChecksumKey;

  const completed = Number(bankConfigured) + Number(payosConfigured);

  const handleRefresh = () => {
    void loadConfig();
  };

  return (
    <div className="space-y-6">
      {/* Page header */}
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Payment Settings</h1>
          <p className="mt-1.5 max-w-xl text-sm leading-6 text-slate-500">
            Manage the bank account and PayOS credentials connected to your shop.
          </p>
        </div>
      </header>

      {/* Setup status */}
      {!loading && !loadError && (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="flex items-center justify-between gap-4 border-b border-slate-100 px-5 py-3">
            <p className="text-sm font-medium text-slate-700">
              {completed === 2
                ? "Your shop is ready to accept payments"
                : `${completed} of 2 steps completed`}
            </p>
            <div
              className="flex w-24 gap-1"
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={2}
              aria-valuenow={completed}
              aria-label="Payment setup progress"
            >
              {[0, 1].map((i) => (
                <span
                  key={i}
                  className={`h-1.5 flex-1 rounded-full ${
                    i < completed ? "bg-emerald-500" : "bg-slate-200"
                  }`}
                />
              ))}
            </div>
          </div>

          <div className="grid divide-y divide-slate-100 sm:grid-cols-2 sm:divide-x sm:divide-y-0">
            <StatusItem
              icon={<Landmark size={20} />}
              title="Bank Account"
              done={bankConfigured}
              doneText="Bank details configured"
              todoText="Add your bank details"
            />
            <StatusItem
              icon={<ShieldCheck size={20} />}
              title="PayOS Credentials"
              done={payosConfigured}
              doneText="Credentials configured"
              todoText="Setup required"
            />
          </div>
        </div>
      )}

      {/* Loading, error, or payment configuration */}
      {loading ? (
        <SectionSkeleton />
      ) : loadError ? (
        <section className="flex flex-col items-center rounded-2xl border border-red-200 bg-white px-6 py-10 text-center">
          <span className="flex h-12 w-12 items-center justify-center rounded-full bg-red-50 text-red-600">
            <AlertCircle size={22} />
          </span>
          <p className="mt-4 text-sm font-semibold text-slate-900">
            Couldn't load payment settings
          </p>
          <p role="alert" className="mt-1 max-w-sm text-sm text-red-600">
            {loadError}
          </p>

          <button
            type="button"
            onClick={handleRefresh}
            disabled={saving}
            className="mt-5 inline-flex items-center gap-2 rounded-xl bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-teal-600/30 disabled:opacity-50"
          >
            <RefreshCw size={15} />
            Try again
          </button>
        </section>
      ) : (
        <div className="space-y-5">
          <BankAccountSection
            initial={{
              bankName: config.bankName ?? "",
              bankAccountNumber: config.bankAccountNumber ?? "",
              bankAccountHolder: config.bankAccountHolder ?? "",
            }}
            saving={saving}
            onSave={handleSaveBankAccount}
          />

          <PayOSSection
            initial={{
              payosClientId: config.payosClientId,
              hasPayosApiKey: config.hasPayosApiKey,
              hasPayosChecksumKey: config.hasPayosChecksumKey,
            }}
            saving={saving}
            onSave={handleSavePayOS}
          />
        </div>
      )}
    </div>
  );
}
