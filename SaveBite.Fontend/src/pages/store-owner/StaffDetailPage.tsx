import { useEffect, useState } from "react";
import { ArrowLeft, Mail, UserRound } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { shopStaffApi } from "@/features/shopStaff/api/shopStaffApi";

import type {
  ShopStaff,
  StaffActivityLog as StaffActivityLogType,
} from "@/features/shopStaff/types/shopStaff.types";

import { StaffStatusBadge } from "@/features/shopStaff/components/StaffStatusBadge";
import { StaffActivityLog } from "@/features/shopStaff/components/StaffActivityLog";

import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";

export function StaffDetailPage() {
  const navigate = useNavigate();
  const location = useLocation();

  const { staffId } = useParams<{
    staffId: string;
  }>();

  const passedStaff = (
    location.state as {
      staff?: ShopStaff;
    } | null
  )?.staff;

  const [staff] = useState<ShopStaff | null>(passedStaff ?? null);

  const [logs, setLogs] = useState<StaffActivityLogType[]>([]);

  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!staffId) {
      setLoading(false);
      return;
    }

    void loadActivityLogs(staffId);
  }, [staffId]);

  async function loadActivityLogs(id: string) {
    try {
      setLoading(true);

      const activityLogs = await shopStaffApi.getStaffActivityLogs(id);

      setLogs(activityLogs);
    } catch (error) {
      console.error(error);

      toast.error(getApiErrorMessage(error) || "Unable to load staff activity logs.");

      setLogs([]);
    } finally {
      setLoading(false);
    }
  }

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-40 animate-pulse rounded bg-gray-100" />

        <div className="rounded-2xl border border-gray-200 bg-white p-6">
          <div className="h-6 w-56 animate-pulse rounded bg-gray-100" />

          <div className="mt-3 h-4 w-72 animate-pulse rounded bg-gray-100" />
        </div>

        <div className="h-64 animate-pulse rounded-2xl bg-gray-100" />
      </div>
    );
  }

  if (!staff) {
    return (
      <div className="rounded-2xl border border-gray-200 bg-white p-10 text-center">
        <p className="font-medium text-gray-900">Staff member not found</p>

        <button
          type="button"
          onClick={() => navigate(-1)}
          className="mt-4 text-sm font-medium text-emerald-600 hover:text-emerald-700"
        >
          Go back
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Back */}
      <button
        type="button"
        onClick={() => navigate(-1)}
        className="inline-flex items-center gap-2 text-sm font-medium text-gray-600 transition hover:text-gray-900"
      >
        <ArrowLeft size={17} />
        Back to Staff Management
      </button>

      {/* Staff information */}
      <section className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-700">
              <UserRound size={25} />
            </div>

            <div>
              <h1 className="text-xl font-bold text-gray-900">{staff.displayName}</h1>

              {staff.staffNickname && (
                <p className="mt-0.5 text-sm text-gray-400">{staff.userName}</p>
              )}

              <div className="mt-1 flex items-center gap-2 text-sm text-gray-500">
                <Mail size={15} />
                {staff.userEmail}
              </div>
            </div>
          </div>

          <StaffStatusBadge status={staff.status} />
        </div>

        <div className="mt-6 grid gap-4 border-t border-gray-100 pt-6 sm:grid-cols-3">
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-gray-400">Role</p>

            <p className="mt-1 text-sm font-medium text-gray-900">Staff</p>
          </div>

          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-gray-400">Joined date</p>

            <p className="mt-1 text-sm font-medium text-gray-900">
              {new Date(staff.joinedAt).toLocaleDateString("vi-VN")}
            </p>
          </div>

          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-gray-400">Nickname</p>

            <p className="mt-1 text-sm font-medium text-gray-900">{staff.staffNickname || "—"}</p>
          </div>
        </div>
      </section>

      {/* Activity */}
      <StaffActivityLog logs={logs} />
    </div>
  );
}
