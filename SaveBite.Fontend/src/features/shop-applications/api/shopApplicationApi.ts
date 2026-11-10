import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  ShopApplication,
  ShopApplicationForm,
  ShopDocumentUpload,
} from "../types/shopApplication.types";
import type { UploadReference } from "@/shared/uploads/uploadDrafts";

function appendForm(
  formData: FormData,
  input: ShopApplicationForm,
  logo?: File,
  coverImage?: File,
  documents: ShopDocumentUpload[] = [],
  includeFiles = true,
) {
  Object.entries(input).forEach(([key, value]) => {
    if (value !== "" && value !== null && value !== undefined) {
      formData.append(key, String(value));
    }
  });

  if (includeFiles && logo) {
    formData.append("logo", logo);
  }

  if (includeFiles && coverImage) {
    formData.append("coverImage", coverImage);
  }

  documents.forEach(({ type, file }) => {
    if (includeFiles) formData.append("documents", file);
    formData.append("documentTypes", type);
  });
}

const multipartConfig = {
  headers: {
    "Content-Type": "multipart/form-data",
  },
};

export const shopApplicationApi = {
  async verifyPayOs(input: { applicationId?: string; clientId?: string; apiKey?: string; checksumKey?: string }): Promise<void> {
    await httpClient.post("/shop-applications/verify-payos", input, { timeout: 40_000 });
  },
  async getMine(): Promise<ShopApplication | null> {
    try {
      const response = await httpClient.get<ApiResponse<ShopApplication>>(
        API_ENDPOINTS.SHOP_APPLICATIONS.ME,
      );

      return response.data.data;
    } catch (error: any) {
      if (error?.response?.status === 404) {
        return null;
      }

      throw error;
    }
  },

  async getHistory(): Promise<ShopApplication[]> {
    const response = await httpClient.get<ApiResponse<ShopApplication[]>>(
      API_ENDPOINTS.SHOP_APPLICATIONS.HISTORY,
    );

    return response.data.data ?? [];
  },

  async create(
    input: ShopApplicationForm,
    logo?: File,
    coverImage?: File,
    documents: ShopDocumentUpload[] = [],
    uploadReferences: UploadReference[] = [],
  ): Promise<ShopApplication> {
    const formData = new FormData();

    appendForm(formData, input, logo, coverImage, documents, uploadReferences.length === 0);
    if (uploadReferences.length) formData.append("__uploadAssets", JSON.stringify(uploadReferences));

    const response = await httpClient.post<ApiResponse<ShopApplication>>(
      API_ENDPOINTS.SHOP_APPLICATIONS.BASE,
      formData,
      multipartConfig,
    );

    return response.data.data;
  },

  async resubmit(
    applicationId: string,
    input: ShopApplicationForm,
    logo?: File,
    coverImage?: File,
    documents: ShopDocumentUpload[] = [],
    uploadReferences: UploadReference[] = [],
  ): Promise<ShopApplication> {
    const formData = new FormData();

    appendForm(formData, input, logo, coverImage, documents, uploadReferences.length === 0);
    if (uploadReferences.length) formData.append("__uploadAssets", JSON.stringify(uploadReferences));

    const response = await httpClient.post<ApiResponse<ShopApplication>>(
      API_ENDPOINTS.SHOP_APPLICATIONS.RESUBMIT(applicationId),
      formData,
      multipartConfig,
    );

    return response.data.data;
  },

  async cancel(applicationId: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.SHOP_APPLICATIONS.CANCEL(applicationId));
  },
};
