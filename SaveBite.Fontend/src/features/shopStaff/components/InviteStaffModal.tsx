import { Mail, Send, X } from "lucide-react";
import { useState } from "react";

interface InviteStaffModalProps {
  loading?: boolean;
  onClose: () => void;
  onSubmit: (email: string) => void;
}

export function InviteStaffModal({
  loading = false,
  onClose,
  onSubmit,
}: InviteStaffModalProps) {
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedEmail = email.trim();

    if (!trimmedEmail) {
      setError("Please enter an email address.");
      return;
    }

    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!emailPattern.test(trimmedEmail)) {
      setError("Please enter a valid email address.");
      return;
    }

    setError("");
    onSubmit(trimmedEmail);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-xl">
        {/* Header */}
        <div className="flex items-start justify-between border-b border-gray-100 px-6 py-5">
          <div>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                <Mail size={20} />
              </div>

              <div>
                <h2 className="text-lg font-semibold text-gray-900">
                  Invite Staff
                </h2>

                <p className="mt-0.5 text-sm text-gray-500">
                  Invite a customer to join your shop as staff.
                </p>
              </div>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-lg p-1.5 text-gray-400 transition hover:bg-gray-100 hover:text-gray-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <X size={20} />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit}>
          <div className="px-6 py-6">
            <label
              htmlFor="staff-email"
              className="mb-2 block text-sm font-semibold text-gray-700"
            >
              Customer email
            </label>

            <div className="relative">
              <Mail
                size={18}
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
              />

              <input
                id="staff-email"
                type="email"
                value={email}
                onChange={(event) => {
                  setEmail(event.target.value);

                  if (error) {
                    setError("");
                  }
                }}
                placeholder="customer@example.com"
                disabled={loading}
                autoFocus
                className="w-full rounded-xl border border-gray-200 bg-white py-2.5 pl-10 pr-4 text-sm text-gray-900 outline-none transition placeholder:text-gray-400 focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 disabled:bg-gray-50"
              />
            </div>

            {error && (
              <p className="mt-2 text-sm text-red-600">
                {error}
              </p>
            )}

            <p className="mt-3 text-xs leading-5 text-gray-500">
              The customer will receive a staff invitation
              associated with your shop.
            </p>
          </div>

          {/* Footer */}
          <div className="flex justify-end gap-3 border-t border-gray-100 bg-gray-50 px-6 py-4">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="rounded-xl border border-gray-200 bg-white px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={loading}
              className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
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