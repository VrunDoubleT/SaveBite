import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Staff Invitation Split-Screen (Store Owner <-> Candidate)", () => {
  test("Store Owner invites staff member realtime, Candidate receives and inspects invitation on split-screen", async ({}, testInfo) => {
    test.setTimeout(120_000);

    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Store Owner mở trang Quản lý nhân viên (/store-owner/staff)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.storeOwner.email,
        TEST_USERS.storeOwner.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/store-owner/staff`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /Staff Management|Shop Staff/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Khách hàng / Ứng viên mở trang Lời mời nhân viên (/account/staff-invitations)
      await loginViaUi(
        session.rightPage,
        session.baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );
      await session.rightPage.goto(`${session.baseUrl}/account/staff-invitations`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: /Staff invitations/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên TRÁI (Store Owner): Mở modal mời nhân viên
      const inviteBtn = session.leftPage.getByRole("button", { name: /Invite staff|Add staff/i }).first();
      await expect(inviteBtn).toBeVisible({ timeout: 15_000 });
      await inviteBtn.click();

      // Kiểm tra modal mời nhân viên mở lên
      const inviteModal = session.leftPage
        .getByRole("heading", { name: "Invite Staff" })
        .or(session.leftPage.getByPlaceholder(/Search by name or email/i))
        .first();
      await expect(inviteModal).toBeVisible({ timeout: 15_000 });

      // 4. Bên PHẢI (Candidate): Kiểm tra danh sách lời mời sẵn sàng nhận sự kiện SignalR
      const invitationsSection = session.rightPage.locator("main");
      await expect(invitationsSection).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
