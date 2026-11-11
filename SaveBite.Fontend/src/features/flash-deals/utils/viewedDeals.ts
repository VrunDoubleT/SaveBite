const VIEWED_DEALS_KEY = "savebite_viewed_deals";

// In-memory set for rapid lookups and SSR safety
const memoryViewedDeals = new Set<string>();

function loadViewedDeals(): Set<string> {
  if (typeof window === "undefined") return memoryViewedDeals;
  try {
    const raw = sessionStorage.getItem(VIEWED_DEALS_KEY);
    if (raw) {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) {
        for (const id of parsed) {
          if (id) memoryViewedDeals.add(String(id).toLowerCase());
        }
      }
    }
  } catch (err) {
    void err;
  }
  return memoryViewedDeals;
}

export function isDealViewed(dealId?: string | null): boolean {
  if (!dealId) return false;
  loadViewedDeals();
  return memoryViewedDeals.has(dealId.toLowerCase());
}

export function markDealAsViewed(dealId?: string | null): void {
  if (!dealId) return;
  const normalized = dealId.toLowerCase();
  memoryViewedDeals.add(normalized);
  if (typeof window !== "undefined") {
    try {
      sessionStorage.setItem(
        VIEWED_DEALS_KEY,
        JSON.stringify(Array.from(memoryViewedDeals)),
      );
    } catch (err) {
      void err;
    }
  }
}

export function removeDealFromViewed(dealId?: string | null): void {
  if (!dealId) return;
  const normalized = dealId.toLowerCase();
  memoryViewedDeals.delete(normalized);
  if (typeof window !== "undefined") {
    try {
      sessionStorage.setItem(
        VIEWED_DEALS_KEY,
        JSON.stringify(Array.from(memoryViewedDeals)),
      );
    } catch (err) {
      void err;
    }
  }
}