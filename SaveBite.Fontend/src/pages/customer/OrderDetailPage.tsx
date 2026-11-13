import { useParams } from "react-router-dom";
import { CustomerOrderDetail } from "@/features/orders/components/CustomerOrderDetail";
import { useAuthStore } from "@/shared/stores/authStore";

export function OrderDetailPage() {
  const { orderId } = useParams<{ orderId: string }>();
  const userId = useAuthStore((state) => state.user?.id);
  return <CustomerOrderDetail key={`${userId}:${orderId}`} orderId={orderId ?? ""} />;
}
