import { test, expect } from "@playwright/test";
import { readFileSync } from "node:fs";
const uid = "00000000-0000-4000-8000-000000000001";
test.beforeEach(async ({ page }) => {
  // Component/network coverage does not depend on remote map libraries or fonts.
  await page.route("https://unpkg.com/**", (route) => route.abort());
  await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
});
async function ready(page: import("@playwright/test").Page) {
  await page.waitForFunction(
    () => Boolean(document.getElementById("root")?.textContent?.trim()),
    undefined,
    { timeout: 30000 },
  );
  await page.waitForFunction(() =>
    Boolean((window as unknown as { __savebiteMock?: unknown }).__savebiteMock),
  );
}
test("all declared routes load without application errors; GET inventory is intercepted", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  const audit = readFileSync(new URL("../docs/ui-redesign/ROUTES.md", import.meta.url), "utf8");
  const paths = [...audit.matchAll(/^\| \d+ \| `([^`]+)`/gm)].map((m) => m[1]);
  expect(paths).toHaveLength(75);
  for (const path of paths) {
    const role = path.startsWith("/admin")
      ? "admin"
      : path.startsWith("/store-owner")
        ? "storeOwner"
        : path.startsWith("/staff")
          ? "staff"
          : /^\/(login|register|forgot-password)/.test(path)
            ? "guest"
            : "user";
    const actual =
      path === "*"
        ? "/not-a-real-route"
        : path
            .replace(/:tab\?/g, path.includes("/admin/trust") ? "levels" : "overview")
            .replace(/:[^/]+/g, uid);
    const url = new URL(actual, "http://127.0.0.1:4175");
    url.searchParams.set("role", role);
    if (path === "/checkout") url.searchParams.set("shopId", uid);
    if (path === "/checkout/simulation") url.searchParams.set("orderId", uid);
    await page.goto(url.href, { waitUntil: "domcontentloaded" });
    await ready(page);
    await expect(page.getByText("Unexpected Application Error!", { exact: true })).toHaveCount(0);
  }
  const failures = await page.evaluate(async () => {
    const { registry } = await import("/src/mocks/handlers/registry.ts");
    const { httpClient } = await import("/src/shared/api/httpClient.ts");
    const failed: string[] = [];
    for (const route of registry.filter((route) => route.method === "GET"))
      try {
        await httpClient.get(route.path.replace(/:[^/]+/g, "00000000-0000-4000-8000-000000000001"));
      } catch {
        failed.push(route.key);
      }
    return failed;
  });
  expect(failures).toEqual([]);
  expect(errors).toEqual([]);
  expect(
    await page.evaluate(() => [
      ...(window as unknown as { __savebiteMock: { unhandledRequests: Set<string> } })
        .__savebiteMock.unhandledRequests,
    ]),
  ).toEqual([]);
});
test("search/filter/pagination and mutations persist across reloads; commerce is linked", async ({
  page,
}) => {
  await page.goto("/__ui?role=user");
  await ready(page);
  const result = await page.evaluate(async () => {
    const { httpClient: api } = await import("/src/shared/api/httpClient.ts");
    const { core } = await import("/src/mocks/coreState.ts");
    const { db, patch } = await import("/src/mocks/db.ts");
    const multipartTypes = patch(
      { latitude: 10, isOpen: true },
      { latitude: "11.25", isOpen: "false" },
    );
    const initial = await api.get("/products", { params: { page: 1, pageSize: 5 } });
    const second = await api.get("/products", { params: { page: 2, pageSize: 5 } });
    const missing = await api.get("/products", { params: { search: "DOES_NOT_EXIST" } });
    const updated = await api.put("/products/00000000-0000-4000-8000-000000000001", {
      name: "Sản phẩm đã chỉnh sửa",
    });
    const list = await api.get("/products", { params: { search: "đã chỉnh sửa" } });
    const category = await api.post("/categories", {
      name: "Danh mục thử nghiệm",
      description: "Mock CRUD",
      isActive: true,
    });
    const categories = await api.get("/categories", { params: { search: "Danh mục thử nghiệm" } });
    await api.put(`/categories/${category.data.data}`, { name: "Danh mục đã đổi" });
    const renamedCategory = await api.get("/categories", { params: { search: "Danh mục đã đổi" } });
    await api.delete(`/categories/${category.data.data}`);
    const removedCategory = await api.get("/categories", { params: { search: "Danh mục đã đổi" } });
    const cart = await api.get("/cart/page");
    const item = cart.data.data.page.items[0].items[0];
    await api.put(`/cart/items/${item.id}`, { quantity: 3 });
    const changed = await api.get("/cart/page");
    const quote = await api.post("/checkout/quotes", {
      shopId: "00000000-0000-4000-8000-000000000001",
      location: {
        latitude: 10.7769,
        longitude: 106.7009,
        accuracyMeters: 5,
        capturedAt: new Date().toISOString(),
      },
    });
    const placed = await api.post("/checkout/orders", {
      quoteId: quote.data.data.id,
      paymentMethod: "OnlinePayment",
      requestId: "preview-checkout",
    });
    const payment = await api.post(`/payments/simulation/${placed.data.data.id}`, {
      outcome: "paid",
    });
    const detail = await api.get(`/orders/${placed.data.data.id}`);
    return {
      first: initial.data.data.items.map((row) => row.id),
      second: second.data.data.items.map((row) => row.id),
      missing: missing.data.data.totalItems,
      name: updated.data.data.name,
      matched: list.data.data.items.length,
      category: category.data.data,
      categoryCount: categories.data.data.totalItems,
      categoryRenamed: renamedCategory.data.data.totalItems,
      categoryRemoved: removedCategory.data.data.totalItems,
      quantity: changed.data.data.page.items[0].items[0].quantity,
      subtotal: changed.data.data.page.items[0].items[0].subtotal,
      orderId: placed.data.data.id,
      paymentStatus: payment.data.data.paymentStatus,
      detailStatus: detail.data.data.status,
      coreOrders: core.orders.length,
      revision: db.revision,
      multipartTypes,
    };
  });
  expect(result.multipartTypes).toEqual({ latitude: 11.25, isOpen: false });
  expect(result.first).toHaveLength(5);
  expect(result.second).toHaveLength(5);
  expect(result.first.some((id) => result.second.includes(id))).toBe(false);
  expect(result.missing).toBe(0);
  expect(result.name).toBe("Sản phẩm đã chỉnh sửa");
  expect(result.matched).toBe(1);
  expect(result.categoryCount).toBe(1);
  expect(result.categoryRenamed).toBe(1);
  expect(result.categoryRemoved).toBe(0);
  expect(result.quantity).toBe(3);
  expect(result.subtotal).toBe(78000);
  expect(result.paymentStatus).toBe("Succeeded");
  expect(result.detailStatus).toBe("Pending");
  await page.reload();
  await ready(page);
  expect(
    await page.evaluate(async () => {
      const { httpClient } = await import("/src/shared/api/httpClient.ts");
      return (await httpClient.get("/products/00000000-0000-4000-8000-000000000001")).data.data
        .name;
    }),
  ).toBe("Sản phẩm đã chỉnh sửa");
});
test("empty/error/slow scenarios preserve auth, mock login and role switching", async ({
  page,
}) => {
  for (const scenario of ["empty", "error", "slow"]) {
    await page.goto(`/__ui?role=storeOwner&mock=${scenario}`);
    await ready(page);
    const result = await page.evaluate(async () => {
      const { httpClient } = await import("/src/shared/api/httpClient.ts");
      const start = performance.now();
      const me = await httpClient.get("/auth/me");
      try {
        const response = await httpClient.get("/products");
        return {
          role: me.data.data.role,
          count: response.data.data.items.length,
          elapsed: performance.now() - start,
          status: response.status,
        };
      } catch (error) {
        return {
          role: me.data.data.role,
          status: error.response.status,
          elapsed: performance.now() - start,
        };
      }
    });
    expect(result.role).toBe("storeOwner");
    if (scenario === "empty") expect(result.count).toBe(0);
    if (scenario === "error") expect(result.status).toBe(500);
    if (scenario === "slow") expect(result.elapsed).toBeGreaterThanOrEqual(1400);
  }
  await page.goto("/__ui?role=guest");
  await ready(page);
  const login = await page.evaluate(async () => {
    const { authApi } = await import("/src/features/auth/api/authApi.ts");
    const result = await authApi.login({ email: "anything", password: "anything" });
    return { token: result.accessToken, refresh: result.refreshToken };
  });
  expect(login.token).toContain(".mock");
  expect(login.refresh).toBe("mock-refresh-token");
  await page.goto("/admin/users?role=admin");
  await ready(page);
  await expect(page).toHaveURL(/admin\/users/);
});

test("shop fulfillment, remaining payment, refunds and feedback replies update projections", async ({
  page,
}) => {
  await page.goto("/__ui?role=storeOwner");
  await ready(page);
  const result = await page.evaluate(async () => {
    const { httpClient: api } = await import("/src/shared/api/httpClient.ts");
    const shop = "00000000-0000-4000-8000-000000000001",
      order = "00000000-0000-4000-8000-000000000002";
    const base = `/orders/shops/${shop}/${order}`;
    await api.post(`${base}/confirm`);
    await api.post(`${base}/preparation`, { status: "Preparing" });
    const ready = await api.post(`${base}/preparation`, { status: "ReadyForPickup" });
    const link = await api.post(`${base}/remaining-payment`);
    const payment = link.data.data.payments.find((p) => p.type === "Remaining");
    await api.post(`/payments/simulation/shops/${shop}/${order}/remaining`, {
      paymentId: payment.id,
      outcome: "paid",
    });
    const completed = await api.post(`${base}/complete`, { cashReceived: false });
    const refund = await api.post("/refunds/shop/00000000-0000-4000-8000-000000000004/transfer", {
      transferReference: "MOCK-TRANSFER",
    });
    const reply = await api.post(`/feedbacks/${shop}/reply`, { content: "Cảm ơn bạn đã ủng hộ!" });
    const before = await api.get("/feedbacks/shop");
    await api.put(`/feedbacks/${shop}/reply`, { content: "Nội dung trả lời đã cập nhật" });
    const edited = await api.get("/feedbacks/shop");
    await api.delete(`/feedbacks/${shop}/reply`);
    const after = await api.get("/feedbacks/shop");
    return {
      ready: ready.data.data.order.status,
      completed: completed.data.data.order.status,
      outstanding: completed.data.data.order.outstandingAmount,
      refundAmount: refund.data.data.transferredAmount,
      reply: reply.data.data.content,
      beforeReply: before.data.data.items.find((f) => f.id === shop).reply.content,
      editedReply: edited.data.data.items.find((f) => f.id === shop).reply.content,
      afterReply: after.data.data.items.find((f) => f.id === shop).reply,
      totalBefore: before.data.data.totalItems,
      totalAfter: after.data.data.totalItems,
    };
  });
  expect(result.ready).toBe("ReadyForPickup");
  expect(result.completed).toBe("Completed");
  expect(result.outstanding).toBe(0);
  expect(result.refundAmount).toBe(25000);
  expect(result.reply).toBe("Cảm ơn bạn đã ủng hộ!");
  expect(result.beforeReply).toBe(result.reply);
  expect(result.editedReply).toBe("Nội dung trả lời đã cập nhật");
  expect(result.afterReply).toBeNull();
  expect(result.totalBefore).toBe(result.totalAfter);
});
