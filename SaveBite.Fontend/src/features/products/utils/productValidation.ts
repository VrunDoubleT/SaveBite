import type { RegisterOptions } from "react-hook-form";

export type ProductInfoValues = { name: string; description: string; categoryId: string };
export const productInfoRules = {
  name: {
    validate: (value: string) => !!value.trim() || "Please enter a product name.",
    maxLength: { value: 200, message: "Product name must not exceed 200 characters." },
  },
  categoryId: { required: "Please select a category." },
  description: {
    maxLength: { value: 10000, message: "Description must not exceed 10,000 characters." },
  },
} satisfies { [K in keyof ProductInfoValues]: RegisterOptions<ProductInfoValues, K> };
