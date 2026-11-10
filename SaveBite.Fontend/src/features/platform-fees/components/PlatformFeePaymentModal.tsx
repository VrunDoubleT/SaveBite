import { SharedModal } from "@/shared/components/modal/BaseModal";
import { useEffect, useState } from "react";
import { CheckCircle2, Sparkles, Loader2, Copy, Check } from "lucide-react";
import { platformFeeApi } from "../api/platformFeeApi";
import type { PlatformFeePaymentLinkResponse } from "../types/platformFee.types";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

interface PlatformFeePaymentModalProps {
  isOpen: boolean;
  onClose: () => void;
  paymentData: PlatformFeePaymentLinkResponse | null;
  statementPeriodText?: string;
  onPaymentSuccess?: () => void;
}

export function PlatformFeePaymentModal({
  isOpen,
  onClose,
  paymentData,
  statementPeriodText,
  onPaymentSuccess,
}: PlatformFeePaymentModalProps) {
  const [qrResult, setQrResult] = useState<{
    paymentId: string;
    code: string;
    image: string;
  } | null>(null);
  const [copied, setCopied] = useState<boolean>(false);
  const [isSimulating, setIsSimulating] = useState<boolean>(false);

  useEffect(() => {
    if (!paymentData || paymentData.isSimulated || !paymentData.qrCode) return;
    let alive = true;
    const qrTarget = paymentData.qrCode;
    void import("qrcode")
      .then(({ default: QRCode }) =>
        QRCode.toDataURL(qrTarget, {
          width: 280,
          margin: 2,
          color: { dark: "#0f172a", light: "#ffffff" },
        }),
      )
      .then((image) => {
        if (alive) setQrResult({ paymentId: paymentData.paymentId, code: qrTarget, image });
      })
      .catch(() => {
        if (alive) setQrResult(null);
      });

    return () => {
      alive = false;
    };
  }, [paymentData]);

  if (!isOpen || !paymentData) return null;
  const qrImage =
    qrResult?.paymentId === paymentData.paymentId && qrResult.code === paymentData.qrCode
      ? qrResult.image
      : "";

  const formattedAmount = new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(paymentData.amount);

  const handleCopyCode = () => {
    navigator.clipboard.writeText(paymentData.orderCode.toString());
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleSimulatePayment = async () => {
    setIsSimulating(true);
    try {
      await platformFeeApi.simulateWebhook(paymentData.orderCode, paymentData.amount);
      window.dispatchEvent(new Event("store-owner-actions-changed"));
      onPaymentSuccess?.();
      onClose();
    } catch (err) {
      toast.error(getApiErrorMessage(err) || "Failed to simulate payment.");
    } finally {
      setIsSimulating(false);
    }
  };

  return (
    <SharedModal
      isOpen={isOpen}
      onClose={onClose}
      title="Pay SaveBite Platform Fee"
      maxWidthClass="max-w-md"
      headerContent={statementPeriodText ? <p className="text-sm text-slate-600">Period: {statementPeriodText}</p> : undefined}
    >
        {/* Content */}
        <div className="flex-1 overflow-y-auto p-4 sm:p-5 space-y-3.5">
          {/* Amount Callout */}
          <div className="rounded-xl bg-gradient-to-b from-slate-50 to-emerald-50/30 p-3 text-center ring-1 ring-slate-200/80">
            <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
              Amount Due
            </span>
            <div className="mt-0.5 text-xl sm:text-2xl font-black text-emerald-600 tracking-tight">
              {formattedAmount}
            </div>
            <div className="mt-1.5 flex items-center justify-center gap-1.5 text-xs text-slate-600">
              <span>Code:</span>
              <span className="font-mono font-bold text-slate-800">{paymentData.orderCode}</span>
              <button
                type="button"
                onClick={handleCopyCode}
                className="text-emerald-600 hover:text-emerald-700 transition-colors p-0.5"
                title="Copy Code"
              >
                {copied ? (
                  <Check className="h-3.5 w-3.5 text-emerald-600" />
                ) : (
                  <Copy className="h-3.5 w-3.5" />
                )}
              </button>
            </div>
          </div>

          {/* QR Code Container */}
          <div className="flex flex-col items-center justify-center">
            <div className="relative rounded-2xl border-2 border-emerald-500/20 bg-white p-2.5 shadow-md ring-4 ring-emerald-50">
              {paymentData.isSimulated ? (
                <div className="flex h-40 w-40 sm:h-44 sm:w-44 items-center justify-center p-3 text-center text-sm font-semibold text-amber-800">
                  Simulation mode — no bank QR
                </div>
              ) : qrImage ? (
                <img
                  src={qrImage}
                  alt="VietQR PayOS"
                  className="h-40 w-40 sm:h-44 sm:w-44 object-contain rounded-lg"
                />
              ) : (
                <div className="flex h-40 w-40 sm:h-44 sm:w-44 flex-col items-center justify-center gap-2 text-slate-400">
                  <Loader2 className="h-7 w-7 animate-spin text-emerald-600" />
                  <span className="text-[11px]">
                    {paymentData.qrCode ? "Loading QR…" : "Payment QR unavailable"}
                  </span>
                </div>
              )}
            </div>

            {!paymentData.isSimulated && paymentData.checkoutUrl && (
              <a href={paymentData.checkoutUrl} target="_blank" rel="noopener noreferrer"
                className="mt-3 inline-flex min-h-11 items-center rounded-lg px-4 text-sm font-semibold text-emerald-700 underline">
                Open PayOS checkout
              </a>
            )}
            <p className="mt-2 text-center text-[11px] text-slate-500 max-w-xs leading-relaxed">
              {paymentData.isSimulated
                ? "This is a test payment. No real money is transferred."
                : "Scan this QR with your banking app to pay the platform fee."}
            </p>
          </div>

          {/* Development / Simulation Mode Helper */}
          {paymentData.isSimulated && (
            <div className="rounded-xl border border-amber-200 bg-amber-50/70 p-3 text-amber-900">
              <div className="flex items-start gap-2">
                <Sparkles className="h-4 w-4 shrink-0 text-amber-600 mt-0.5" />
                <div className="flex-1 text-xs">
                  <p className="font-bold text-amber-900">Testing Simulation</p>
                  <p className="mt-0.5 text-[11px] text-amber-800/90 leading-normal">
                    Click below to simulate an instant transfer confirmation without actual banking
                    funds.
                  </p>
                  <button
                    type="button"
                    disabled={isSimulating}
                    onClick={handleSimulatePayment}
                    className="mt-2 inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-amber-600 px-3 py-1.5 text-xs font-bold text-white shadow-xs transition-all hover:bg-amber-700 active:scale-98 disabled:opacity-50"
                  >
                    {isSimulating ? (
                      <>
                        <Loader2 className="h-3.5 w-3.5 animate-spin" /> Processing...
                      </>
                    ) : (
                      <>
                        <CheckCircle2 className="h-3.5 w-3.5" /> Confirm Simulation Payment
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="shrink-0 bg-slate-50 px-5 py-2.5 border-t border-slate-100 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl px-4 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-200/70 transition-colors"
          >
            Close
          </button>
        </div>
    </SharedModal>
  );
}
