import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  ShopApplication,
  ShopApplicationForm,
  ShopDocumentUpload,
} from "../types/shopApplication.types";

function appendForm(
  formData: FormData,
  input: ShopApplicationForm,
  logo?: File,
  coverImage?: File,
  documents: ShopDocumentUpload[] = [],
) {
  Object.entries(input).forEach(([key, value]) => {
    if (value !== "" && value !== null && value !== undefined) {
      formData.append(key, String(value));
    }
  });

  if (logo) {
    formData.append("logo", logo);
  }

  if (coverImage) {
    formData.append("coverImage", coverImage);
  }

  documents.forEach(({ type, file }) => {
    formData.append("documents", file);
    formData.append("documentTypes", type);
  });
}

const multipartConfig = {
  headers: {
    "Content-Type": "multipart/form-data",
  },
};

export const shopApplicationApi = {
  async getMine(): Promise<ShopApplication | null> {
    try {
      const response = await httpClient.get<ApiResponse<ShopApplication>>(
        API_ENDPOINTS.SHOP_APPLICATION.ME,
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
      API_ENDPOINTS.SHOP_APPLICATION.HISTORY,
    );

    return response.data.data ?? [];
  },

  async create(
    input: ShopApplicationForm,
    logo?: File,
    coverImage?: File,
    documents: ShopDocumentUpload[] = [],
  ): Promise<ShopApplication> {
    const formData = new FormData();

    appendForm(formData, input, logo, coverImage, documents);

    const response = await httpClient.post<ApiResponse<ShopApplication>>(
      API_ENDPOINTS.SHOP_APPLICATION.BASE,
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
  ): Promise<ShopApplication> {
    const formData = new FormData();

    appendForm(formData, input, logo, coverImage, documents);

    const response = await httpClient.put<ApiResponse<ShopApplication>>(
      API_ENDPOINTS.SHOP_APPLICATION.RESUBMIT(applicationId),
      formData,
      multipartConfig,
    );

    return response.data.data;
  },

  async cancel(applicationId: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.SHOP_APPLICATION.CANCEL(applicationId));
  },
};
