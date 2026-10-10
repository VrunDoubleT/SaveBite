import { create } from "zustand";
import { authApi } from "@/features/auth/api/authApi";
import type { AuthRole, AuthUser, LoginCredentials } from "@/features/auth/types/auth.types";
import { REFRESH_TOKEN_KEY, TOKEN_KEY } from "@/shared/api";

export function isAuthRole(role: unknown): role is AuthRole {
  return role === "user" || role === "storeOwner" || role === "staff" || role === "admin";
}

interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  isLoggingOut: boolean;
  login: (credentials: LoginCredentials) => Promise<AuthUser>;
  logout: () => Promise<void>;
  initializeSession: () => Promise<void>;
  refreshUser: () => Promise<AuthUser>;
  clearSession: () => void;
}

function clearStoredSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

let sessionInitialization: Promise<void> | null = null;

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isInitializing: Boolean(localStorage.getItem(TOKEN_KEY)),
  isLoggingOut: false,
  login: async (credentials) => {
    const tokens = await authApi.login(credentials);
    localStorage.setItem(TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);

    try {
      // /me is authoritative: it validates account access and supplies the role.
      const user = await authApi.getMe();
      set({ user, isAuthenticated: true, isInitializing: false });
      return user;
    } catch (error) {
      clearStoredSession();
      set({ user: null, isAuthenticated: false, isInitializing: false });
      throw error;
    }
  },
  logout: async () => {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    set({ isLoggingOut: true });

    try {
      if (refreshToken) await authApi.logout(refreshToken);
    } finally {
      clearStoredSession();
      set({ user: null, isAuthenticated: false, isInitializing: false, isLoggingOut: false });
    }
  },
  initializeSession: () => {
    if (sessionInitialization) return sessionInitialization;

    sessionInitialization = (async () => {
      if (!localStorage.getItem(TOKEN_KEY)) {
        set({ user: null, isAuthenticated: false, isInitializing: false });
        return;
      }

      set({ isInitializing: true });
      try {
        const user = await authApi.getMe();
        set({ user, isAuthenticated: true, isInitializing: false });
      } catch {
        clearStoredSession();
        set({ user: null, isAuthenticated: false, isInitializing: false });
      }
    })().finally(() => {
      sessionInitialization = null;
    });

    return sessionInitialization;
  },
  refreshUser: async () => {
    const user = await authApi.getMe();
    set({ user, isAuthenticated: true, isInitializing: false });  
    return user;
  },
  clearSession: () => {
    clearStoredSession();
    set({ user: null, isAuthenticated: false, isInitializing: false, isLoggingOut: false });
  },
}));

export type { AuthRole, AuthUser } from "@/features/auth/types/auth.types";
