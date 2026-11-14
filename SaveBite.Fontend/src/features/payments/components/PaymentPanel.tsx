import { useEffect, useState, type ReactNode } from "react";
import { Clock3, Copy, Download, Loader2 } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";

export interface PaymentPanelProps {
  amount: number;
  qrCode?: string | null;
  reference?: string | number | null;
  expiresAt?: string;
  isSimulated?: boolean;
  children?: ReactNode;
  padded?: boolean;
}

// The same bank QR and payment instructions are used on pages and in dialogs.
export function PaymentPanel({
  amount,
  qrCode,
  reference,
  expiresAt,
  isSimulated,
  children,
  padded = true,
}: PaymentPanelProps) {
  const [qr, setQr] = useState<{ value: string; image: string } | null>(null);
  const [qrError, setQrError] = useState(false);
  const [copied, setCopied] = useState(false);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    if (!qrCode || isSimulated) return;
    let active = true;
    void import("qrcode")
      .then(({ default: QRCode }) =>
        QRCode.toDataURL(qrCode, { width: 640, margin: 4, errorCorrectionLevel: "M" }),
      )
      .then((image) => {
        if (active) {
          setQr({ value: qrCode, image });
          setQrError(false);
        }
      })
      .catch(() => {
        if (active) setQrError(true);
      });
    return () => {
      active = false;
    };
  }, [qrCode, isSimulated]);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);
  const deadline = expiresAt ? Date.parse(expiresAt) : NaN;
  const seconds = Number.isFinite(deadline)
    ? Math.max(0, Math.ceil((deadline - now) / 1000))
    : null;
  const expired = seconds !== null && seconds <= 0;
  const image = qr && qr.value === qrCode ? qr.image : null;
  if (expired) {
    return (
      <div
        role="status"
        aria-live="polite"
        aria-atomic="true"
        className={`flex w-full flex-col items-center justify-center gap-5 px-6 text-center ${padded ? "min-h-96 py-12" : "min-h-72 py-8"}`}
      >
        <div className="flex size-20 items-center justify-center rounded-full bg-primary-50">
          <Loader2
            size={36}
            className="motion-safe:animate-spin text-primary-700"
            aria-hidden="true"
          />
        </div>
        <div className="max-w-sm space-y-2">
          <p className="text-lg font-semibold text-text-primary">Processing payment</p>
          <p className="text-sm leading-relaxed text-text-secondary">
            Please wait while we confirm your payment result. This screen will update automatically.
          </p>
        </div>
      </div>
    );
  }
  return (
    <div className={padded ? "p-4 sm:p-6" : ""}>
      <div
        className={
          padded
            ? "grid w-full grid-cols-1 items-center gap-4 text-center md:grid-cols-2 md:gap-x-8 lg:gap-x-12"
            : "grid w-full grid-cols-1 items-center gap-3 text-center sm:grid-cols-2 sm:gap-x-6"
        }
      >
        <div
          className={`rounded-xl border border-primary-100 bg-primary-50/60 px-4 py-4 ${padded ? "md:col-start-2 md:row-start-1" : "sm:col-start-2 sm:row-start-1"}`}
        >
          <p className="text-sm text-text-secondary">Amount to pay</p>
          <p className="mt-1 text-3xl font-bold text-primary-700">{money(amount)}</p>
          {reference && (
            <div className="mt-2 flex flex-wrap items-center justify-center gap-x-2 text-sm">
              <span className="break-all">Payment code: {reference}</span>
              <button
                type="button"
                aria-label="Copy payment code"
                className="inline-flex min-h-11 items-center gap-1 rounded-lg px-3 text-primary-700"
                onClick={() => {
                  void navigator.clipboard
                    .writeText(String(reference))
                    .then(() => setCopied(true))
                    .catch(() => setCopied(false));
                }}
              >
                <Copy size={16} aria-hidden="true" />
                {copied ? "Copied" : "Copy"}
              </button>
            </div>
          )}
        </div>
        {seconds !== null && (
          <div
            className={`flex items-center justify-center gap-2 text-sm text-text-secondary ${padded ? "md:col-start-2 md:row-start-2" : "sm:col-start-2 sm:row-start-2"}`}
          >
            <Clock3 size={16} aria-hidden="true" />
            {expired ? (
              "Waiting for final confirmation"
            ) : (
              <>
                <span>Time remaining</span>
                <span className="rounded-md bg-primary-50 px-2 py-1 font-mono font-semibold tabular-nums text-text-primary">
                  {Math.floor(seconds / 60)}:{String(seconds % 60).padStart(2, "0")}
                </span>
              </>
            )}
          </div>
        )}
        <div
          className={`mx-auto flex aspect-square w-full max-w-80 items-center justify-center overflow-hidden rounded-2xl border border-border-default bg-white ${padded ? "md:col-start-1 md:row-start-1 md:row-span-4" : "sm:col-start-1 sm:row-start-1 sm:row-span-4"}`}
        >
          {isSimulated ? (
            <p className="p-6 text-sm text-amber-800">Test payment — no money is transferred.</p>
          ) : image ? (
            <img
              src={image}
              width={320}
              height={320}
              alt="Scan this bank QR to pay"
              className="h-auto w-full"
            />
          ) : qrError || !qrCode ? (
            <p role="status" className="p-6 text-sm text-text-secondary">
              Payment QR unavailable. Please reopen this payment.
            </p>
          ) : (
            <Loader2
              className="animate-spin text-primary-700"
              size={32}
              aria-label="Loading payment QR"
            />
          )}
        </div>
        {!isSimulated && !expired && image && (
          <a
            href={image}
            download={`savebite-payment-${reference ?? "qr"}.png`}
            className={`inline-flex min-h-11 items-center justify-center justify-self-center gap-2 rounded-lg border border-border-default px-5 text-sm font-semibold text-primary-700 transition-colors hover:bg-primary-50 ${padded ? "md:col-start-1 md:row-start-5" : "sm:col-start-1 sm:row-start-5"}`}
          >
            <Download size={18} aria-hidden="true" />
            Save QR image
          </a>
        )}
        <p
          className={`mx-auto max-w-80 text-sm leading-relaxed text-text-secondary ${padded ? "md:col-start-2 md:row-start-3" : "sm:col-start-2 sm:row-start-3"}`}
        >
          {expired
            ? "This page will update automatically when your payment result is confirmed."
            : isSimulated
              ? "Use the test controls below to confirm the payment."
              : "Scan or import this QR in your banking app. Check the recipient and amount before confirming."}
        </p>
        {!expired && (
          <p
            role="status"
            className={`flex items-center justify-center gap-2 rounded-xl bg-primary-50/60 px-3 py-3 text-sm text-primary-800 ${padded ? "md:col-start-2 md:row-start-4" : "sm:col-start-2 sm:row-start-4"}`}
          >
            <Loader2 size={16} className="animate-spin" aria-hidden="true" />
            Waiting for payment confirmation
          </p>
        )}
        {children && (
          <div
            className={padded ? "md:col-start-2 md:row-start-5" : "sm:col-start-2 sm:row-start-5"}
          >
            {children}
          </div>
        )}
      </div>
    </div>
  );
}

export function PaymentModal({
  isOpen,
  onClose,
  title = "Pay with bank QR",
  ...props
}: PaymentPanelProps & { isOpen: boolean; onClose: () => void; title?: string }) {
  return (
    <SharedModal isOpen={isOpen} onClose={onClose} title={title} maxWidthClass="max-w-2xl">
      <PaymentPanel {...props} padded={false} />
    </SharedModal>
  );
}
