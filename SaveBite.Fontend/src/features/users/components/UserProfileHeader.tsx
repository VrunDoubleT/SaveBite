import { CalendarDays } from "lucide-react";
import type { UserDetailsResponse } from "@/features/users/types/user.types";
import { RecordId } from "@/shared/components/RecordId";
import { UserStatusBadge } from "./UserStatusBadge";
import { UserAvatar } from "./UserAvatar";

interface UserProfileHeaderProps {
  data: UserDetailsResponse;
}

export function UserProfileHeader({ data }: UserProfileHeaderProps) {
  return (
    <div className="flex flex-col sm:flex-row gap-5 items-start bg-white p-5 rounded-xl border border-border-default shadow-sm">
      {/* Avatar */}
      <UserAvatar avatarUrl={data.avatarUrl} fullName={data.fullName} size="lg" />

      <div className="flex-1 min-w-0">
        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-2">
          <div className="min-w-0">
            <h4 className="text-xl font-bold text-text-primary truncate">{data.fullName}</h4>

            <p className="text-sm text-text-secondary truncate">{data.email}</p>
            <RecordId id={data.id} variant="full" className="mt-2" />
          </div>

          <span className="inline-flex rounded-md bg-neutral-100 border border-neutral-200 px-2.5 py-1 text-[11px] font-bold text-neutral-700 uppercase tracking-widest shrink-0">
            {data.role}
          </span>
        </div>

        <div className="mt-4 flex flex-wrap gap-2 items-center">
          <UserStatusBadge status={data.status} label="Account" />

          <UserStatusBadge status={data.customerStatus} label="Customer" />

          <div className="ml-auto flex items-center gap-1.5 text-xs font-medium text-text-muted">
            <CalendarDays className="size-3.5" />
            Joined {new Date(data.createdAt).toLocaleDateString()}
          </div>
        </div>
      </div>
    </div>
  );
}
