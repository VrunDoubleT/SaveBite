import { StaffDetailSkeleton } from "@/shared/components/ui/PageLoadingSkeleton";
import { useEffect, useState } from "react";
import { ArrowLeft, Mail, UserRound } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router-dom";

import { shopStaffApi } from "@/features/shop-staffs/api/shopStaffApi";

import type {
  ShopStaff,
  StaffActivityLog as StaffActivityLogType,
} from "@/features/shop-staffs/types/shopStaff.types";

import { StaffStatusBadge } from "@/features/shop-staffs/components/StaffStatusBadge";
import {
  ACTIVITY_PAGE_SIZE,
  StaffActivityLog,
} from "@/features/shop-staffs/components/StaffActivityLog";

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
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Date filter (yyyy-MM-dd) + current page; any change refetches from the server.
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [page, setPage] = useState(1);

  // First load shows the page skeleton; later filter/page changes only dim the activity list.
  const [loading, setLoading] = useState(Boolean(staffId));
  const [logsLoading, setLogsLoading] = useState(false);

  useEffect(() => {
    if (!staffId) return;
    // Do not call the API while the range is invalid (from > to); the filter shows the error.
    if (fromDate && toDate && fromDate > toDate) return;

    let cancelled = false;

    async function loadActivityLogs(id: string) {
      setLogsLoading(true);
      try {
        const result = await shopStaffApi.getStaffActivityLogs(id, {
          page,
          pageSize: ACTIVITY_PAGE_SIZE,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
        });

        if (cancelled) return;
        setLogs(result.items);
        setTotalItems(result.totalItems);
        setTotalPages(Math.max(result.totalPages, 1));
      } catch (error) {
        if (cancelled) return;

        console.error(error);

        toast.error(getApiErrorMessage(error) || "Unable to load staff activity logs.");

        setLogs([]);
        setTotalItems(0);
        setTotalPages(1);
      } finally {
        if (!cancelled) {
          setLoading(false);
          setLogsLoading(false);
        }
      }
    }

    void loadActivityLogs(staffId);

    return () => {
      cancelled = true;
    };
  }, [staffId, fromDate, toDate, page]);

  // Changing a filter always goes back to the first page.
  function handleFromDateChange(value: string) {
    setFromDate(value);
    setPage(1);
  }

  function handleToDateChange(value: string) {
    setToDate(value);
    setPage(1);
  }

  function handleClearFilters() {
    setFromDate("");
    setToDate("");
    setPage(1);
  }

  if (loading) return <StaffDetailSkeleton />;

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
              {new Date(staff.joinedAt).toLocaleDateString("en-US", {
                month: "short",
                day: "2-digit",
                year: "numeric",
              })}
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
      <StaffActivityLog
        logs={logs}
        loading={logsLoading}
        fromDate={fromDate}
        toDate={toDate}
        onFromDateChange={handleFromDateChange}
        onToDateChange={handleToDateChange}
        onClearFilters={handleClearFilters}
        page={page}
        totalPages={totalPages}
        totalItems={totalItems}
        onPageChange={setPage}
      />
    </div>
  );
}
