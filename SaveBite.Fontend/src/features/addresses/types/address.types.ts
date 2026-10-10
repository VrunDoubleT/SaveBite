export interface UserAddress {
  id: string;
  label: string | null;
  addressLine: string;
  ward: string | null;
  district: string | null;
  city: string | null;
  latitude: number;
  longitude: number;
  isDefault: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserAddressInput {
  label?: string | null;
  addressLine: string;
  ward?: string | null;
  district?: string | null;
  city?: string | null;
  latitude: number;
  longitude: number;
  isDefault: boolean;
}

export interface UpdateAddressInput {
  label: string | null;
  addressLine: string;
  ward: string | null;
  district: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  isDefault: boolean;
}
