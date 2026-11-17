import { expect, test } from "@playwright/test";
import { authenticateInBrowser, TEST_USERS } from "../../helpers/authHelper.js";
import { getMeViaApi, updateProfileViaApi } from "../../helpers/profileHelper.js";
import { ProfilePage } from "../../pages/ProfilePage.js";

test.describe("Profile & Address - View & Edit Profile (TC-001 ~ TC-004)", () => {
  test("TC-001: View current customer profile information on /account/profile", async ({
    page,
    request,
  }) => {
    const profilePage = new ProfilePage(page);

    await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );
    await profilePage.navigate();

    await expect(profilePage.profileHeading).toBeVisible();
    await expect(profilePage.profileFieldValue("Email")).toHaveText(TEST_USERS.customer.email);
    await expect(profilePage.profileFieldValue("Account status")).toHaveText("Active");
    await expect(profilePage.profileFieldValue("Full name")).not.toBeEmpty();
    await expect(profilePage.editProfileButton).toBeVisible();
    await expect(profilePage.changePasswordButton).toBeVisible();
  });

  test("TC-002: Update profile rejects empty Full name and invalid Phone number", async ({
    page,
    request,
  }) => {
    const profilePage = new ProfilePage(page);

    await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );
    await profilePage.navigate();
    await profilePage.openEditProfile();

    await profilePage.fillAndSaveProfile({
      fullName: "",
      phone: "12345",
    });

    await expect(profilePage.editProfileAlertWithText("Full name is required.")).toBeVisible();
    await expect(
      profilePage.editProfileAlertWithText("Phone number must contain 9 or 10 digits."),
    ).toBeVisible();

    await profilePage.phoneInput.fill("090123abc");
    await profilePage.saveProfileButton.click();

    await expect(profilePage.editProfileAlertWithText("Phone number is invalid.")).toBeVisible();
  });

  test("TC-003: Cancel editing profile discards unsaved changes", async ({ page, request }) => {
    const profilePage = new ProfilePage(page);

    await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );
    await profilePage.navigate();

    const originalName = (await profilePage.profileFieldValue("Full name").textContent())?.trim() ?? "";
    await profilePage.openEditProfile();
    await profilePage.fullNameInput.fill("Temporary Unsaved Name");
    await profilePage.cancelProfileButton.click();

    await expect(profilePage.editProfileForm).toBeHidden();
    await expect(profilePage.profileFieldValue("Full name")).toHaveText(originalName);
  });

  test("TC-004: Update Full name and Phone number successfully and persist after reload", async ({
    page,
    request,
  }) => {
    const profilePage = new ProfilePage(page);
    const { accessToken } = await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );
    const originalProfile = await getMeViaApi(request, accessToken);

    try {
      await profilePage.navigate();
      await profilePage.openEditProfile();

      const updatedName = "Customer E2E Updated";
      const updatedPhone = "0901234567";
      await profilePage.fillAndSaveProfile({
        fullName: updatedName,
        phone: updatedPhone,
      });

      await expect(
        profilePage.toastStatusWithText("Profile updated successfully."),
      ).toBeVisible();
      await expect(profilePage.profileHeading).toHaveText(updatedName);
      await expect(profilePage.profileFieldValue("Full name")).toHaveText(updatedName);
      await expect(profilePage.profileFieldValue("Phone number")).toHaveText(updatedPhone);

      await page.reload({ waitUntil: "domcontentloaded" });
      await expect(profilePage.profileFieldValue("Full name")).toHaveText(updatedName);
      await expect(profilePage.profileFieldValue("Phone number")).toHaveText(updatedPhone);
    } finally {
      await updateProfileViaApi(request, accessToken, {
        fullName: originalProfile.fullName,
        phone: originalProfile.phone,
      });
    }
  });
});
