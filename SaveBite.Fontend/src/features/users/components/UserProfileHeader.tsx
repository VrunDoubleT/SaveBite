import { CalendarDays } from "lucide-react";
import type { UserDetailsResponse } from "@/features/users/types/user.types";
import { UserStatusBadge } from "./UserStatusBadge";

interface UserProfileHeaderProps {
  data: UserDetailsResponse;
}

export function UserProfileHeader({ data }: UserProfileHeaderProps) {
  return (
    <div className="flex flex-col sm:flex-row gap-5 items-start bg-white p-5 rounded-xl border border-border-default shadow-sm">
      <div className="flex size-16 shrink-0 items-center justify-center rounded-full bg-primary-50 text-2xl font-black text-primary-600 border border-primary-100 shadow-sm">
        {data.fullName.charAt(0).toUpperCase()}
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-2">
          <div className="truncate">
            <h4 className="text-xl font-bold text-text-primary truncate">{data.fullName}</h4>
            <p className="text-sm text-text-secondary truncate">{data.email}</p>
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
