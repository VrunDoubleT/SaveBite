import { AddressSection } from "@/features/addresses/components/AddressSection";
import { UserProfileCard } from "@/features/profiles/components/UserProfileCard";

export function ProfilePage() {
  return (
    <div className="space-y-6">
      <UserProfileCard />
      <AddressSection />
    </div>
  );
}
