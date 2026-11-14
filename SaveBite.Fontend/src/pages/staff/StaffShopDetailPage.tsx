import { StaffShopDetailSkeleton } from "@/shared/components/ui/PageLoadingSkeleton";
import { useEffect, useState } from "react";
import { ArrowLeft, CalendarDays, Clock3, Mail, MapPin, Store, User } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";

import { staffApi } from "@/features/staff-shops/api/staffApi";
import type { StaffShopDetail } from "@/features/staff-shops/types/staff.types";
import { APP_PATHS } from "@/app/router/paths";
import { formatStaffName } from "@/features/shop-staffs/utils/formatStaffName";

function formatDate(date: string) {
  return new Date(date).toLocaleDateString("en-US", { month: "short", day: "2-digit", year: "numeric" });
}

function formatTime(time: string | null) {
  if (!time) return "Not set";

  return time.slice(0, 5);
}

function getStatusStyle(status: string) {
  switch (status) {
    case "Active":
      return "border-emerald-200 bg-emerald-50 text-emerald-700";

    case "Suspended":
      return "border-amber-200 bg-amber-50 text-amber-700";

    case "Removed":
      return "border-red-200 bg-red-50 text-red-700";

    default:
      return "border-neutral-200 bg-neutral-50 text-neutral-600";
  }
}

function getStatusLabel(status: string) {
  switch (status) {
    case "Active":
      return "Active";

    case "Suspended":
      return "Suspended";

    case "Removed":
      return "Removed";

    default:
      return status;
  }
}

export function StaffShopDetailPage() {
  const { shopId } = useParams<{ shopId: string }>();

  const navigate = useNavigate();

  const [data, setData] = useState<StaffShopDetail | null>(null);
  // no shopId don't need loading
  const [loading, setLoading] = useState(Boolean(shopId));
  const [error, setError] = useState("");

  useEffect(() => {
    if (!shopId) return;

    let cancelled = false;

    async function loadShopDetail(id: string) {
      try {
        const response = await staffApi.getShopDetail(id);

        if (cancelled) return;

        setData(response.data.data);
        setError("");
      } catch (err) {
        if (cancelled) return;

        console.error(err);
        setError("Unable to load store information.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void loadShopDetail(shopId);

    return () => {
      cancelled = true;
    };
  }, [shopId]);

  if (loading) return <StaffShopDetailSkeleton />;

  if (error || !data) {
    return (
      <div className="space-y-6">
        <button
          type="button"
          onClick={() => navigate(APP_PATHS.ACCOUNT_WORKSPACE)}
          className="inline-flex items-center gap-2 text-sm font-medium text-neutral-600 transition hover:text-neutral-900"
        >
          <ArrowLeft size={18} />
          Back to Workspace
        </button>

        <div className="rounded-xl border border-red-200 bg-red-50 p-6 text-sm text-red-700 shadow-2xs">
          {error || "Store not found."}
        </div>
      </div>
    );
  }

  const { shop, staff } = data;

  const fullAddress = [shop.addressLine, shop.ward, shop.district, shop.city]
    .filter(Boolean)
    .join(", ");

  const staffInitial =
    (staff.displayName || "Staff").split(" ").filter(Boolean).slice(-1)[0]?.[0]?.toUpperCase() ||
    "U";

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Shop & Staff Info</h1>
          <p className="mt-1 text-sm text-neutral-500">
            View store profile, operating hours, and your staff account details.
          </p>
        </div>
        <button
          type="button"
          onClick={() => navigate(APP_PATHS.ACCOUNT_WORKSPACE)}
          className="inline-flex items-center gap-2 self-start rounded-lg border border-neutral-200 bg-white px-3.5 py-2 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50"
        >
          <ArrowLeft size={15} />
          Back to Workspace
        </button>
      </div>

      {/* 4 Stats Cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-4">
        {/* Card 1: Shop Status */}
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span
              className={`flex size-10 items-center justify-center rounded-lg ${
                shop.status === "Active"
                  ? "bg-emerald-50 text-emerald-600"
                  : "bg-amber-50 text-amber-600"
              }`}
            >
              <Store className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Store Status</p>
              <p
                className={`text-xl font-bold ${
                  shop.status === "Active" ? "text-emerald-600" : "text-amber-600"
                }`}
              >
                {getStatusLabel(shop.status)}
              </p>
            </div>
          </div>
        </div>

        {/* Card 2: Operating Hours */}
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
              <Clock3 className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Operating Hours</p>
              <p className="text-sm font-bold text-neutral-900 sm:text-base">
                {formatTime(shop.openingTime)} - {formatTime(shop.closingTime)}
              </p>
            </div>
          </div>
        </div>

        {/* Card 3: Staff Role */}
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <User className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Staff Role</p>
              <p className="text-xl font-bold text-neutral-900">Employee</p>
            </div>
          </div>
        </div>

        {/* Card 4: Membership Status */}
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span
              className={`flex size-10 items-center justify-center rounded-lg ${
                staff.status === "Active"
                  ? "bg-emerald-50 text-emerald-600"
                  : "bg-rose-50 text-rose-600"
              }`}
            >
              <CalendarDays className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Account Status</p>
              <p
                className={`text-xl font-bold ${
                  staff.status === "Active" ? "text-emerald-600" : "text-rose-600"
                }`}
              >
                {staff.status}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Shop */}
      <section className="overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-2xs">
        {/* Cover */}
        <div className="relative h-52 overflow-hidden bg-neutral-100">
          {shop.coverImageUrl ? (
            <img src={shop.coverImageUrl} alt={shop.name} className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full items-center justify-center text-neutral-400">
              <Store size={48} />
            </div>
          )}
        </div>

        {/* Shop content */}
        <div className="p-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-center gap-4">
              {/* Logo */}
              <div className="-mt-16 flex size-20 shrink-0 items-center justify-center overflow-hidden rounded-2xl border-4 border-white bg-neutral-100 shadow-sm">
                {shop.logoUrl ? (
                  <img src={shop.logoUrl} alt={shop.name} className="h-full w-full object-cover" />
                ) : (
                  <Store className="text-neutral-400" size={32} />
                )}
              </div>

              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h2 className="text-2xl font-bold text-neutral-900">{shop.name}</h2>

                  <span
                    className={`rounded-full border px-2.5 py-1 text-xs font-medium ${getStatusStyle(
                      shop.status,
                    )}`}
                  >
                    {getStatusLabel(shop.status)}
                  </span>
                </div>

                {shop.description && (
                  <p className="mt-1 text-sm text-neutral-500">{shop.description}</p>
                )}
              </div>
            </div>
          </div>

          {/* Shop information */}
          <div className="mt-6 grid gap-4 md:grid-cols-2">
            <div className="rounded-xl border border-neutral-100 bg-neutral-50/75 p-4">
              <div className="flex items-start gap-3">
                <MapPin size={20} className="mt-0.5 shrink-0 text-emerald-600" />

                <div>
                  <p className="text-xs font-medium text-neutral-400">Store Address</p>

                  <p className="mt-1 text-sm font-medium text-neutral-900">
                    {fullAddress || "Not updated"}
                  </p>
                </div>
              </div>
            </div>

            <div className="rounded-xl border border-neutral-100 bg-neutral-50/75 p-4">
              <div className="flex items-start gap-3">
                <Clock3 size={20} className="mt-0.5 shrink-0 text-emerald-600" />

                <div>
                  <p className="text-xs font-medium text-neutral-400">Opening hours</p>

                  <p className="mt-1 text-sm font-medium text-neutral-900">
                    {formatTime(shop.openingTime)} - {formatTime(shop.closingTime)}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Staff */}
      <section className="space-y-4">
        <div>
          <h2 className="text-xl font-bold text-neutral-900">Staff Account Details</h2>

          <p className="mt-1 text-sm text-neutral-500">
            Account information for employees assigned to this store
          </p>
        </div>

        <div className="rounded-xl border border-neutral-200 bg-white p-6 shadow-2xs">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-center gap-4">
              {/* Avatar */}
              <div className="flex size-14 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-lg font-semibold text-emerald-700">
                {staffInitial}
              </div>

              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="text-lg font-semibold text-neutral-900">
                    {formatStaffName(staff.displayName, staff.staffNickname)}
                  </h3>

                  <span
                    className={`rounded-full border px-2.5 py-1 text-xs font-medium ${getStatusStyle(
                      staff.status,
                    )}`}
                  >
                    {getStatusLabel(staff.status)}
                  </span>
                </div>

                <div className="mt-1 flex items-center gap-2 text-sm text-neutral-500">
                  <Mail size={15} />
                  {staff.userEmail}
                </div>
              </div>
            </div>
          </div>

          <div className="mt-6 grid gap-4 border-t border-neutral-100 pt-6 md:grid-cols-3">
            <div className="rounded-xl border border-neutral-100 bg-neutral-50/75 p-4">
              <div className="flex items-center gap-2 text-neutral-400">
                <User size={16} />

                <span className="text-xs font-medium">Nickname</span>
              </div>

              <p className="mt-2 text-sm font-medium text-neutral-900">
                {staff.staffNickname || "—"}
              </p>
            </div>

            <div className="rounded-xl border border-neutral-100 bg-neutral-50/75 p-4">
              <div className="flex items-center gap-2 text-neutral-400">
                <CalendarDays size={16} />

                <span className="text-xs font-medium">Join Date</span>
              </div>

              <p className="mt-2 text-sm font-medium text-neutral-900">
                {formatDate(staff.joinedAt)}
              </p>
            </div>

            <div className="rounded-xl border border-neutral-100 bg-neutral-50/75 p-4">
              <div className="flex items-center gap-2 text-neutral-400">
                <Store size={16} />

                <span className="text-xs font-medium">Role</span>
              </div>

              <p className="mt-2 text-sm font-medium text-neutral-900">Employee</p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

export default StaffShopDetailPage;
