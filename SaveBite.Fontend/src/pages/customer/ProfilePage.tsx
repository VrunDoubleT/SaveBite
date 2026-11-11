import { AddressSection } from "@/features/addresses/components/AddressSection";
import { UserProfileCard } from "@/features/profiles/components/UserProfileCard";

export function ProfilePage() {
  return (
    <div className="min-w-0 space-y-4">
      <UserProfileCard />
      <AddressSection />
    </div>
  );
}
