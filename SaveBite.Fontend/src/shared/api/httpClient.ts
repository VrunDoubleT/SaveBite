import axios, { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from "axios";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiErrorResponse, ApiResponse } from "@/shared/types";

interface TokenPair {
  accessToken: string;
  refreshToken?: string;
}

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

const API_URL = import.meta.env.VITE_API_URL ?? "/api";
const TOKEN_KEY = "savebite-access-token";
const REFRESH_TOKEN_KEY = "savebite-refresh-token";

export const httpClient = axios.create({
  baseURL: API_URL,
  timeout: 15_000,
  withCredentials: true,
  headers: { "Content-Type": "application/json" },
});

const refreshClient = axios.create({
  baseURL: API_URL,
  timeout: 15_000,
  withCredentials: true,
  headers: { "Content-Type": "application/json" },
});

let refreshRequest: Promise<string> | null = null;

function getTokenPair(response: AxiosResponse<ApiResponse<TokenPair> | TokenPair>): TokenPair {
  const body = response.data;

  return "data" in body ? body.data : body;
}

function clearTokens(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

function notifyUnauthorized(): void {
  clearTokens();
  window.dispatchEvent(new Event("auth:unauthorized"));
}

async function refreshAccessToken(): Promise<string> {
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
  const response = await refreshClient.post<ApiResponse<TokenPair> | TokenPair>(
    API_ENDPOINTS.AUTH.REFRESH,
    refreshToken ? { refreshToken } : undefined,
  );
  const tokens = getTokenPair(response);

  if (!tokens.accessToken) {
    throw new Error("The refresh response did not include an access token.");
  }

  localStorage.setItem(TOKEN_KEY, tokens.accessToken);

  if (tokens.refreshToken) {
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
  }

  return tokens.accessToken;
}

httpClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem(TOKEN_KEY);

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiErrorResponse>) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined;

    if (error.response?.status !== 401 || !originalRequest) {
      return Promise.reject(error);
    }

    // Invalid credentials must be returned to the login form, not treated as
    // an expired authenticated session.
    if (originalRequest.url === API_ENDPOINTS.AUTH.LOGIN) {
      return Promise.reject(error);
    }

    if (originalRequest._retry || originalRequest.url === API_ENDPOINTS.AUTH.REFRESH) {
      notifyUnauthorized();
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      refreshRequest ??= refreshAccessToken().finally(() => {
        refreshRequest = null;
      });

      const accessToken = await refreshRequest;
      originalRequest.headers.Authorization = `Bearer ${accessToken}`;

      return httpClient(originalRequest);
    } catch (refreshError) {
      notifyUnauthorized();
      return Promise.reject(refreshError);
    }
  },
);

export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ApiErrorResponse>(error)) {
    return error.response?.data?.message ?? error.message;
  }

  return error instanceof Error ? error.message : "An unknown error occurred.";
}
export function getApiErrorStatus(
  error: unknown,
): number | undefined {
  if (!axios.isAxiosError(error)) {
    return undefined;
  }

  return error.response?.status;
} 

export function getApiValidationErrors(error: unknown): Record<string, string> {
  if (!axios.isAxiosError<ApiErrorResponse>(error)) return {};

  return Object.fromEntries(
    Object.entries(error.response?.data?.errors ?? {}).map(([key, messages]) => [
      key.charAt(0).toLowerCase() + key.slice(1),
      messages[0],
    ]),
  );
}

export { API_URL, REFRESH_TOKEN_KEY, TOKEN_KEY };
