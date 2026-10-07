import { Search, Send, User, X } from "lucide-react";

import { useEffect, useState } from "react";

import { shopStaffApi } from "../api/shopStaffApi";

import type { StaffCandidate } from "../types/shopStaff.types";

interface InviteStaffModalProps {
  loading?: boolean;
  onClose: () => void;
  onSubmit: (userId: string) => void;
}

export function InviteStaffModal({ loading = false, onClose, onSubmit }: InviteStaffModalProps) {
  const [keyword, setKeyword] = useState("");
  const [candidates, setCandidates] = useState<StaffCandidate[]>([]);

  const [searching, setSearching] = useState(false);

  const [selectedUser, setSelectedUser] = useState<StaffCandidate | null>(null);

  useEffect(() => {
    const value = keyword.trim();

    if (!value) {
      setCandidates([]);
      setSelectedUser(null);
      return;
    }

    const timer = window.setTimeout(async () => {
      try {
        setSearching(true);

        const result = await shopStaffApi.searchStaffCandidates(value);

        setCandidates(result);
      } finally {
        setSearching(false);
      }
    }, 350);

    return () => {
      window.clearTimeout(timer);
    };
  }, [keyword]);

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedUser) return;

    onSubmit(selectedUser.userId);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-gray-100 px-6 py-5">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Invite Staff</h2>

            <p className="mt-1 text-sm text-gray-500">Search for a customer account to invite.</p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-lg p-2 text-gray-400 hover:bg-gray-100"
          >
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="space-y-4 px-6 py-5">
            <div className="relative">
              <Search
                size={18}
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
              />

              <input
                value={keyword}
                onChange={(event) => {
                  setKeyword(event.target.value);

                  setSelectedUser(null);
                }}
                placeholder="Search by name or email"
                autoFocus
                disabled={loading}
                className="w-full rounded-xl border border-gray-200 py-2.5 pl-10 pr-4 text-sm outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
              />
            </div>

            {searching && <p className="text-sm text-gray-500">Searching...</p>}

            {!searching && keyword.trim() && candidates.length === 0 && (
              <p className="text-sm text-gray-500">No available account found.</p>
            )}

            <div className="max-h-64 space-y-2 overflow-y-auto">
              {candidates.map((user) => {
                const selected = selectedUser?.userId === user.userId;

                return (
                  <button
                    key={user.userId}
                    type="button"
                    onClick={() => setSelectedUser(user)}
                    className={`flex w-full items-center gap-3 rounded-xl border p-3 text-left transition ${
                      selected
                        ? "border-emerald-500 bg-emerald-50"
                        : "border-gray-200 hover:bg-gray-50"
                    }`}
                  >
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gray-100 text-gray-500">
                      <User size={18} />
                    </div>

                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-gray-900">
                        {user.fullName}
                      </p>

                      <p className="truncate text-sm text-gray-500">{user.email}</p>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="flex justify-end gap-3 border-t border-gray-100 bg-gray-50 px-6 py-4">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={loading || !selectedUser}
              className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
            >
              <Send size={16} />

              {loading ? "Sending..." : "Send invitation"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
