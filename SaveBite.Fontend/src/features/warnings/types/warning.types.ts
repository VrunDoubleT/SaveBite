export interface Warning {
  id: string;
  shopId: string;
  productId: string;
  productName: string;
  requiredCorrection: string;
  action: string;
  reason: string | null;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  issuedByUserId: string | null;
  createdAt: string;
  dueAt: string;
  firstEditedAt: string | null;
  resolvedAt: string | null;
  dealBlocked: boolean;
}
export interface WarningPage {
  items: Warning[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface IssueViolationWarning {
  reason: string;
  requiredCorrection: string;
}
