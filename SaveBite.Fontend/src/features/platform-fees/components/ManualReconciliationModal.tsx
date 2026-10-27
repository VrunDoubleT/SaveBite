import { useState, useEffect, type FormEvent } from "react";
import { Receipt, X } from "lucide-react";
import { platformFeeApi } from "../api/platformFeeApi";
import type { PlatformFeeStatementSummary } from "../types/platformFee.types";
import { normalizeStatementStatus } from "../utils/platformFeeUtils";
import { StatusBadge } from "./StatusBadge";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

const formatVnd = (amount: number) => {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount);
};

interface ManualReconciliationModalProps {
  isOpen: boolean;
  onClose: () => void;
  statements: PlatformFeeStatementSummary[];
  initialStatement?: PlatformFeeStatementSummary | null;
  onSuccess: () => void;
}

export function ManualReconciliationModal({
  isOpen,
  onClose,
  statements,
  initialStatement,
  onSuccess,
}: ManualReconciliationModalProps) {
  const [statementId, setStatementId] = useState("");
  const [amount, setAmount] = useState("");
  const [referenceCode, setReferenceCode] = useState("");
  const [selectedStatement, setSelectedStatement] = useState<PlatformFeeStatementSummary | null>(null);
  const [unpaidList, setUnpaidList] = useState<PlatformFeeStatementSummary[]>([]);
  const [loadingUnpaid, setLoadingUnpaid] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!isOpen) return;

    if (initialStatement) {
      setStatementId(initialStatement.id);
      setAmount(initialStatement.totalFeeAmount.toString());
      setSelectedStatement(initialStatement);
      setUnpaidList([initialStatement]);
      setReferenceCode("");
      return;
    }

    setStatementId("");
    setAmount("");
    setSelectedStatement(null);
    setReferenceCode("");

    // Fetch all pending / overdue statements across system
    let isMounted = true;
    const fetchUnpaid = async () => {
      setLoadingUnpaid(true);
      try {
        const result = await platformFeeApi.getAdminStatements({
          pageSize: 100,
        });
        if (isMounted) {
          const unpaid = (result.items || []).filter(
            (s) => normalizeStatementStatus(s.status) !== "Paid",
          );
          setUnpaidList(unpaid);
        }
      } catch {
        if (isMounted) {
          // Fallback to filtering statements prop
          const unpaid = statements.filter(
            (s) => normalizeStatementStatus(s.status) !== "Paid",
          );
          setUnpaidList(unpaid);
        }
      } finally {
        if (isMounted) setLoadingUnpaid(false);
      }
    };

    void fetchUnpaid();
    return () => {
      isMounted = false;
    };
  }, [isOpen, initialStatement, statements]);

  if (!isOpen) return null;

  const handleSelectChange = (selectedId: string) => {
    setStatementId(selectedId);
    const found = unpaidList.find((s) => s.id === selectedId);
    if (found) {
      setSelectedStatement(found);
      setAmount(found.totalFeeAmount.toString());
    } else {
      setSelectedStatement(null);
      setAmount("");
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!statementId.trim() || !amount.trim()) {
      toast.error("Please select a statement and enter the payment amount.");
      return;
    }

    const numAmount = parseFloat(amount);
    if (isNaN(numAmount) || numAmount <= 0) {
      toast.error("Please enter a valid payment amount.");
      return;
    }

    setIsSubmitting(true);
    try {
      await platformFeeApi.recordExternalPayment(statementId.trim(), {
        amount: numAmount,
        referenceCode: referenceCode.trim() || undefined,
      });
      toast.success("External payment reconciliation recorded successfully!");
      onSuccess();
      onClose();
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to record reconciliation payment.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="w-full max-w-lg max-h-[90vh] flex flex-col rounded-2xl bg-white shadow-2xl ring-1 ring-slate-900/10 overflow-hidden">
        {/* Header */}
        <div className="shrink-0 flex items-center justify-between px-6 py-4 border-b border-slate-100 bg-slate-50/50">
          <div className="flex items-center gap-2">
            <Receipt className="h-5 w-5 text-emerald-600" />
            <h3 className="text-base font-bold text-slate-900">Manual Reconciliation</h3>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          {/* Select Statement from System */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Select Statement:
            </label>
            <select
              required
              value={statementId}
              onChange={(e) => handleSelectChange(e.target.value)}
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs text-slate-800 shadow-2xs focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            >
              <option value="">
                {loadingUnpaid
                  ? "-- Loading Unpaid Statements... --"
                  : unpaidList.length === 0
                  ? "-- No Unpaid Statements Found --"
                  : "-- Choose an Unpaid Statement from System --"}
              </option>
              {unpaidList.map((stmt) => {
                const norm = normalizeStatementStatus(stmt.status);
                return (
                  <option key={stmt.id} value={stmt.id}>
                    {stmt.shopName || "Shop"} • Tháng {stmt.billingMonth}/{stmt.billingYear} • {formatVnd(stmt.totalFeeAmount)} ({norm})
                  </option>
                );
              })}
            </select>
          </div>

          {/* Selected Statement Card Info */}
          {selectedStatement && (
            <div className="rounded-xl border border-emerald-200 bg-emerald-50/70 p-3.5 text-xs space-y-2">
              <div className="flex items-center justify-between">
                <span className="font-bold text-slate-900">{selectedStatement.shopName || "Shop"}</span>
                <StatusBadge status={selectedStatement.status} size="sm" />
              </div>
              <div className="flex items-center justify-between text-slate-600">
                <span>Period: Month {selectedStatement.billingMonth}/{selectedStatement.billingYear}</span>
                <span className="font-extrabold text-emerald-700 text-sm">
                  {formatVnd(selectedStatement.totalFeeAmount)}
                </span>
              </div>
              <div className="text-[11px] font-mono text-slate-500 truncate pt-1 border-t border-emerald-200/60">
                Statement ID: {selectedStatement.id}
              </div>
            </div>
          )}

          {/* Payment Amount */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Payment Amount (VNĐ):
            </label>
            <input
              type="number"
              min="1000"
              required
              placeholder="e.g. 150000"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className="w-full rounded-xl border border-slate-200 px-3 py-2 text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {/* Reference Note */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Reference Code / Note:
            </label>
            <input
              type="text"
              placeholder="e.g. Direct bank transfer or cash payment at office"
              value={referenceCode}
              onChange={(e) => setReferenceCode(e.target.value)}
              className="w-full rounded-xl border border-slate-200 px-3 py-2 text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {/* Action buttons */}
          <div className="flex justify-end gap-2 pt-3 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              className="rounded-xl px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 transition"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-xl bg-emerald-600 px-4 py-2 text-xs font-bold text-white shadow-xs hover:bg-emerald-700 disabled:opacity-50 transition"
            >
              {isSubmitting ? "Processing..." : "Confirm Reconciliation"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
