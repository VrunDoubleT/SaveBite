// FIX-43: staff names are shown as "Display Name (Nickname)"; the nickname part is omitted when empty.
export function formatStaffName(displayName?: string | null, nickname?: string | null): string {
  const name = displayName?.trim() || "Staff";
  const nick = nickname?.trim();
  return nick ? `${name} (${nick})` : name;
}
