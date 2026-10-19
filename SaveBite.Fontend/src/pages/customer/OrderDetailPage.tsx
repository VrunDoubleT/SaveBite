import { useParams } from "react-router-dom";
import { CheckoutOrderReceipt } from "@/features/checkout/components/CheckoutOrderReceipt";
import { useAuthStore } from "@/shared/stores/authStore";

export function OrderDetailPage() {
  const { orderId } = useParams<{ orderId: string }>();
  const userId = useAuthStore((state) => state.user?.id);
  return <CheckoutOrderReceipt key={`${userId}:${orderId}`} orderId={orderId ?? ""} />;
}
