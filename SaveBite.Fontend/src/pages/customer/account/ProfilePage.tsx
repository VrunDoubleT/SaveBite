import { AddressSection } from "@/features/account/profile/components/AddressSection";
import { UserProfileCard } from "@/features/account/profile/components/UserProfileCard";

export function ProfilePage() {
  return (
    <div className="space-y-4">
      <UserProfileCard />
      <AddressSection />
    </div>
  );
}
