import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";

// Compact preview for the shop workspace; the Show button opens the full payment dialog.
export function PaymentQr({
  value,
  simulated,
  processing,
}: {
  value?: string;
  simulated?: boolean;
  processing?: boolean;
}) {
  const [qr, setQr] = useState<{ value: string; image: string } | null>(null);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    if (!value || simulated) return;
    let active = true;
    void import("qrcode")
      .then(({ default: QRCode }) => QRCode.toDataURL(value, { width: 400, margin: 4 }))
      .then((image) => {
        if (active) {
          setQr({ value, image });
          setFailed(false);
        }
      })
      .catch(() => {
        if (active) setFailed(true);
      });
    return () => {
      active = false;
    };
  }, [value, simulated]);
  const image = qr && qr.value === value ? qr.image : null;
  return (
    <div className="mt-3 space-y-2 rounded-lg border border-border-default bg-white p-3 text-center">
      {processing ? (
        <div
          role="status"
          className="flex min-h-48 flex-col items-center justify-center gap-3 text-sm text-text-secondary"
        >
          <Loader2
            className="motion-safe:animate-spin text-primary-700"
            size={28}
            aria-hidden="true"
          />
          Processing payment…
        </div>
      ) : simulated ? (
        <p className="py-6 text-sm text-amber-800">Test payment — no bank QR.</p>
      ) : image ? (
        <img
          src={image}
          width={200}
          height={200}
          className="mx-auto h-auto max-w-full"
          alt="Scan this bank QR to pay"
        />
      ) : (
        <p role="status" className="py-6 text-sm text-text-secondary">
          {failed || !value ? "Payment QR unavailable." : "Loading payment QR…"}
        </p>
      )}
      {!processing && (
        <p className="text-xs text-text-secondary">
          {simulated
            ? "Open the payment dialog to test the payment."
            : "Scan with your banking app to pay the remaining balance."}
        </p>
      )}
    </div>
  );
}
