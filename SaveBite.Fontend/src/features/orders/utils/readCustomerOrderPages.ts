import type { OrderHistoryPage } from "@/features/checkout/types/checkout.types";

// Rebuild loaded history from fresh cursors when status changes move orders between filters.
export async function readCustomerOrderPages(read: (cursor?: string | null) => Promise<OrderHistoryPage>, minimumItems: number) {
  let result = await read();
  const orders = new Map(result.page.items.map((order) => [order.id, order]));
  const visited = new Set<string>();
  while (orders.size < minimumItems && result.page.hasMore && result.page.nextCursor) {
    const cursor = result.page.nextCursor;
    if (visited.has(cursor)) break;
    visited.add(cursor);
    result = await read(cursor);
    result.page.items.forEach((order) => orders.set(order.id, order));
  }
  return { ...result, page: { ...result.page, items: [...orders.values()] } };
}
