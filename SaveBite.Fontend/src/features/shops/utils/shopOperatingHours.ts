export function formatShopOperatingHours(
  openingTime?: string | null,
  closingTime?: string | null,
): string | null {
  if (!openingTime || !closingTime) return null;

  const open = openingTime.slice(0, 5);
  const close = closingTime.slice(0, 5);
  if (open === close) return "Open 24/7";

  return `${open} - ${close}`;
}
