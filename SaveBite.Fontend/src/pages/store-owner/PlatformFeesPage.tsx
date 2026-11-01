import { useEffect, useState, useCallback } from "react";
import { Link } from "react-router-dom";
import { Receipt, CreditCard, Filter, RefreshCw, Eye, ShieldAlert } from "lucide-react";
import { ownerShopApi, type OwnerShop } from "@/features/shops/api/ownerShopApi";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";
import type {
  PlatformFeeStatementSummary,
  PlatformFeePaymentLinkResponse,
} from "@/features/platform-fees/types/platformFee.types";
import { normalizeStatementStatus } from "@/features/platform-fees/utils/platformFeeUtils";
import { StatusBadge } from "@/features/platform-fees/components/StatusBadge";
import { PlatformFeePaymentModal } from "@/features/platform-fees/components/PlatformFeePaymentModal";
import { Pagination } from "@/shared/components/pagination";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

export function PlatformFeesPage() {
  const [shop, setShop] = useState<OwnerShop | null>(null);
  const [shopLoading, setShopLoading] = useState(true);

  const [statements, setStatements] = useState<PlatformFeeStatementSummary[]>([]);
  const [loading, setLoading] = useState(true);

  // Filters
  const [selectedYear, setSelectedYear] = useState<number | string>(new Date().getFullYear());
  const [selectedMonth, setSelectedMonth] = useState<string>("All");
  const [selectedStatus, setSelectedStatus] = useState<string>("All");
  const [page, setPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);

  // Payment Modal state
  const [payingStatement, setPayingStatement] = useState<PlatformFeeStatementSummary | null>(null);
  const [paymentData, setPaymentData] = useState<PlatformFeePaymentLinkResponse | null>(null);
  const [isCreatingLink, setIsCreatingLink] = useState<boolean>(false);

  // Load shop profile to get shopId
  const loadShopProfile = useCallback(async () => {
    setShopLoading(true);
    try {
      const data = await ownerShopApi.getMyShopProfile();
      setShop(data);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load shop profile.");
    } finally {
      setShopLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadShopProfile();
  }, [loadShopProfile]);

  // Load statements list (UC-79)
  const loadStatements = useCallback(async () => {
    if (!shop?.id) return;
    setLoading(true);
    try {
      const params = {
        year: selectedYear === "All" ? undefined : Number(selectedYear),
        month: selectedMonth !== "All" ? parseInt(selectedMonth) : undefined,
        status: selectedStatus !== "All" ? selectedStatus : undefined,
        page: page,
        pageNumber: page,
        pageSize: 10,
      };
      const result = await platformFeeApi.getShopStatements(shop.id, params);
      setStatements(result.items || []);
      setTotalPages(result.totalPages || 1);
      setTotalCount(result.totalItems ?? result.totalCount ?? (result.items?.length || 0));
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load fee statements.");
    } finally {
      setLoading(false);
    }
  }, [shop?.id, selectedYear, selectedMonth, selectedStatus, page]);

  useEffect(() => {
    if (shop?.id) {
      void loadStatements();
    }
  }, [shop?.id, loadStatements]);

  // Handle Pay Online (UC-81)
  const handleOpenPayment = async (statement: PlatformFeeStatementSummary) => {
    if (!shop?.id) return;
    setIsCreatingLink(true);
    try {
      const linkData = await platformFeeApi.createPaymentLink(shop.id, statement.id);
      setPayingStatement(statement);
      setPaymentData(linkData);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to initiate payment.");
    } finally {
      setIsCreatingLink(false);
    }
  };

  const handlePaymentSuccess = () => {
    void loadStatements();
    void loadShopProfile();
  };

  const formatVnd = (amount: number) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
    }).format(amount);
  };

  // Compute summary stats
  const unpaidTotal = statements
    .filter((s) => s.status === "Issued" || s.status === "Overdue")
    .reduce((sum, s) => sum + s.totalFeeAmount, 0);

  const hasOverdue = statements.some((s) => s.status === "Overdue");

  return (
    <div className="space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight sm:text-3xl">
            Platform Fees
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            Track periodic commission statements and settle balances online via PayOS
          </p>
        </div>
        <button
          onClick={() => void loadStatements()}
          disabled={loading}
          className="inline-flex items-center gap-2 self-start rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-semibold text-slate-700 shadow-xs transition hover:bg-slate-50 disabled:opacity-50"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${loading ? "animate-spin" : ""}`} />
          Refresh
        </button>
      </div>

      {/* Warning banner if shop is suspended due to overdue fee */}
      {(hasOverdue || shop?.status === "Suspended") && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50/80 p-4 text-rose-900 shadow-xs">
          <div className="flex items-start gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-rose-100 text-rose-700">
              <ShieldAlert className="h-5 w-5" />
            </span>
            <div className="flex-1 text-xs sm:text-sm">
              <h3 className="font-bold text-rose-900">
                {shop?.status === "Suspended"
                  ? "Your store is currently SUSPENDED due to overdue platform fees"
                  : "Attention: You have overdue platform fee statements!"}
              </h3>
              <p className="mt-1 text-rose-800 leading-normal">
                Please settle the overdue balances below to automatically reactivate your store
                operations.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Metrics Row */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs">
          <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Total Unpaid Fees
          </span>
          <div className="mt-2 text-2xl font-black text-rose-600 tracking-tight">
            {formatVnd(unpaidTotal)}
          </div>
          <p className="mt-1 text-xs text-slate-400">Includes Issued & Overdue statements</p>
        </div>

        <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs">
          <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Total Statements
          </span>
          <div className="mt-2 text-2xl font-black text-slate-900 tracking-tight">{totalCount}</div>
          <p className="mt-1 text-xs text-slate-400">
            {selectedYear === "All" ? "All time" : `In year ${selectedYear}`}
          </p>
        </div>

        <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs">
          <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Store Operating Status
          </span>
          <div className="mt-2 flex items-center gap-2">
            <span
              className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold ${
                shop?.status === "Active"
                  ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                  : shop?.status === "Suspended"
                    ? "bg-rose-50 text-rose-700 border border-rose-200"
                    : "bg-slate-100 text-slate-700"
              }`}
            >
              <span
                className={`h-2 w-2 rounded-full ${
                  shop?.status === "Active" ? "bg-emerald-500" : "bg-rose-500"
                }`}
              />
              {shop?.status === "Active"
                ? "Active"
                : shop?.status === "Suspended"
                  ? "Suspended"
                  : shop?.status || "Loading..."}
            </span>
          </div>
          <p className="mt-1 text-xs text-slate-400">System account status</p>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200/80 bg-white p-4 shadow-xs">
        <div className="flex flex-wrap items-center gap-3">
          <span className="flex items-center gap-1.5 text-xs font-bold text-slate-700">
            <Filter className="h-4 w-4 text-slate-400" /> Filters:
          </span>

          {/* Year filter */}
          <select
            value={selectedYear}
            onChange={(e) => {
              setSelectedYear(e.target.value === "All" ? "All" : parseInt(e.target.value));
              setPage(1);
            }}
            className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-semibold text-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
          >
            <option value="All">All Years</option>
            {[2024, 2025, 2026, 2027].map((y) => (
              <option key={y} value={y}>
                Year {y}
              </option>
            ))}
          </select>

          {/* Month filter */}
          <select
            value={selectedMonth}
            onChange={(e) => {
              setSelectedMonth(e.target.value);
              setPage(1);
            }}
            className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-semibold text-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
          >
            <option value="All">All Months</option>
            {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
              <option key={m} value={m}>
                Month {m}
              </option>
            ))}
          </select>

          {/* Status filter */}
          <select
            value={selectedStatus}
            onChange={(e) => {
              setSelectedStatus(e.target.value);
              setPage(1);
            }}
            className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-semibold text-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
          >
            <option value="All">All Statuses</option>
            <option value="Issued">Issued</option>
            <option value="Paid">Paid</option>
            <option value="Overdue">Overdue</option>
          </select>
        </div>

        <div className="text-xs text-slate-400 font-medium">Found {totalCount} statements</div>
      </div>

      {/* Table of Statements */}
      <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50/80 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-200/80">
              <tr>
                <th className="px-5 py-4">Billing Period</th>
                <th className="px-5 py-4">Total Fee Amount</th>
                <th className="px-5 py-4">Status</th>
                <th className="px-5 py-4">Due Date</th>
                <th className="px-5 py-4">Paid Date</th>
                <th className="px-5 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading || shopLoading ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">
                    <RefreshCw className="mx-auto h-6 w-6 animate-spin text-emerald-600 mb-2" />
                    Loading statements data...
                  </td>
                </tr>
              ) : statements.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">
                    <Receipt className="mx-auto h-8 w-8 text-slate-300 mb-2" />
                    No statements found matching the filter criteria.
                  </td>
                </tr>
              ) : (
                statements.map((stmt) => {
                  const normalizedStatus = normalizeStatementStatus(stmt.status);
                  const canPay = normalizedStatus === "Issued" || normalizedStatus === "Overdue";
                  return (
                    <tr key={stmt.id} className="hover:bg-slate-50/60 transition-colors">
                      <td className="px-5 py-4 font-bold text-slate-900">
                        Month {stmt.billingMonth}/{stmt.billingYear}
                      </td>
                      <td className="px-5 py-4 font-extrabold text-slate-900">
                        {formatVnd(stmt.totalFeeAmount)}
                      </td>
                      <td className="px-5 py-4">
                        <StatusBadge status={stmt.status} />
                      </td>
                      <td className="px-5 py-4 text-slate-600">
                        {stmt.dueAt ? new Date(stmt.dueAt).toLocaleDateString("en-US") : "—"}
                      </td>
                      <td className="px-5 py-4 text-slate-600">
                        {stmt.paidAt ? new Date(stmt.paidAt).toLocaleDateString("en-US") : "—"}
                      </td>
                      <td className="px-5 py-4 text-right">
                        <div className="inline-flex items-center gap-2">
                          {/* Detail Button */}
                          <Link
                            to={`/store-owner/platform-fees/${stmt.id}`}
                            className="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 shadow-2xs hover:bg-slate-50 transition"
                          >
                            <Eye className="h-3.5 w-3.5 text-slate-500" />
                            Details
                          </Link>

                          {/* Pay Now Button */}
                          {canPay && (
                            <button
                              type="button"
                              onClick={() => void handleOpenPayment(stmt)}
                              disabled={isCreatingLink}
                              className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-bold text-white shadow-2xs hover:bg-emerald-700 active:scale-98 transition disabled:opacity-50"
                            >
                              <CreditCard className="h-3.5 w-3.5" />
                              Pay Now
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="border-t border-slate-100 bg-slate-50/50 p-4">
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              totalItems={totalCount}
              pageSize={10}
              onPageChange={(p) => setPage(p)}
              itemLabel="statements"
            />
          </div>
        )}
      </div>

      {/* Payment Modal */}
      <PlatformFeePaymentModal
        isOpen={Boolean(paymentData)}
        onClose={() => {
          setPaymentData(null);
          setPayingStatement(null);
        }}
        paymentData={paymentData}
        statementPeriodText={
          payingStatement
            ? `Month ${payingStatement.billingMonth}/${payingStatement.billingYear}`
            : undefined
        }
        onPaymentSuccess={handlePaymentSuccess}
      />
    </div>
  );
}
