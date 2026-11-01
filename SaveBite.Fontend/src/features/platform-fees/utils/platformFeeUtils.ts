import type { PlatformFeeStatementStatus } from "../types/platformFee.types";

export function normalizeStatementStatus(
  status?: PlatformFeeStatementStatus | number | string | null,
): PlatformFeeStatementStatus {
  if (status === 0 || status === "0" || status === "Draft") return "Draft";
  if (status === 1 || status === "1" || status === "Issued") return "Issued";
  if (status === 2 || status === "2" || status === "Paid") return "Paid";
  if (status === 3 || status === "3" || status === "Overdue") return "Overdue";
  return (status as PlatformFeeStatementStatus) || "Draft";
}
