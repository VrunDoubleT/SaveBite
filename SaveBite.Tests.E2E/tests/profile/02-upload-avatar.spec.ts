import { expect, test } from "@playwright/test";
import { authenticateInBrowser, TEST_USERS } from "../../helpers/authHelper.js";
import { ProfilePage } from "../../pages/ProfilePage.js";

const VALID_1X1_PNG_BASE64 =
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

test.describe("Profile & Address - Upload Avatar (TC-005 ~ TC-006)", () => {
  test("TC-005: Upload avatar rejects unsupported file type", async ({ page, request }) => {
    const profilePage = new ProfilePage(page);

    await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );
    await profilePage.navigate();
    await profilePage.openEditProfile();

    await profilePage.avatarFileInput.setInputFiles({
      name: "invalid.txt",
      mimeType: "text/plain",
      buffer: Buffer.from("not-an-image"),
    });

    await expect(profilePage.avatarErrorAlert).toContainText("Choose a JPG, PNG or WebP image.");
  });

  test("TC-006: Upload valid PNG/JPG avatar updates preview and saves to server", async ({
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

    await profilePage.avatarFileInput.setInputFiles({
      name: "e2e-avatar.png",
      mimeType: "image/png",
      buffer: Buffer.from(VALID_1X1_PNG_BASE64, "base64"),
    });

    await expect(profilePage.avatarImage).toHaveAttribute("src", /^blob:/);

    await profilePage.saveProfileButton.click();
    await expect(
      profilePage.toastStatusWithText("Profile updated successfully."),
    ).toBeVisible({ timeout: 30_000 });

    await page.reload({ waitUntil: "domcontentloaded" });
    await expect(profilePage.avatarImage).toBeVisible();
    await expect(profilePage.avatarImage).toHaveAttribute("src", /^https?:\/\//);
  });
});
