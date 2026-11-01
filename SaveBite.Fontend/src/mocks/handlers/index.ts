import { core, saveCore } from "../coreState";
import { enumFields } from "./enum-fields";
import { http, HttpResponse, delay, passthrough } from "msw";
import { registry } from "./registry";
import {
  bodyFor,
  db,
  clone,
  object,
  payload,
  replacePayload,
  findList,
  patch,
  updateFeature,
  save,
  id,
  type Json,
  type Row,
} from "../db";
import { filtered, paginate } from "./lists";
import { commerce } from "./commerce";
import { currentUser, tokens, userMutation } from "../auth";
import { scenario, emptyLists, applyVariant } from "../scenarios";
import { emit } from "../realtime";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
const json = (value: unknown): Json => JSON.parse(JSON.stringify(value)) as Json;
const base = new URL(import.meta.env.VITE_API_URL || "/api", location.origin);
const prefix = base.pathname.replace(/\/$/, "");
type Route = (typeof registry)[number];
function pattern(path: string) {
  return new RegExp(
    "^" +
      path
        .split("/")
        .map((part) =>
          part.startsWith(":") ? "([^/]+)" : part.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"),
        )
        .join("/") +
      "/?$",
  );
}
const routes = [...registry].sort((a, b) => {
  const score = (r: Route) =>
    r.path.split("/").reduce((s, x) => s + (x.startsWith(":") ? 1 : 10), 0);
  return (
    score(b) - score(a) ||
    JSON.stringify(bodyFor(b.key)).length - JSON.stringify(bodyFor(a.key)).length
  );
});
export const matchedRequests = new Set<string>();
export const unhandledRequests = new Set<string>();
export function matchRoute(method: string, path: string) {
  return routes.find((route) => route.method === method && pattern(route.path).test(path));
}
async function input(request: Request): Promise<Row> {
  if (request.method === "GET" || request.method === "HEAD") return {};
  if (request.headers.get("content-type")?.includes("multipart/form-data")) {
    const form = await request.formData();
    return Object.fromEntries(
      [...form.entries()].map(([key, value]) => [
        key,
        typeof value === "string" ? value : "/images/deals/banh-mi/bm-overview.jpg",
      ]),
    );
  }
  try {
    const value: unknown = await request.json();
    return object(value) ? value : {};
  } catch {
    return {};
  }
}
function error(status: number, message: string) {
  return HttpResponse.json(
    { message, errorCode: `MOCK_${status}`, statusCode: status },
    { status },
  );
}
function successful(data: unknown) {
  return { success: true, message: "Thao tác thành công.", data: json(data) };
}
function genericMutation(route: Route, path: string, data: Row, body: Json): Json {
  const value = payload(body);
  const parts = path.split("/");
  const names = route.path.split("/");
  const paramIndex = names.findLastIndex((x) => x.startsWith(":"));
  let targetId = paramIndex >= 0 ? parts[paramIndex] : "";
  if (route.feature === "shops" && /updateBusinessInfo|updateImages/.test(route.name))
    targetId = id(0);
  if (route.method === "POST" && !targetId && route.name.match(/create|invite|add/i)) {
    const newId = id(1000 + db.revision);
    for (const getRoute of registry.filter(
      (r) => r.method === "GET" && r.feature === route.feature,
    )) {
      const list = findList(payload(db.responses[getRoute.key]));
      if (list && object(list[0])) {
        const created = clone(list[0]);
        created.id = newId;
        patch(created, data);
        list.unshift(created);
      }
    }
    if (object(value)) {
      if ("id" in value) value.id = newId;
      patch(value, data);
    } else if (typeof value === "string") return replacePayload(body, newId);
  } else {
    const patchData = { ...data };
    if (typeof data.Logo === "string") patchData.logoUrl = data.Logo;
    if (typeof data.CoverImage === "string") patchData.coverImageUrl = data.CoverImage;
    if (route.feature === "feedback") {
      patchData.status = route.name === "remove" ? "HiddenByAdmin" : "Visible";
      patchData.currentReason = data.reason ?? null;
    }
    if (route.name === "recordExternalPayment") {
      patchData.status = "Paid";
      patchData.paidAt = new Date().toISOString();
    }

    if (/acceptInvitation/.test(route.name)) patchData.status = "Accepted";
    if (/declineInvitation/.test(route.name)) patchData.status = "Declined";
    if (/cancel/.test(route.name)) patchData.status = "Cancelled";
    if (/updateStatus/.test(route.name) && typeof data.isSuspended === "boolean")
      patchData[data.isCustomerProfile ? "customerStatus" : "status"] = data.isSuspended
        ? "Suspended"
        : "Active";
    if (/markAsRead/.test(route.name)) {
      patchData.isRead = true;
      patchData.readAt = new Date().toISOString();
    }
    if (route.name === "markAllAsRead") {
      for (const getRoute of registry.filter(
        (r) => r.feature === "notifications" && r.method === "GET",
      )) {
        const stored = payload(db.responses[getRoute.key]);
        const list = findList(stored);
        list?.forEach((item) => {
          if (object(item)) {
            item.isRead = true;
            item.readAt = new Date().toISOString();
          }
        });
        if (object(stored) && "unreadCount" in stored) stored.unreadCount = 0;
      }
    }
    if (object(value)) {
      patch(value, patchData);
      if (targetId && "id" in value) value.id = targetId;
    }
    if (!targetId)
      for (const sibling of registry.filter(
        (r) => r.feature === route.feature && r.method === "GET" && r.path === route.path,
      )) {
        const stored = payload(db.responses[sibling.key]);
        patch(stored, patchData);
      }

    if (targetId)
      updateFeature(
        route.feature,
        targetId,
        patchData,
        route.method === "DELETE" && !/removeForViolation|remove$|deleteReply/.test(route.name),
      );
  }
  if (route.feature === "feedbacks" && /replyFeedback|editReply|deleteReply/.test(route.name)) {
    const reply = payload(body);
    if (object(reply)) patch(reply, data);
    for (const sibling of registry.filter((r) => r.feature === "feedbacks" && r.method === "GET")) {
      const stored = payload(db.responses[sibling.key]);
      const records = findList(stored) ?? (object(stored) ? [stored] : []);
      records.forEach((item) => {
        if (object(item) && item.id === targetId)
          item.reply = route.name === "deleteReply" ? null : clone(reply);
      });
    }
  }
  if (route.feature === "feedbacks") {
    const feedback = payload(body);
    for (const order of core.orders)
      for (const item of order.items) {
        if (
          route.name === "addFeedback" &&
          item.orderItemId === data.orderItemId &&
          object(feedback)
        ) {
          item.feedbackId = String(feedback.id);
          item.rating = Number(data.rating) || 5;
          item.canReview = false;
        }
        if (route.name === "deleteFeedback" && item.feedbackId === targetId) {
          item.feedbackId = null;
          item.rating = null;
          item.canReview = true;
        }
      }
    saveCore();
  }
  save();
  return body;
}
export const handlers = [
  http.all("*", async ({ request }) => {
    const url = new URL(request.url);
    if (url.hostname === "api.vietqr.io" && url.pathname === "/v2/banks")
      return HttpResponse.json({
        code: "00",
        data: [
          { bin: "970436", name: "Ngân hàng TMCP Ngoại thương Việt Nam", shortName: "Vietcombank" },
          { bin: "970422", name: "Ngân hàng Quân Đội", shortName: "MB Bank" },
        ],
      });
    if (url.hostname === "maps.vietmap.vn") {
      if (url.pathname.includes("reverse"))
        return HttpResponse.json([
          {
            name: "12 Nguyễn Huệ",
            boundaries: [
              { type: 2, full_name: "Phường Bến Nghé" },
              { type: 1, full_name: "Quận 1" },
              { type: 0, full_name: "TP. Hồ Chí Minh" },
            ],
          },
        ]);
      if (url.pathname.includes("route"))
        return HttpResponse.json({
          code: "OK",
          paths: [
            {
              distance: 1200,
              time: 600000,
              points: "szw`As_gjSoKoK",
              instructions: [{ text: "Đi thẳng đến cửa hàng" }],
            },
          ],
        });
      return HttpResponse.json({
        version: 8,
        sources: {},
        layers: [
          { id: "background", type: "background", paint: { "background-color": "#eef2f0" } },
        ],
      });
    }
    if (url.hostname.endsWith("tile.openstreetmap.org"))
      return new HttpResponse(
        '<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><path fill="#f7f9f8" d="M0 0h256v256H0z"/><path stroke="#dce3e0" d="M0 64h256M0 128h256M0 192h256M64 0v256M128 0v256M192 0v256"/></svg>',
        { headers: { "Content-Type": "image/svg+xml" } },
      );
    if (
      url.origin !== base.origin ||
      (prefix && url.pathname !== prefix && !url.pathname.startsWith(prefix + "/"))
    )
      return passthrough();
    const path = url.pathname.slice(prefix.length) || "/";
    const method = request.method;
    // Session endpoints remain available while testing data failures/empty states.
    const auth = path.startsWith("/auth/");
    if (!auth && scenario() === "slow") await delay(1500);
    if (!auth && scenario() === "error") return error(500, "Mock scenario: unable to load data.");
    if (path === "/auth/token-renewals" || path === "/auth/sessions") {
      sessionStorage.removeItem("savebite-mock-guest");
      return HttpResponse.json(successful(tokens()));
    }
    if (path === "/auth/me") return HttpResponse.json(successful(currentUser()));
    if (path === "/auth/session-revocations") {
      sessionStorage.setItem("savebite-mock-guest", "true");
      return HttpResponse.json({ success: true, message: "Signed out." });
    }
    const route = matchRoute(method, path);
    if (!route) {
      unhandledRequests.add(`${method} ${path}`);
      console.error(`[mock] Unhandled API: ${method} ${path}`);
      return error(501, `Mock endpoint not implemented: ${method} ${path}`);
    }
    matchedRequests.add(`${method} ${route.path}`);
    const data = await input(request);
    if (path === "/users/me" || path === "/users/me/avatar") {
      const userBody = userMutation(
        path.endsWith("/avatar") ? { avatarUrl: "/images/deals/banh-mi/bm-overview.jpg" } : data,
      );
      if (userBody) db.responses["auth-authApi_getMe_2"] = userBody;
      save();
      return HttpResponse.json(successful(currentUser()));
    }
    const special = commerce(method, path, data, url.searchParams);
    if (special?.status && special.status >= 400)
      return error(
        special.status,
        object(special.data) ? String(special.data.message) : "Mock request failed",
      );
    let body = special ? successful(special.data) : bodyFor(route.key);
    if (!special && method !== "GET") body = genericMutation(route, path, data, body);
    if (!special && method === "GET") {
      const current = payload(body);
      const match = pattern(route.path).exec(path);
      const target = match?.at(-1);
      // Detail pages share names/status with existing list rows of their resource.
      if (target && object(current) && "id" in current) {
        for (const sibling of registry.filter(
          (r) => r.feature === route.feature && r.method === "GET",
        )) {
          const list = findList(payload(db.responses[sibling.key]));
          const item = list?.find((row) => object(row) && row.id === target);
          if (object(item)) {
            patch(current, item);
            break;
          }
        }
        current.id = target;
      }
    }
    const current = payload(body);
    if (route.feature === "notifications" && object(current)) {
      if ("unreadCount" in current) {
        const list = findList(current);
        if (list) current.unreadCount = list.filter((n) => object(n) && !n.isRead).length;
      }
    }
    if (method === "GET") body = replacePayload(body, paginate(current, url.searchParams));
    if (!auth && method === "GET" && scenario() === "empty") body = emptyLists(body);
    if (method !== "GET" && route.feature === "notifications")
      emit(REALTIME_EVENTS.NOTIFICATION_READ, {
        notificationId: path.split("/")[2],
        unreadCount: 0,
        readAt: new Date().toISOString(),
      });
    return HttpResponse.json(applyVariant(body, enumFields[route.key] || []));
  }),
];
// Export the same search implementation for contract tests.
export { filtered };
