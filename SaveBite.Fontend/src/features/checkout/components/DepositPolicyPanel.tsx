import { Badge, Card } from "@/shared/components/ui";
import { BadgeCheck, LockKeyhole, ShieldCheck } from "lucide-react";
import type { CheckoutContext } from "../types/checkout.types";
import { restrictionMessage } from "../utils/checkoutPolicy";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";

export function DepositPolicyPanel({ context }: { context: CheckoutContext }) {
  const { policy, shop } = context;
  const free = policy.payAtShopAllowed && !policy.appliedRange;
  return (
    <Card aria-label="Your payment policy" className="!p-0 overflow-hidden">
      <div className="flex items-start gap-3 border-b border-border-default p-4 sm:p-5">
        <div className="rounded-md bg-primary-50 p-2 text-primary-700">
          <ShieldCheck aria-hidden="true" size={22} />
        </div>
        <div className="min-w-0">
          <h2 className="text-section font-semibold text-text-primary">Your payment policy</h2>
          <p
            title={`For your ${money(shop.subtotal)} order at ${shop.shopName}.`}
            className="mt-1 line-clamp-2 text-body text-text-secondary [overflow-wrap:anywhere]"
          >
            For your {money(shop.subtotal)} order at {shop.shopName}.
          </p>
        </div>
      </div>
      <div className="space-y-3 p-4 sm:p-5">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <Badge className="max-w-full bg-secondary-50 text-secondary-700">
            <BadgeCheck aria-hidden="true" size={17} />
            {policy.levelRank ? (
              <span className="shrink-0">Level {policy.levelRank} · </span>
            ) : null}
            <span
              title={policy.levelName}
              className="line-clamp-2 min-w-0 [overflow-wrap:anywhere]"
            >
              {policy.levelName}
            </span>
          </Badge>
          {policy.isDefaultLevel && (
            <span className="text-caption text-text-secondary">
              The starting level applies until you have a rating.
            </span>
          )}
        </div>
        {!policy.payAtShopAllowed ? (
          <div
            role="status"
            className="rounded-md border border-accent-300 bg-accent-50 p-4 text-accent-700"
          >
            <div className="flex items-center gap-2 text-body font-bold">
              <LockKeyhole aria-hidden="true" size={19} className="shrink-0" />
              FULL ONLINE PAYMENT REQUIRED
            </div>
            <p className="mt-2 text-body leading-relaxed">{restrictionMessage(context)}</p>
            <p className="mt-3 text-xl font-bold tabular-nums">{money(shop.subtotal)}</p>
            <p className="mt-1 text-caption text-accent-700">
              Pay in full online · No remaining balance at the shop.
            </p>
          </div>
        ) : (
          <div className="rounded-md border border-primary-200 bg-primary-50 p-4">
            <p className="text-body font-semibold text-primary-900">
              {free ? "No deposit required" : "Your applicable deposit range"}
            </p>
            <p className="mt-1 text-body text-primary-800">
              {policy.appliedRange
                ? `Over ${money(policy.appliedRange.fromOrderAmount)} up to ${money(policy.appliedRange.toOrderAmount)}`
                : `Order value up to ${money(policy.freeOrderAmount ?? 0)}`}
            </p>
            <div className="mt-3 flex flex-wrap items-end justify-between gap-3">
              <span className="text-body text-primary-900">Online deposit for Pay at shop</span>
              <strong className="shrink-0 text-xl tabular-nums text-primary-900">
                {money(policy.requiredDeposit)}
              </strong>
            </div>
            <p className="mt-2 text-caption text-primary-800">
              This is a fixed amount for your order value range, not a percentage.
            </p>
          </div>
        )}
        {policy.ranges.length > 0 && (
          <div className="overflow-x-auto rounded-md border border-border-default">
            <table className="w-full text-left text-caption">
              <caption className="border-b border-border-default bg-neutral-50 px-3 py-2 text-left font-semibold text-text-secondary">
                Deposit ranges for your current level
              </caption>
              <thead className="text-caption text-text-secondary">
                <tr>
                  <th scope="col" className="px-3 py-2 font-medium">
                    Order value
                  </th>
                  <th scope="col" className="px-3 py-2 text-right font-medium">
                    Fixed deposit
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-default">
                {policy.freeOrderAmount !== undefined && (
                  <tr
                    className={
                      free ? "bg-primary-50 font-semibold text-primary-900" : "text-text-secondary"
                    }
                  >
                    <td className="px-3 py-2">
                      Up to {money(policy.freeOrderAmount)}
                      {free && <span className="ml-2 text-caption">· Applied</span>}
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
                          ? "bg-primary-50 font-semibold text-primary-900"
                          : "text-text-secondary"
                      }
                    >
                      <td className="px-3 py-2">
                        Over {money(range.fromOrderAmount)} up to {money(range.toOrderAmount)}
                        {applied && (
                          <span className="block text-caption">Applied to this order</span>
                        )}
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
                        ? "bg-accent-50 font-semibold text-accent-700"
                        : "text-text-secondary"
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
        <p className="text-caption leading-relaxed text-text-secondary">
          Each range includes its upper limit and excludes its lower limit. The policy is fixed when
          you place your order. Later level or policy changes do not affect the deposit recorded on
          this order.
        </p>
      </div>
    </Card>
  );
}
