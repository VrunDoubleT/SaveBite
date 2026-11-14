import { useRef, useState } from "react";
import { PaymentModal } from "@/features/payments/components/PaymentPanel";
import { usePaymentUpdates } from "@/features/payments/hooks/usePaymentUpdates";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { ownerShopApi } from "@/features/shops/api/ownerShopApi";
import { platformFeeApi } from "../api/platformFeeApi";
import type { PlatformFeePaymentLinkResponse } from "../types/platformFee.types";
import { normalizeStatementStatus } from "../utils/platformFeeUtils";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

interface Props {
  isOpen: boolean;
  onClose: () => void;
  paymentData: PlatformFeePaymentLinkResponse | null;
  statementPeriodText?: string;
  onPaymentSuccess?: () => void;
}
export function PlatformFeePaymentModal(props: Props) {
  if (!props.isOpen || !props.paymentData) return null;
  return (
    <ActiveFeePayment
      key={props.paymentData.paymentId}
      {...props}
      paymentData={props.paymentData}
    />
  );
}
function ActiveFeePayment({
  onClose,
  paymentData,
  statementPeriodText,
  onPaymentSuccess,
}: Props & { paymentData: PlatformFeePaymentLinkResponse }) {
  const [simulating, setSimulating] = useState(false);
  const completed = useRef(false);
  usePaymentUpdates(
    REALTIME_EVENTS.PLATFORM_FEE_CHANGED,
    async (signal) => {
      const shop = await ownerShopApi.getMyShopProfile();
      const statement = await platformFeeApi.getStatementDetail(shop.id, paymentData.statementId);
      if (
        !signal.aborted &&
        !completed.current &&
        normalizeStatementStatus(statement.status) === "Paid"
      ) {
        completed.current = true;
        window.dispatchEvent(new Event("store-owner-actions-changed"));
        onPaymentSuccess?.();
        onClose();
      }
    },
    (data) => data.statementId === paymentData.statementId,
  );
  async function simulate() {
    setSimulating(true);
    try {
      await platformFeeApi.simulateWebhook(paymentData.orderCode, paymentData.amount);
      onPaymentSuccess?.();
      onClose();
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setSimulating(false);
    }
  }
  return (
    <PaymentModal
      isOpen
      onClose={onClose}
      title="Pay SaveBite platform fee"
      amount={paymentData.amount}
      qrCode={paymentData.qrCode}
      reference={paymentData.orderCode}
      expiresAt={paymentData.expiredAt}
      isSimulated={paymentData.isSimulated}
    >
      {statementPeriodText && (
        <p className="text-sm text-text-secondary">Period: {statementPeriodText}</p>
      )}
      {paymentData.isSimulated && (
        <button
          type="button"
          disabled={simulating}
          onClick={() => void simulate()}
          className="min-h-11 rounded-lg bg-amber-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
        >
          {simulating ? "Processing…" : "Confirm test payment"}
        </button>
      )}
    </PaymentModal>
  );
}
