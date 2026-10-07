import { useEffect, useState } from "react";
import { ArrowLeft, CalendarDays, Clock3, Mail, MapPin, Store, User } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";

import { staffApi } from "@/features/staff/api/staffApi";
import type { StaffShopDetail } from "@/features/staff/types/staff.types";
import { APP_PATHS } from "@/app/router/paths";

function formatDate(date: string) {
  return new Date(date).toLocaleDateString("vi-VN");
}

function formatTime(time: string | null) {
  if (!time) return "Chưa cập nhật";

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
      return "border-gray-200 bg-gray-50 text-gray-600";
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
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function loadShopDetail() {
    if (!shopId) {
      setError("Store not found.");
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError("");

      const response = await staffApi.getShopDetail(shopId);

      setData(response.data.data);
    } catch (error) {
      console.error(error);
      setError("Unable to load store information.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadShopDetail();
  }, [shopId]);

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="h-6 w-40 animate-pulse rounded bg-gray-200" />

        <div className="rounded-2xl border border-gray-200 bg-white p-6">
          <div className="h-40 animate-pulse rounded-xl bg-gray-100" />

          <div className="mt-6 space-y-3">
            <div className="h-6 w-1/3 animate-pulse rounded bg-gray-100" />
            <div className="h-4 w-2/3 animate-pulse rounded bg-gray-100" />
            <div className="h-4 w-1/2 animate-pulse rounded bg-gray-100" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="space-y-6">
        <button
          type="button"
          onClick={() => navigate(APP_PATHS.ACCOUNT_WORKSPACE)}
          className="inline-flex items-center gap-2 text-sm font-medium text-gray-600 transition hover:text-gray-900"
        >
          <ArrowLeft size={18} />
          Back to Workspace
        </button>

        <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700">
          {error || "Store not found."}
        </div>
      </div>
    );
  }

  const { shop, staff } = data;

  const fullAddress = [shop.addressLine, shop.ward, shop.district, shop.city]
    .filter(Boolean)
    .join(", ");

  return (
    <div className="space-y-6">
      {/* Back */}
      {/* Back */}
      <button
        type="button"
        onClick={() => navigate(APP_PATHS.ACCOUNT_WORKSPACE)}
        className="inline-flex items-center gap-2 text-sm font-medium text-gray-600 transition hover:text-gray-900"
      >
        <ArrowLeft size={18} />
        Back to Workspace
      </button>

      {/* Shop */}
      <section className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
        {/* Cover */}
        <div className="relative h-52 overflow-hidden bg-gray-100">
          {shop.coverImageUrl ? (
            <img src={shop.coverImageUrl} alt={shop.name} className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full items-center justify-center text-gray-400">
              <Store size={48} />
            </div>
          )}
        </div>

        {/* Shop content */}
        <div className="p-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-center gap-4">
              {/* Logo */}
              <div className="-mt-16 flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-2xl border-4 border-white bg-gray-100 shadow-sm">
                {shop.logoUrl ? (
                  <img src={shop.logoUrl} alt={shop.name} className="h-full w-full object-cover" />
                ) : (
                  <Store className="text-gray-400" size={32} />
                )}
              </div>

              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h1 className="text-2xl font-bold text-gray-900">{shop.name}</h1>

                  <span
                    className={`rounded-full border px-2.5 py-1 text-xs font-medium ${getStatusStyle(
                      shop.status,
                    )}`}
                  >
                    {getStatusLabel(shop.status)}
                  </span>
                </div>

                {shop.description && (
                  <p className="mt-1 text-sm text-gray-500">{shop.description}</p>
                )}
              </div>
            </div>
          </div>

          {/* Shop information */}
          <div className="mt-6 grid gap-4 md:grid-cols-2">
            <div className="rounded-xl bg-gray-50 p-4">
              <div className="flex items-start gap-3">
                <MapPin size={20} className="mt-0.5 shrink-0 text-emerald-600" />

                <div>
                  <p className="text-xs font-medium text-gray-400">Store Address</p>

                  <p className="mt-1 text-sm font-medium text-gray-900">
                    {fullAddress || "Not updated"}
                  </p>
                </div>
              </div>
            </div>

            <div className="rounded-xl bg-gray-50 p-4">
              <div className="flex items-start gap-3">
                <Clock3 size={20} className="mt-0.5 shrink-0 text-emerald-600" />

                <div>
                  <p className="text-xs font-medium text-gray-400">Opening hours</p>

                  <p className="mt-1 text-sm font-medium text-gray-900">
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
          <h2 className="text-xl font-bold text-gray-900">Staff Information</h2>

          <p className="mt-1 text-sm text-gray-500">
            Staff account information for employees working at the store
          </p>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-center gap-4">
              {/* Avatar */}
              <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-lg font-semibold text-emerald-700">
                {staff.userName.split(" ").filter(Boolean).slice(-1)[0]?.[0]?.toUpperCase() || "U"}
              </div>

              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="text-lg font-semibold text-gray-900">{staff.userName}</h3>

                  <span
                    className={`rounded-full border px-2.5 py-1 text-xs font-medium ${getStatusStyle(
                      staff.status,
                    )}`}
                  >
                    {getStatusLabel(staff.status)}
                  </span>
                </div>

                <div className="mt-1 flex items-center gap-2 text-sm text-gray-500">
                  <Mail size={15} />
                  {staff.userEmail}
                </div>
              </div>
            </div>
          </div>

          <div className="mt-6 grid gap-4 border-t border-gray-100 pt-6 md:grid-cols-3">
            <div className="rounded-xl bg-gray-50 p-4">
              <div className="flex items-center gap-2 text-gray-400">
                <User size={16} />

                <span className="text-xs font-medium">Nickname</span>
              </div>

              <p className="mt-2 text-sm font-medium text-gray-900">{staff.staffNickname || "—"}</p>
            </div>

            <div className="rounded-xl bg-gray-50 p-4">
              <div className="flex items-center gap-2 text-gray-400">
                <CalendarDays size={16} />

                <span className="text-xs font-medium">Join Date</span>
              </div>

              <p className="mt-2 text-sm font-medium text-gray-900">{formatDate(staff.joinedAt)}</p>
            </div>

            <div className="rounded-xl bg-gray-50 p-4">
              <div className="flex items-center gap-2 text-gray-400">
                <Store size={16} />

                <span className="text-xs font-medium">Role</span>
              </div>

              <p className="mt-2 text-sm font-medium text-gray-900">Employee</p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
