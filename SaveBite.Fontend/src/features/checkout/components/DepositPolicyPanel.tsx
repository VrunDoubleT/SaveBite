import { BadgeCheck, LockKeyhole, ShieldCheck } from "lucide-react";
import type { CheckoutContext } from "../types/checkout.types";
import { restrictionMessage } from "../utils/checkoutPolicy";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";

export function DepositPolicyPanel({ context }: { context: CheckoutContext }) {
  const { policy, shop } = context;
  const free = policy.payAtShopAllowed && !policy.appliedRange;
  return (
    <section className="overflow-hidden rounded-xl border border-neutral-200 bg-white">
      <div className="flex items-start gap-3 border-b border-neutral-100 p-4 sm:p-5">
        <div className="rounded-lg bg-emerald-50 p-2 text-emerald-700">
          <ShieldCheck size={22} />
        </div>
        <div>
          <h2 className="text-base font-semibold text-neutral-900">Your payment policy</h2>
          <p className="mt-1 text-sm text-neutral-500">
            For your {money(shop.subtotal)} order at {shop.shopName}.
          </p>
        </div>
      </div>
      <div className="space-y-3 p-4 sm:p-5">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="inline-flex items-center gap-2 rounded-full bg-emerald-50 px-3 py-1.5 text-sm font-semibold text-emerald-800">
            <BadgeCheck size={17} />
            {policy.levelRank ? `Level ${policy.levelRank} · ` : ""}
            {policy.levelName}
          </span>
          {policy.isDefaultLevel && (
            <span className="text-xs text-neutral-500">
              The starting level applies until you have a rating.
            </span>
          )}
        </div>
        {!policy.payAtShopAllowed ? (
          <div
            role="status"
            className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-amber-950"
          >
            <div className="flex items-center gap-2 text-sm font-bold">
              <LockKeyhole size={19} className="shrink-0" />
              FULL ONLINE PAYMENT REQUIRED
            </div>
            <p className="mt-2 text-sm leading-relaxed">{restrictionMessage(context)}</p>
            <p className="mt-3 text-xl font-bold tabular-nums">{money(shop.subtotal)}</p>
            <p className="mt-1 text-xs text-amber-900">
              Pay in full online · No remaining balance at the shop.
            </p>
          </div>
        ) : (
          <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4">
            <p className="text-sm font-semibold text-emerald-900">
              {free ? "No deposit required" : "Your applicable deposit range"}
            </p>
            <p className="mt-1 text-sm text-emerald-800">
              {policy.appliedRange
                ? `Over ${money(policy.appliedRange.fromOrderAmount)} up to ${money(policy.appliedRange.toOrderAmount)}`
                : `Order value up to ${money(policy.freeOrderAmount ?? 0)}`}
            </p>
            <div className="mt-3 flex items-end justify-between gap-3">
              <span className="text-sm text-emerald-900">Online deposit for Pay at shop</span>
              <strong className="shrink-0 text-xl tabular-nums text-emerald-900">
                {money(policy.requiredDeposit)}
              </strong>
            </div>
            <p className="mt-2 text-xs text-emerald-800">
              This is a fixed amount for your order value range, not a percentage.
            </p>
          </div>
        )}
        {policy.ranges.length > 0 && (
          <div className="overflow-hidden rounded-xl border border-neutral-200">
            <table className="w-full text-left text-xs">
              <caption className="border-b border-neutral-100 bg-neutral-50 px-3 py-2 text-left font-semibold text-neutral-700">
                Deposit ranges for your current level
              </caption>
              <thead className="text-xs text-neutral-500">
                <tr>
                  <th scope="col" className="px-3 py-2 font-medium">
                    Order value
                  </th>
                  <th scope="col" className="px-3 py-2 text-right font-medium">
                    Fixed deposit
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-100">
                {policy.freeOrderAmount !== undefined && (
                  <tr
                    className={
                      free ? "bg-emerald-50 font-semibold text-emerald-900" : "text-neutral-600"
                    }
                  >
                    <td className="px-3 py-2">
                      Up to {money(policy.freeOrderAmount)}
                      {free && <span className="ml-2 text-xs">· Applied</span>}
                    </td>
                    <td className="px-3 py-2 text-right">{money(0)}</td>
                  </tr>
                )}
                {policy.ranges.map((range) => {
                  const applied = policy.payAtShopAllowed && policy.appliedRange?.id === range.id;
                  return (
                    <tr
                      key={range.id}
                      className={
                        applied
                          ? "bg-emerald-50 font-semibold text-emerald-900"
                          : "text-neutral-600"
                      }
                    >
                      <td className="px-3 py-2">
                        Over {money(range.fromOrderAmount)} up to {money(range.toOrderAmount)}
                        {applied && <span className="block text-xs">Applied to this order</span>}
                      </td>
                      <td className="whitespace-nowrap px-3 py-2 text-right tabular-nums">
                        {money(range.depositAmount)}
                      </td>
                    </tr>
                  );
                })}
                {policy.maxDepositOrderAmount !== undefined && (
                  <tr
                    className={
                      policy.restrictionCode === "AboveDepositMaximum"
                        ? "bg-amber-50 font-semibold text-amber-900"
                        : "text-neutral-600"
                    }
                  >
                    <td className="px-3 py-2">Over {money(policy.maxDepositOrderAmount)}</td>
                    <td className="px-3 py-2 text-right">Online 100%</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
        <p className="text-xs leading-relaxed text-neutral-500">
          Each range includes its upper limit and excludes its lower limit. The policy is fixed when
          you place your order. Later level or policy changes do not affect the deposit recorded on
          this order.
        </p>
      </div>
    </section>
  );
}
