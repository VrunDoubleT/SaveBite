import { Skeleton } from "@/shared/components/ui";
import { useState, type ReactNode } from "react";
import { Clock, Repeat } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { UserDetailsResponse } from "@/features/users/types/user.types";
import { UserProfileHeader } from "./UserProfileHeader";
import { UserAuditTimeline } from "./UserAuditTimeline";
import { UserRoleTimeline } from "./UserRoleTimeline";

interface UserDetailsModalProps {
  isOpen: boolean;
  isLoading: boolean;
  data: UserDetailsResponse | null;
  onClose: () => void;
}

export function UserDetailsModal({ isOpen, isLoading, data, onClose }: UserDetailsModalProps) {
  return (
    <SharedModal
      isOpen={isOpen}
      onClose={onClose}
      title="User Profile & Activity"
      maxWidthClass="max-w-2xl"
    >
      {isLoading ? (
        <div role="status" aria-label="Loading user profile" className="space-y-6">
          <span className="sr-only">Loading user profile</span>
          <div
            aria-hidden="true"
            className="flex flex-col items-start gap-5 rounded-xl border border-border-default p-5 sm:flex-row"
          >
            <Skeleton className="size-16 shrink-0 rounded-full" />
            <div className="w-full space-y-3">
              <Skeleton className="h-6 w-1/2" />
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-6 w-full" />
            </div>
          </div>
          <div aria-hidden="true" className="space-y-5">
            <div className="flex gap-4 border-b border-border-default pb-3">
              <Skeleton className="h-5 w-28" />
              <Skeleton className="h-5 w-28" />
            </div>
            <Skeleton className="h-28 w-full rounded-xl" />
          </div>
        </div>
      ) : data ? (
        <UserDetailsContent data={data} />
      ) : null}
    </SharedModal>
  );
}

type DetailsTab = "audit" | "role";

interface TabButtonProps {
  isActive: boolean;
  onClick: () => void;
  icon: ReactNode;
  children: ReactNode;
}

function TabButton({ isActive, onClick, icon, children }: TabButtonProps) {
  return (
    <button
      onClick={onClick}
      className={`flex items-center gap-2 pb-3 text-sm font-semibold transition-colors border-b-2 ${
        isActive
          ? "border-primary-600 text-primary-600"
          : "border-transparent text-text-secondary hover:text-text-primary"
      }`}
    >
      {icon}
      {children}
    </button>
  );
}

// Mounted only when data exists, so the active tab resets to "audit" on every open
function UserDetailsContent({ data }: { data: UserDetailsResponse }) {
  const [activeTab, setActiveTab] = useState<DetailsTab>("audit");

  return (
    <div className="space-y-6">
      <UserProfileHeader data={data} />

      <div>
        <div className="flex gap-4 border-b border-border-default mb-5">
          <TabButton
            isActive={activeTab === "audit"}
            onClick={() => setActiveTab("audit")}
            icon={<Clock className="size-4" />}
          >
            Account Logs
          </TabButton>
          <TabButton
            isActive={activeTab === "role"}
            onClick={() => setActiveTab("role")}
            icon={<Repeat className="size-4" />}
          >
            Role Changes
          </TabButton>
        </div>

        {activeTab === "audit" ? (
          <UserAuditTimeline logs={data.logs} />
        ) : (
          <UserRoleTimeline logs={data.roleLogs} />
        )}
      </div>
    </div>
  );
}
