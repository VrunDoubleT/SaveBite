export interface Category {
  id: string;
  name: string;
  description?: string | null;
  imageUrl?: string | null;
  isActive: boolean;
}
