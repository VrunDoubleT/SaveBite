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
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Platform Fees</h1>
          <p className="mt-1 text-sm text-neutral-500">
            Track periodic commission statements and settle balances online via PayOS
          </p>
        </div>
        <button
          onClick={() => void loadStatements()}
          disabled={loading}
          className="inline-flex items-center gap-2 self-start rounded-lg border border-neutral-200 bg-white px-4 py-2.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-50"
        >
          <RefreshCw className={`size-3.5 ${loading ? "animate-spin" : ""}`} />
          Refresh
        </button>
      </div>

      {/* Overdue fee guidance stays on this page; shop suspension is shown by the workspace layout. */}
      {hasOverdue && (
        <div className="rounded-xl border border-rose-200 bg-rose-50/80 p-4 text-rose-900 shadow-2xs">
          <div className="flex items-start gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-rose-100 text-rose-700">
              <ShieldAlert className="size-5" />
            </span>
            <div className="flex-1 text-xs sm:text-sm">
              <h3 className="font-bold text-rose-900">
                Attention: You have overdue platform fee statements!
              </h3>
              <p className="mt-1 text-rose-800 leading-normal">
                Settle all overdue balances below. Fee-related suspensions are lifted automatically
                after all outstanding balances are paid.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* 3 Stats Cards */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 sm:gap-4">
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-rose-50 text-rose-600">
              <CreditCard className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Total unpaid fees</p>
              <p className="text-xl font-bold text-rose-600">{formatVnd(unpaidTotal)}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <Receipt className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Total statements</p>
              <p className="text-xl font-bold text-neutral-900">{totalCount}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span
              className={`flex size-10 items-center justify-center rounded-lg ${
                shop?.status === "Active"
                  ? "bg-emerald-50 text-emerald-600"
                  : "bg-rose-50 text-rose-600"
              }`}
            >
              {shop?.status === "Active" ? (
                <CreditCard className="size-5" />
              ) : (
                <ShieldAlert className="size-5" />
              )}
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Operating status</p>
              <p
                className={`text-xl font-bold ${
                  shop?.status === "Active" ? "text-emerald-600" : "text-rose-600"
                }`}
              >
                {shop?.status ?? "Normal"}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-neutral-200 bg-white p-4 shadow-2xs">
        <div className="flex flex-wrap items-center gap-3">
          <span className="flex items-center gap-1.5 text-xs font-bold text-neutral-700">
            <Filter className="size-4 text-neutral-400" /> Filters:
          </span>

          {/* Year filter */}
          <select
            value={selectedYear}
            onChange={(e) => {
              setSelectedYear(e.target.value === "All" ? "All" : parseInt(e.target.value));
              setPage(1);
            }}
            className="rounded-lg border border-neutral-200 bg-neutral-50 px-3 py-1.5 text-xs font-semibold text-neutral-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
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
            className="rounded-lg border border-neutral-200 bg-neutral-50 px-3 py-1.5 text-xs font-semibold text-neutral-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
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
            className="rounded-lg border border-neutral-200 bg-neutral-50 px-3 py-1.5 text-xs font-semibold text-neutral-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
          >
            <option value="All">All Statuses</option>
            <option value="Issued">Issued</option>
            <option value="Paid">Paid</option>
            <option value="Overdue">Overdue</option>
          </select>
        </div>

        <div className="text-xs text-neutral-400 font-medium">Found {totalCount} statements</div>
      </div>

      {/* Table of Statements */}
      <div className="overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-2xs">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-neutral-50/75 text-neutral-500 uppercase tracking-wider font-semibold border-b border-neutral-200">
              <tr>
                <th className="px-5 py-4">Billing Period</th>
                <th className="px-5 py-4">Total Fee Amount</th>
                <th className="px-5 py-4">Status</th>
                <th className="px-5 py-4">Due Date</th>
                <th className="px-5 py-4">Paid Date</th>
                <th className="px-5 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-100">
              {loading || shopLoading ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-neutral-400">
                    <RefreshCw className="mx-auto h-6 w-6 animate-spin text-emerald-600 mb-2" />
                    Loading statements data...
                  </td>
                </tr>
              ) : statements.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-neutral-400">
                    <Receipt className="mx-auto h-8 w-8 text-neutral-300 mb-2" />
                    No statements found matching the filter criteria.
                  </td>
                </tr>
              ) : (
                statements.map((stmt) => {
                  const normalizedStatus = normalizeStatementStatus(stmt.status);
                  const canPay = normalizedStatus === "Issued" || normalizedStatus === "Overdue";
                  return (
                    <tr key={stmt.id} className="hover:bg-neutral-50/60 transition-colors">
                      <td className="px-5 py-4 font-bold text-neutral-900">
                        Month {stmt.billingMonth}/{stmt.billingYear}
                      </td>
                      <td className="px-5 py-4 font-extrabold text-neutral-900">
                        {formatVnd(stmt.totalFeeAmount)}
                      </td>
                      <td className="px-5 py-4">
                        <StatusBadge status={stmt.status} />
                      </td>
                      <td className="px-5 py-4 text-neutral-600">
                        {stmt.dueAt ? new Date(stmt.dueAt).toLocaleDateString("en-US") : "—"}
                      </td>
                      <td className="px-5 py-4 text-neutral-600">
                        {stmt.paidAt ? new Date(stmt.paidAt).toLocaleDateString("en-US") : "—"}
                      </td>
                      <td className="px-5 py-4 text-right">
                        <div className="inline-flex items-center gap-2">
                          {/* Detail Button */}
                          <Link
                            to={`/store-owner/platform-fees/${stmt.id}`}
                            className="inline-flex items-center gap-1 rounded-lg border border-neutral-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs hover:bg-neutral-50 transition"
                          >
                            <Eye className="size-3.5 text-neutral-500" />
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
                              <CreditCard className="size-3.5" />
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
          <div className="border-t border-neutral-100 bg-neutral-50/50 p-4">
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