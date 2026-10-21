export function confirmationRemaining(deadline: string, now: number) {
  const expires = Date.parse(deadline);
  if (!Number.isFinite(expires) || !Number.isFinite(now)) return null;
  const seconds = Math.max(0, Math.ceil((expires - now) / 1000));
  const minutes = Math.floor(seconds / 60);
  const remainder = seconds % 60;
  return {
    seconds,
    expired: seconds === 0,
    urgent: seconds <= 60,
    label:
      seconds === 0
        ? "Confirmation window expired"
        : minutes > 0
          ? `${minutes} min ${remainder.toString().padStart(2, "0")} sec left to confirm`
          : `${seconds} sec left to confirm`,
  };
}

export function historyActor(event: {
  actorId?: string | null;
  actorRole?: string | null;
  actorName?: string | null;
}) {
  if (event.actorRole === "StoreOwner") return { name: "Shop owner", role: null };
  if (event.actorRole === "Staff")
    return { name: event.actorName?.trim() || "Staff member", role: "Staff" };
  if (event.actorRole === "Customer")
    return { name: event.actorName?.trim() || "Customer", role: "Customer" };
  if (event.actorRole === "System" || !event.actorId) return { name: "System", role: null };
  return { name: event.actorName?.trim() || "Shop operator", role: null };
}
