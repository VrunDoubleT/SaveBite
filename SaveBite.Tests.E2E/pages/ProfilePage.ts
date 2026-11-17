import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export interface ProfileFormValues {
  fullName: string;
  phone?: string;
}

export interface AddressFormValues {
  label?: string;
  addressLine: string;
  ward?: string;
  district?: string;
  city?: string;
  latitude: string;
  longitude: string;
  isDefault?: boolean;
}

export class ProfilePage extends BasePage {
  // Profile card & personal info
  readonly profileCard: Locator;
  readonly profileHeading: Locator;
  readonly editProfileButton: Locator;
  readonly editProfileForm: Locator;
  readonly fullNameInput: Locator;
  readonly phoneInput: Locator;
  readonly saveProfileButton: Locator;
  readonly cancelProfileButton: Locator;
  readonly avatarFileInput: Locator;
  readonly avatarImage: Locator;
  readonly avatarErrorAlert: Locator;

  // Change password & sign out
  readonly changePasswordButton: Locator;
  readonly changePasswordSection: Locator;
  readonly currentPasswordInput: Locator;
  readonly newPasswordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly saveNewPasswordButton: Locator;
  readonly cancelPasswordButton: Locator;
  readonly signOutButton: Locator;

  // Delivery addresses
  readonly addressesCard: Locator;
  readonly addNewAddressButton: Locator;
  readonly addressForm: Locator;
  readonly addressLabelInput: Locator;
  readonly addressLineInput: Locator;
  readonly wardInput: Locator;
  readonly districtInput: Locator;
  readonly cityInput: Locator;
  readonly latitudeInput: Locator;
  readonly longitudeInput: Locator;
  readonly setDefaultCheckbox: Locator;
  readonly saveAddressButton: Locator;
  readonly cancelAddressButton: Locator;
  readonly addressItems: Locator;

  constructor(page: Page) {
    super(page);

    this.profileCard = page.locator('[aria-labelledby="profile-heading"]');
    this.profileHeading = page.locator("#profile-heading");
    this.editProfileButton = this.profileCard.getByRole("button", { name: "Edit profile" });
    this.editProfileForm = page.locator('[aria-labelledby="edit-profile-heading"]');
    this.fullNameInput = this.editProfileForm.getByLabel("Full name");
    this.phoneInput = this.editProfileForm.getByLabel("Phone number");
    this.saveProfileButton = this.editProfileForm.getByRole("button", {
      name: /Save changes|Saving changes/i,
    });
    this.cancelProfileButton = this.editProfileForm.getByRole("button", { name: "Cancel" });
    this.avatarFileInput = this.profileCard.locator('input[type="file"]');
    this.avatarImage = this.profileCard.locator("img").first();
    this.avatarErrorAlert = this.profileCard.locator('p[role="alert"]');

    this.changePasswordButton = page.getByRole("button", { name: "Change password" });
    this.changePasswordSection = page.locator("#change-password-form");
    this.currentPasswordInput = this.changePasswordSection.getByLabel("Current password", {
      exact: true,
    });
    this.newPasswordInput = this.changePasswordSection.getByLabel("New password", { exact: true });
    this.confirmPasswordInput = this.changePasswordSection.getByLabel("Confirm new password", {
      exact: true,
    });
    this.saveNewPasswordButton = this.changePasswordSection.getByRole("button", {
      name: /Save new password|Changing password/i,
    });
    this.cancelPasswordButton = this.changePasswordSection.getByRole("button", { name: "Cancel" });
    this.signOutButton = page.getByRole("button", { name: /Sign out|Signing out/i });

    this.addressesCard = page.locator('[aria-labelledby="addresses-heading"]');
    this.addNewAddressButton = this.addressesCard.getByRole("button", {
      name: /Add new address|Add an address/i,
    });
    this.addressForm = this.addressesCard.locator("form");
    this.addressLabelInput = this.addressForm.getByLabel("Label");
    this.addressLineInput = this.addressForm.getByLabel(/Address line/i);
    this.wardInput = this.addressForm.getByLabel("Ward");
    this.districtInput = this.addressForm.getByLabel("District");
    this.cityInput = this.addressForm.getByLabel(/City \/ Province/i);
    this.latitudeInput = this.addressForm.getByLabel(/Latitude/i);
    this.longitudeInput = this.addressForm.getByLabel(/Longitude/i);
    this.setDefaultCheckbox = this.addressForm.getByLabel(/Set as default address/i);
    this.saveAddressButton = this.addressForm.getByRole("button", {
      name: /Save address|Save changes|Saving\.\.\./i,
    });
    this.cancelAddressButton = this.addressForm.getByRole("button", { name: "Cancel" });
    this.addressItems = this.addressesCard.locator("ul").first().locator("> li");
  }

  async navigate(): Promise<void> {
    const accountLink = this.page.getByRole("link", { name: "Account", exact: true });
    if (await accountLink.isVisible().catch(() => false)) {
      await accountLink.click();
    } else {
      await this.goto("/account/profile");
    }
    await this.profileHeading.waitFor({ state: "visible", timeout: 30_000 });
  }

  async openEditProfile(): Promise<void> {
    await this.editProfileButton.click();
    await this.editProfileForm.waitFor({ state: "visible" });
  }

  async fillAndSaveProfile(values: ProfileFormValues): Promise<void> {
    await this.fullNameInput.fill(values.fullName);
    if (values.phone !== undefined) {
      await this.phoneInput.fill(values.phone);
    }
    await this.saveProfileButton.click();
  }

  profileFieldValue(label: "Full name" | "Phone number" | "Email" | "Account status"): Locator {
    return this.profileCard
      .locator("dl div")
      .filter({ has: this.page.locator("dt", { hasText: label }) })
      .locator("dd");
  }

  editProfileAlertWithText(text: string | RegExp): Locator {
    return this.editProfileForm.getByRole("alert").filter({ hasText: text });
  }

  async openAddAddressForm(): Promise<void> {
    await this.addNewAddressButton.click();
    await this.addressForm.waitFor({ state: "visible" });
  }

  async fillAddressForm(values: AddressFormValues): Promise<void> {
    if (values.label !== undefined) {
      await this.addressLabelInput.fill(values.label);
    }
    await this.addressLineInput.fill(values.addressLine);
    if (values.ward !== undefined) {
      await this.wardInput.fill(values.ward);
    }
    if (values.district !== undefined) {
      await this.districtInput.fill(values.district);
    }
    if (values.city !== undefined) {
      await this.cityInput.fill(values.city);
    }
    await this.latitudeInput.fill(values.latitude);
    await this.longitudeInput.fill(values.longitude);

    if (values.isDefault !== undefined) {
      const isDisabled = await this.setDefaultCheckbox.isDisabled();
      if (!isDisabled) {
        await this.setDefaultCheckbox.setChecked(values.isDefault);
      }
    }
  }

  addressAlertWithText(text: string | RegExp): Locator {
    return this.addressForm.getByRole("alert").filter({ hasText: text });
  }

  addressCardByLabel(label: string | RegExp): Locator {
    return this.addressItems.filter({ has: this.page.locator("h3", { hasText: label }) });
  }

  async openChangePassword(): Promise<void> {
    await this.changePasswordButton.click();
    await this.changePasswordSection.waitFor({ state: "visible" });
  }

  async changePassword(
    currentPassword: string,
    newPassword: string,
    confirmPassword: string = newPassword,
    revealPasswords: boolean = true,
  ): Promise<void> {
    if (revealPasswords) {
      await this.revealPasswordField("current password", this.changePasswordSection);
      await this.revealPasswordField("new password", this.changePasswordSection);
      await this.revealPasswordField("confirm new password", this.changePasswordSection);
    }
    await this.currentPasswordInput.fill(currentPassword);
    await this.newPasswordInput.fill(newPassword);
    await this.confirmPasswordInput.fill(confirmPassword);
    await this.saveNewPasswordButton.click();
  }

  changePasswordAlertWithText(text: string | RegExp): Locator {
    return this.changePasswordSection.getByRole("alert").filter({ hasText: text });
  }

  toastStatusWithText(text: string | RegExp): Locator {
    return this.page
      .locator('section[aria-label="Notifications"] [role="status"]')
      .filter({ hasText: text });
  }
}
