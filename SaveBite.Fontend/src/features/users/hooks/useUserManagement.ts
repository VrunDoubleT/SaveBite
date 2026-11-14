import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { userApi } from "@/features/users/api/userApi";
import type { AdminUser, UserDetailsResponse } from "@/features/users/types/user.types";
import { getApiErrorMessage } from "@/shared/api";
import { useDebounce } from "@/shared/hooks/useDebounce";

export const USER_PAGE_SIZE = 10;

export interface UpdateUserStatusPayload {
  isSuspended: boolean;
  isCustomerProfile: boolean;
  reason: string;
}

export function useUserManagement() {
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page")) || 1;
  const currentSearch = searchParams.get("search") || "";

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [searchInput, setSearchInput] = useState(currentSearch);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Debounce the search input into the URL using useDebounce
  const debouncedSearch = useDebounce(searchInput, 400);

  useEffect(() => {
    const trimmed = debouncedSearch.trim();
    if (trimmed !== currentSearch) {
      setSearchParams(
        (prev) => {
          if (trimmed) prev.set("search", trimmed);
          else prev.delete("search");
          prev.set("page", "1");
          return prev;
        },
        { replace: true },
      );
    }
  }, [debouncedSearch, currentSearch, setSearchParams]);

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