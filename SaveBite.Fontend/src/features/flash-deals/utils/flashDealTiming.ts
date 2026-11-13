/** Keep quick durations exact; HH:mm is only the display value in this mode. */
export function resolveDealEnd(
  start: Date,
  endTime: string,
  selectedDuration: number | null,
  fallbackMinutes = 60,
): Date {
  if (selectedDuration !== null || !endTime)
    return new Date(start.getTime() + (selectedDuration ?? fallbackMinutes) * 60_000);
  const [hours, minutes] = endTime.split(":").map(Number);
  const end = new Date(start.getFullYear(), start.getMonth(), start.getDate(), hours, minutes);
  if (end.getTime() <= start.getTime()) end.setDate(end.getDate() + 1);
  return end;
}
