<<<<<<< HEAD
export function ProfilePage() {
  return (
    <div className="rounded-lg border border-border-default bg-bg-surface p-6">
      <h2 className="text-lg font-semibold text-text-primary">Profile</h2>

      <p className="mt-2 text-sm text-text-secondary">Manage your profile information here.</p>
=======
import { AddressSection } from "@/features/account/profile/components/AddressSection";
import { UserProfileCard } from "@/features/account/profile/components/UserProfileCard";

export function ProfilePage() {
  return (
    <div className="space-y-4">
      <UserProfileCard />
      <AddressSection />
>>>>>>> origin/feature/iss-1-user-profile-address-management
    </div>
  );
}
