import { expect, test } from "@playwright/test";
import { authenticateInBrowser, TEST_USERS } from "../../helpers/authHelper.js";
import {
  createAddressViaApi,
  deleteAddressViaApi,
  getAddressesViaApi,
  restoreAddresses,
} from "../../helpers/profileHelper.js";
import { ProfilePage } from "../../pages/ProfilePage.js";

test.describe("Profile & Address - Manage Delivery Addresses (TC-007 ~ TC-012)", () => {
  test("TC-007: Address form validation rejects empty Address line and out-of-range coordinates", async ({
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
    await profilePage.openAddAddressForm();

    await profilePage.fillAddressForm({
      label: "Invalid Coords",
      addressLine: "",
      latitude: "95",
      longitude: "200",
    });
    await profilePage.saveAddressButton.click();

    await expect(profilePage.addressAlertWithText("Address line is required.")).toBeVisible();
    await expect(
      profilePage.addressAlertWithText("Latitude must be a number between -90 and 90."),
    ).toBeVisible();
    await expect(
      profilePage.addressAlertWithText("Longitude must be a number between -180 and 180."),
    ).toBeVisible();
  });

  test("TC-008: Create a new address with valid details and coordinates", async ({
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
    const originalAddresses = await getAddressesViaApi(request, accessToken);

    try {
      await profilePage.navigate();
      await profilePage.openAddAddressForm();

      const label = `Work E2E ${Date.now()}`;
      await profilePage.fillAddressForm({
        label,
        addressLine: "123 Nguyen Van Cu",
        ward: "Phuong An Binh",
        district: "Quan Ninh Kieu",
        city: "Can Tho",
        latitude: "10.012452",
        longitude: "105.732432",
        isDefault: false,
      });
      await profilePage.saveAddressButton.click();

      await expect(profilePage.toastStatusWithText("Address added.")).toBeVisible();
      await expect(profilePage.addressCardByLabel(label)).toBeVisible();
    } finally {
      await restoreAddresses(request, accessToken, originalAddresses);
    }
  });

  // TC-009: Manual - yeu cau quyen GPS trinh duyet + VietMap API + thao tac keo ghim ban do thuc te.
  test.skip("TC-009: Use my location and interactive map picker populate coordinates and address fields", async () => {});

  test("TC-010: Edit an existing saved address updates details and persists after reload", async ({
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
    const originalAddresses = await getAddressesViaApi(request, accessToken);

    try {
      const initialLabel = `Temp Edit ${Date.now()}`;
      await createAddressViaApi(request, accessToken, {
        label: initialLabel,
        addressLine: "100 Initial Street",
        ward: "An Binh",
        district: "Ninh Kieu",
        city: "Can Tho",
        latitude: 10.0124,
        longitude: 105.7324,
        isDefault: false,
      });

      await profilePage.navigate();
      const targetCard = profilePage.addressCardByLabel(initialLabel);
      await expect(targetCard).toBeVisible();
      await targetCard.getByRole("button", { name: "Edit" }).click();

      const updatedLabel = `Updated E2E ${Date.now()}`;
      const updatedLine = "456 Mau Than Street";
      await profilePage.addressLabelInput.fill(updatedLabel);
      await profilePage.addressLineInput.fill(updatedLine);
      await profilePage.saveAddressButton.click();

      await expect(profilePage.toastStatusWithText("Address updated.")).toBeVisible();
      await expect(profilePage.addressCardByLabel(updatedLabel)).toContainText(updatedLine);

      await page.reload({ waitUntil: "domcontentloaded" });
      await expect(profilePage.addressCardByLabel(updatedLabel)).toContainText(updatedLine);
    } finally {
      await restoreAddresses(request, accessToken, originalAddresses);
    }
  });

  test("TC-011: Set a non-default address as default updates Default badge and order", async ({
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
    const originalAddresses = await getAddressesViaApi(request, accessToken);

    try {
      if (originalAddresses.length === 0) {
        await createAddressViaApi(request, accessToken, {
          label: "Seed Base Default",
          addressLine: "600 Nguyen Van Cu Noi Dai",
          ward: "An Binh",
          district: "Ninh Kieu",
          city: "Can Tho",
          latitude: 10.012452,
          longitude: 105.732432,
          isDefault: true,
        });
      }

      const promotedLabel = `Promoted Default ${Date.now()}`;
      await createAddressViaApi(request, accessToken, {
        label: promotedLabel,
        addressLine: "789 Tran Hung Dao",
        ward: "An Nghiep",
        district: "Ninh Kieu",
        city: "Can Tho",
        latitude: 10.033,
        longitude: 105.775,
        isDefault: false,
      });

      await page.goto("/account/profile", { waitUntil: "domcontentloaded" });
      const promotedCard = profilePage.addressCardByLabel(promotedLabel);
      await expect(promotedCard).toBeVisible();
      await promotedCard.getByRole("button", { name: "Set default" }).click();

      await expect(profilePage.toastStatusWithText("Default address updated.")).toBeVisible({ timeout: 15_000 });

      await page.reload({ waitUntil: "domcontentloaded" });
      const topCard = profilePage.addressItems.first();
      await expect(topCard).toContainText(promotedLabel);
      await expect(topCard).toContainText("Default");
      await expect(topCard.getByRole("button", { name: "Set default" })).toHaveCount(0);
      await expect(topCard.getByRole("button", { name: "Delete" })).toHaveCount(0);
    } finally {
      await restoreAddresses(request, accessToken, originalAddresses);
    }
  });

  test("TC-012: Delete non-default address succeeds while default address cannot be deleted", async ({
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
    const originalAddresses = await getAddressesViaApi(request, accessToken);

    try {
      if (originalAddresses.length === 0) {
        await createAddressViaApi(request, accessToken, {
          label: "Seed Base Default",
          addressLine: "600 Nguyen Van Cu Noi Dai",
          ward: "An Binh",
          district: "Ninh Kieu",
          city: "Can Tho",
          latitude: 10.012452,
          longitude: 105.732432,
          isDefault: true,
        });
      }

      const deleteLabel = `Temp Delete ${Date.now()}`;
      await createAddressViaApi(request, accessToken, {
        label: deleteLabel,
        addressLine: "222 Ba Thang Hai",
        ward: "Hung Loi",
        district: "Ninh Kieu",
        city: "Can Tho",
        latitude: 10.025,
        longitude: 105.768,
        isDefault: false,
      });

      const currentAddresses = await getAddressesViaApi(request, accessToken);
      const defaultAddress = currentAddresses.find((item) => item.isDefault);
      expect(defaultAddress).toBeDefined();

      // Verify backend blocks deleting the default address (409 Conflict).
      const deleteDefaultRes = await deleteAddressViaApi(request, accessToken, defaultAddress!.id);
      expect(deleteDefaultRes.status).toBe(409);

      await profilePage.navigate();
      const defaultCard = profilePage.addressItems.filter({ hasText: "Default" }).first();
      await expect(defaultCard.getByRole("button", { name: "Delete" })).toHaveCount(0);

      const targetCard = profilePage.addressCardByLabel(deleteLabel);
      await expect(targetCard).toBeVisible();
      await targetCard.getByRole("button", { name: "Delete" }).click();

      await expect(profilePage.toastStatusWithText("Address deleted.")).toBeVisible();
      await expect(profilePage.addressCardByLabel(deleteLabel)).toHaveCount(0);

      await page.reload({ waitUntil: "domcontentloaded" });
      await expect(profilePage.addressCardByLabel(deleteLabel)).toHaveCount(0);
    } finally {
      await restoreAddresses(request, accessToken, originalAddresses);
    }
  });
});
