import { refundApi } from "./refundApi";
import { useAuthStore } from "@/shared/stores/authStore";
import type { RefundImage } from "../types/refund.types";

// Scope successes to the account and order; reuse them only within the current browser session.
const uploaded = new WeakMap<File, Map<string, RefundImage>>();
export async function uploadRefundFiles(
  orderId: string,
  files: File[],
  purpose: "request-evidence" | "destination-qr" | "transfer-proof",
) {
  const ownerId = useAuthStore.getState().user?.id;
  if (!ownerId) throw new Error("Sign in before uploading images.");
  const scope = `${ownerId}:${orderId}:${purpose}`;
  // Wait for each backend upload to finish before submitting the refund command.
  for (const file of files) {
    if (useAuthStore.getState().user?.id !== ownerId)
      throw new Error("Your session changed. Submit again after signing in.");
    let cache = uploaded.get(file);
    if (!cache) {
      cache = new Map();
      uploaded.set(file, cache);
    }
    if (!cache.has(scope)) cache.set(scope, await refundApi.upload(orderId, file, purpose));
  }
  if (useAuthStore.getState().user?.id !== ownerId)
    throw new Error("Your session changed. Submit again after signing in.");
  return files.map((file) => uploaded.get(file)!.get(scope)!);
}
