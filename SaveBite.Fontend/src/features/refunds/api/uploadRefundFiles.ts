import { refundApi } from "./refundApi";
import type { RefundImage } from "../types/refund.types";

export type RefundUploadPurpose = "destination-qr" | "transfer-proof";

// Single-image operations upload directly without creating a session or browser draft.
export async function uploadRefundFiles(
  orderId: string,
  files: File[],
  purpose: RefundUploadPurpose,
): Promise<RefundImage[]> {
  if (files.length !== 1) throw new Error("Choose exactly one image.");
  return [await refundApi.upload(orderId, files[0], purpose)];
}