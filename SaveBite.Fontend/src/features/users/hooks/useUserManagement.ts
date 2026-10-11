import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { userApi } from "@/features/users/api/userApi";
import type { AdminUser, UserDetailsResponse } from "@/features/users/types/user.types";
import { getApiErrorMessage } from "@/shared/api";

export const USER_PAGE_SIZE = 5;

export interface UpdateUserStatusPayload {
  isSuspended: boolean;
  isCustomerProfile: boolean;
  reason: string;
}

/**
 * Handles data fetching, search (debounced), pagination (via URL params)
 * and all user actions for the admin user management page.
 */
export function useUserManagement() {
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page")) || 1;
  const currentSearch = searchParams.get("search") || "";

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [searchInput, setSearchInput] = useState(currentSearch);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Debounce the search input into the URL
  useEffect(() => {
    const handler = setTimeout(() => {
      if (searchInput !== currentSearch) {
        setSearchParams(
          (prev) => {
            if (searchInput) prev.set("search", searchInput);
            else prev.delete("search");
            prev.set("page", "1");
            return prev;
          },
          { replace: true },
        );
      }
    }, 500);
    return () => clearTimeout(handler);
  }, [searchInput, currentSearch, setSearchParams]);

  const fetchUsers = useCallback(async () => {
    try {
      const res = await userApi.getUsers({
        page,
        pageSize: USER_PAGE_SIZE,
        search: currentSearch || undefined,
      });
      setUsers(res.items);
      setTotalItems(res.total ?? 0);
      setTotalPages(res.totalPages ?? 1);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    }
  }, [page, currentSearch]);

  useEffect(() => {
    const loadData = async () => await fetchUsers();
    void loadData();
  }, [fetchUsers]);

  const goToPage = (newPage: number) => {
    setSearchParams((prev) => {
      prev.set("page", newPage.toString());
      return prev;
    });
  };

  /** Suspend / reactivate an account or customer profile. Returns true on success. */
  const updateUserStatus = async (
    userId: string,
    payload: UpdateUserStatusPayload,
  ): Promise<boolean> => {
    try {
      await userApi.updateStatus(userId, payload);
      await fetchUsers();
      return true;
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
      return false;
    }
  };

  /** Load the profile + logs of a user. Returns null (and shows an error) on failure. */
  const fetchUserDetails = async (id: string): Promise<UserDetailsResponse | null> => {
    try {
      return await userApi.getUserDetails(id);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
      return null;
    }
  };

  return {
    users,
    totalItems,
    totalPages,
    page,
    searchInput,
    setSearchInput,
    errorMessage,
    clearError: () => setErrorMessage(null),
    goToPage,
    updateUserStatus,
    fetchUserDetails,
  };
}
