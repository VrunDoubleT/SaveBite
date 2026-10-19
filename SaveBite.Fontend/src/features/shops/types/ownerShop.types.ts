export interface OwnerShop {
  id: string;
  name: string;
}

export interface ShopPaymentConfig {
  bankName: string | null;
  bankAccountNumber: string | null;
  bankAccountHolder: string | null;
  payosClientId: string;
  hasPayosApiKey: boolean;
  hasPayosChecksumKey: boolean;
  updatedAt: string | null;
}

export interface UpdateShopPaymentConfigRequest {
  bankName: string | null;
  bankAccountNumber: string | null;
  bankAccountHolder: string | null;
  payosClientId: string;
  payosApiKey: string;
  payosChecksumKey: string;
}
