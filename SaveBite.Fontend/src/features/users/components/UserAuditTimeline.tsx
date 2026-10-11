import { Clock, User as UserIcon } from "lucide-react";
import type { UserDetailsResponse } from "@/features/users/types/user.types";
import { ChangedValues } from "./ChangedValues";
import { Timeline, TimelineItem } from "./Timeline";

interface UserAuditTimelineProps {
  logs: UserDetailsResponse["logs"];
}

export function UserAuditTimeline({ logs }: UserAuditTimelineProps) {
  if (logs.length === 0) {
    return (
      <div className="text-center py-8 bg-white rounded-xl border border-dashed border-border-default">
        <UserIcon className="mx-auto size-8 text-neutral-300 mb-2" />
        <p className="text-sm text-text-muted font-medium">No activity history recorded.</p>
      </div>
    );
  }

  return (
    <Timeline>
      {logs.map((log, idx) => {
        const isCustomerLog = log.target === "Customer";

        return (
          <TimelineItem key={idx}>
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-2 gap-2">
              <div className="flex items-center gap-2">
                <h5 className="font-bold text-sm text-text-primary bg-neutral-100 px-2.5 py-1 rounded-md border border-neutral-200">
                  {log.action}
                </h5>
                <span
                  className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${
                    isCustomerLog
                      ? "bg-amber-100 text-amber-700 border border-amber-200"
                      : "bg-blue-100 text-blue-700 border border-blue-200"
                  }`}
                >
                  {isCustomerLog ? "Customer" : "Account"}
                </span>
              </div>
              <time className="text-xs text-text-muted font-medium shrink-0 flex items-center gap-1">
                <Clock className="size-3" />
                {new Date(log.createdAt).toLocaleString()}
              </time>
            </div>
            <p className="text-sm text-text-secondary leading-relaxed mt-3">
              <span className="font-semibold text-text-primary">Reason: </span>
              &quot;{log.reason}&quot;
            </p>
            <ChangedValues values={log.changedValues} />
          </TimelineItem>
        );
      })}
    </Timeline>
  );
}
