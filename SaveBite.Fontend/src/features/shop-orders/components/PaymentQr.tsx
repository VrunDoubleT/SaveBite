import { useEffect, useState } from "react";
import { safePaymentUrl } from "@/features/checkout/utils/checkoutPolicy";
export function PaymentQr({ value, url }: { value?: string; url?: string }) {
  const [image, setImage] = useState("");
  const link = safePaymentUrl(url);
  const simulated = link?.startsWith("/checkout/simulation?") === true;
  useEffect(() => {
    let alive = true;
    const target = value || (link ? new URL(link, window.location.origin).href : "");
    if (target)
      void import("qrcode")
        .then(({ default: QRCode }) => QRCode.toDataURL(target, { width: 240, margin: 2 }))
        .then((src) => {
          if (alive) setImage(src);
        })
        .catch(() => {
          if (alive) setImage("");
        });
    return () => {
      alive = false;
    };
  }, [value, link]);
  return (
    <div className="space-y-2 rounded-lg border border-emerald-200 bg-white p-3 text-center">
      {image && (
        <img
          src={image}
          width={200}
          height={200}
          className="mx-auto"
          alt="Remaining payment QR code"
        />
      )}
      <p className="text-xs text-neutral-500">
        {simulated
          ? "Test QR only. Use the simulation controls below to test the payment outcome."
          : "Scan to pay. Payment is verified automatically before completion."}
      </p>
    </div>
  );
}
