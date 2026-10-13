import { Pagination } from "@/shared/components/Pagination";
import { useCallback, useState } from "react";
import { CalendarDays, ChevronDown, Zap } from "lucide-react";
import { productApi } from "../api/productApi";
import { useProductResource } from "../hooks/useProductResource";
import { dateTime, money } from "../utils/productForm";
import { Empty, ErrorState, inputClass, Loading } from "./ProductUi";

const statuses = ["Upcoming", "Active", "Hidden", "Sold out", "Ended", "Cancelled"];
const filters = ["Pending", "OnSale", "Hidden", "SoldOut", "Closed", "Cancelled"];
export function ProductDealsPanel({ productId }: { productId: string }) {
  const [page, setPage] = useState(1),
    [status, setStatus] = useState("");
  const load = useCallback(
    (signal: AbortSignal) => productApi.deals(productId, page, status, signal),
    [productId, page, status],
  );
  const { data, error, loading, reload } = useProductResource(
    `${productId}:${page}:${status}`,
    load,
  );
  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold">Flash deal history</h2>
          <p className="mt-1 text-sm text-neutral-500">
            Review prices and quantities for each sale.
          </p>
        </div>
        <select
          className={`${inputClass} mt-0! w-auto!`}
          aria-label="Filter by flash deal status"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
        >
          <option value="">All statuses</option>
          {filters.map((filter, i) => (
            <option key={filter} value={filter}>
              {statuses[i]}
            </option>
          ))}
        </select>
      </div>
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorState error={error} retry={reload} />
      ) : data && data.items.length ? (
        <>
          {data.items.map((deal) => {
            const sold = deal.variants.reduce((n, v) => n + v.soldQuantity, 0),
              total = deal.variants.reduce((n, v) => n + v.totalQuantity, 0);
            return (
              <details
                key={deal.id}
                className="group overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-sm"
              >
                <summary className="flex cursor-pointer list-none flex-wrap items-center justify-between gap-4 p-5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-400">
                  <div className="flex items-center gap-3">
                    <span className="rounded-lg bg-accent-50 p-2.5 text-accent-700">
                      <Zap className="size-5" />
                    </span>
                    <div>
                      <h3 className="text-sm font-semibold">Sale {dateTime(deal.saleStartTime)}</h3>
                      <p className="mt-1 text-xs text-neutral-500">
                        Orders close {dateTime(deal.orderEndTime)}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <span className="text-sm font-semibold">
                        {sold}
                        <span className="font-normal text-neutral-500">/{total} sold</span>
                      </span>
                      <span className="mt-1 block text-xs text-primary-700">
                        {statuses[deal.status] ?? "Unknown"}
                      </span>
                    </div>
                    <ChevronDown className="size-4 text-neutral-400 transition group-open:rotate-180" />
                  </div>
                </summary>
                <div className="space-y-4 border-t border-neutral-100 p-5">
                  <p className="flex items-center gap-2 text-xs text-neutral-500">
                    <CalendarDays className="size-4" />
                    Shop closes: {dateTime(deal.shopClosingTime)}
                  </p>
                  <div className="overflow-x-auto">
                    <table className="w-full min-w-[700px] text-left text-sm">
                      <thead className="bg-neutral-50 text-xs text-neutral-500">
                        <tr>
                          {[
                            "Variants",
                            "Original price",
                            "Price",
                            "Quantity",
                            "Reserved",
                            "Sold",
                            "Available",
                          ].map((label) => (
                            <th className="px-3 py-3 font-medium" key={label}>
                              {label}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-neutral-100">
                        {deal.variants.map((v) => (
                          <tr key={v.id}>
                            <td className="px-3 py-3">
                              <span className="font-medium">
                                {v.values.map((value) => value.value).join(" / ") || "Default"}
                              </span>
                              {v.status === 1 && (
                                <span className="mt-1 block text-xs text-neutral-400">Hidden</span>
                              )}
                            </td>
                            <td className="whitespace-nowrap px-3 py-3 text-neutral-500">
                              {money(v.originalPrice)}
                            </td>
                            <td className="whitespace-nowrap px-3 py-3 font-semibold text-primary-700">
                              {money(v.dealPrice)}
                              <span className="mt-0.5 block text-xs font-normal">
                                −{v.discountPercent}%
                              </span>
                            </td>
                            <td className="px-3 py-3">{v.totalQuantity}</td>
                            <td className="px-3 py-3">{v.reservedQuantity}</td>
                            <td className="px-3 py-3 font-semibold">{v.soldQuantity}</td>
                            <td className="px-3 py-3">{v.availableQuantity}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              </details>
            );
          })}
          <Pagination
            page={data.page}
            pages={data.totalPages}
            total={data.totalItems}
            onPage={setPage}
          />
        </>
      ) : (
        <Empty
          title="No flash deals yet"
          description={
            status
              ? "No sales match the selected status."
              : "Flash deals for this product will appear here for reference."
          }
        />
      )}
    </div>
  );
}
