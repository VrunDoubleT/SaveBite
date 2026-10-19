import { UserRound, Cog } from "lucide-react";
import { historyActor } from "../utils/orderHistory";
export function HistoryActor({
  actorId,
  actorRole,
  actorName,
}: {
  actorId?: string | null;
  actorRole?: string | null;
  actorName?: string | null;
}) {
  const actor = historyActor({ actorId, actorRole, actorName });
  const Icon = actor.name === "System" && !actorId ? Cog : UserRound;
  return (
    <p className="mt-1.5 flex flex-wrap items-center gap-1.5 text-xs" aria-label="Performed by">
      <Icon size={12} className="shrink-0 text-neutral-400" />
      <span className="font-medium text-neutral-700">{actor.name}</span>
      {actor.role && (
        <span className="rounded bg-neutral-100 px-1.5 py-0.5 text-[10px] text-neutral-500">
          {actor.role}
        </span>
      )}
    </p>
  );
}
