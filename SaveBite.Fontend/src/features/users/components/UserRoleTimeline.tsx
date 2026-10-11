import { ChevronRight, Clock, Shield } from "lucide-react";
import type { UserDetailsResponse } from "@/features/users/types/user.types";
import { Timeline, TimelineItem } from "./Timeline";

interface UserRoleTimelineProps {
  logs: UserDetailsResponse["roleLogs"];
}

export function UserRoleTimeline({ logs }: UserRoleTimelineProps) {
  if (logs.length === 0) {
    return (
      <div className="text-center py-8 bg-white rounded-xl border border-dashed border-border-default">
        <Shield className="mx-auto size-8 text-neutral-300 mb-2" />
        <p className="text-sm text-text-muted font-medium">No role changes recorded.</p>
      </div>
    );
  }

  return (
    <Timeline>
      {logs.map((log, idx) => (
        <TimelineItem key={idx}>
          <div className="flex justify-between items-center mb-3">
            <h5 className="font-bold text-sm text-text-primary flex items-center gap-2">
              <span className="bg-neutral-100 text-neutral-600 px-2 py-0.5 rounded border border-neutral-200 uppercase text-xs">
                {log.oldRole}
              </span>
              <ChevronRight className="size-4 text-neutral-400" />
              <span className="bg-primary-50 text-primary-700 px-2 py-0.5 rounded border border-primary-100 uppercase text-xs">
                {log.newRole}
              </span>
            </h5>
            <time className="text-xs text-text-muted font-medium flex items-center gap-1">
              <Clock className="size-3" />
              {new Date(log.createdAt).toLocaleString()}
            </time>
          </div>
          <p className="text-sm text-text-secondary leading-relaxed">
            <span className="font-semibold text-text-primary">Reason: </span>
            &quot;{log.reason}&quot;
          </p>
        </TimelineItem>
      ))}
    </Timeline>
  );
}
