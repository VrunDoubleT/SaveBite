import { useEffect, useState } from "react";
import { ArrowLeft, Mail, UserRound } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { shopStaffApi } from "@/features/shop-staffs/api/shopStaffApi";

import type {
  ShopStaff,
  StaffActivityLog as StaffActivityLogType,
} from "@/features/shop-staffs/types/shopStaff.types";

import { StaffStatusBadge } from "@/features/shop-staffs/components/StaffStatusBadge";
import { StaffActivityLog } from "@/features/shop-staffs/components/StaffActivityLog";

import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import { formatStaffName } from "@/features/shop-staffs/utils/formatStaffName";

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

  // No staffId don't need loading
  const [loading, setLoading] = useState(Boolean(staffId));

  useEffect(() => {
    if (!staffId) return;

    let cancelled = false;

    async function loadActivityLogs(id: string) {
      try {
        const activityLogs = await shopStaffApi.getStaffActivityLogs(id);

        if (!cancelled) setLogs(activityLogs);
      } catch (error) {
        if (cancelled) return;

        console.error(error);

        toast.error(getApiErrorMessage(error) || "Unable to load staff activity logs.");

        setLogs([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void loadActivityLogs(staffId);

    return () => {
      cancelled = true;
    };
  }, [staffId]);

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-40 animate-pulse rounded bg-gray-100" />

        <div className="rounded-xl border border-neutral-200 bg-white p-6">
          <div className="h-6 w-56 animate-pulse rounded bg-gray-100" />

          <div className="mt-3 h-4 w-72 animate-pulse rounded bg-gray-100" />
        </div>

        <div className="h-64 animate-pulse rounded-2xl bg-gray-100" />
      </div>
    );
  }

  if (!staff) {
    return (
      <div className="rounded-xl border border-neutral-200 bg-white p-10 text-center">
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
      <section className="rounded-xl border border-neutral-200 bg-white p-6 shadow-2xs">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-700">
              <UserRound size={25} />
            </div>

            <div>
              <h1 className="text-xl font-bold text-gray-900">
                {formatStaffName(staff.displayName, staff.staffNickname)}
              </h1>

              <div className="mt-1 flex items-center gap-2 text-sm text-gray-500">
                <Mail size={15} />
                {staff.userEmail}
              </div>
            </div>
          </div>

          <StaffStatusBadge status={staff.status} />
        </div>

        <div className="mt-6 grid gap-4 border-t border-gray-100 pt-6 sm:grid-cols-3">
          {/* Role */}
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-gray-400">Role</p>

            <p className="mt-1 text-sm font-medium text-gray-900">Staff</p>
          </div>

          {/* Joined date */}
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-gray-400">Joined date</p>

            <p className="mt-1 text-sm font-medium text-gray-900">
              {new Date(staff.joinedAt).toLocaleDateString("en-US", { month: "short", day: "2-digit", year: "numeric" })}
            </p>
          </div>

          {/* Nickname */}
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
