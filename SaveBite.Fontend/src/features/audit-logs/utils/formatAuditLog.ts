import { Activity, Ban, CheckCircle2, EyeOff, PencilLine, PlusCircle, Trash2, type LucideIcon } from "lucide-react";
import type { Tone } from "@/shared/components/analytics";

const VN_TIME_ZONE = "Asia/Ho_Chi_Minh";

/** Turns FLASH_DEAL_STOPPED or UpdateTrustLevelBenefits into "Flash deal stopped" / "Update trust level benefits". */
export function formatAuditAction(action: string): string {
  const words = action.includes("_")
    ? action.split("_")
    : action.replace(/([a-z0-9])([A-Z])/g, "$1 $2").split(" ");
  const sentence = words
    .filter(Boolean)
    .map((w) => w.toLowerCase())
    .join(" ");
  return sentence.charAt(0).toUpperCase() + sentence.slice(1);
}

export function shortId(id: string): string {
  return id.length > 14 ? `${id.slice(0, 8)}…${id.slice(-4)}` : id;
}

/** yyyy-MM-dd of the entry in Vietnam time, used to group the list by day. */
export function auditDayKey(iso: string): string {
  return new Date(iso).toLocaleDateString("en-CA", { timeZone: VN_TIME_ZONE });
}

export function formatAuditDayHeading(iso: string): string {
  // English heading, e.g. "Friday 23 October 2026", still on the Vietnam calendar day.
  return new Date(iso).toLocaleDateString("en-GB", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: VN_TIME_ZONE,
  });
}

export function formatAuditTime(iso: string): string {
  return new Date(iso).toLocaleTimeString("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
    timeZone: VN_TIME_ZONE,
  });
}

export interface AuditActionStyle {
  tone: Tone;
  icon: LucideIcon;
}

/** Picks an icon and colour from the wording of the action so a long list can be scanned by shape and colour. */
export function auditActionStyle(action: string): AuditActionStyle {
  const a = action.toLowerCase().replace(/[^a-z]/g, "");
  if (a.includes("delete") || a.includes("remove")) return { tone: "danger", icon: Trash2 };
  if (a.includes("suspend") || a.includes("stop") || a.includes("cancel") || a.includes("reject") || a.includes("ban") || a.includes("lock"))
    return { tone: "danger", icon: Ban };
  if (a.includes("unhide") || a.includes("reactivate") || a.includes("approve") || a.includes("restore") || a.includes("onsale"))
    return { tone: "primary", icon: CheckCircle2 };
  if (a.includes("hide")) return { tone: "neutral", icon: EyeOff };
  if (a.includes("create") || a.includes("add")) return { tone: "primary", icon: PlusCircle };
  if (a.includes("update") || a.includes("change") || a.includes("edit") || a.includes("config") || a.includes("set"))
    return { tone: "info", icon: PencilLine };
  return { tone: "neutral", icon: Activity };
}