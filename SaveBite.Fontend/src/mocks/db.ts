import { responses } from "./factories";
import { registry } from "./handlers/registry";
export type Json = string | number | boolean | null | Json[] | { [key: string]: Json };
export type Row = { [key: string]: Json };
export const object = (value: unknown): value is Row =>
  typeof value === "object" && value !== null && !Array.isArray(value);
export const clone = <T>(value: T): T => structuredClone(value);
export const id = (index: number) =>
  `00000000-0000-4000-8000-${String(index + 1).padStart(12, "0")}`;
const storageKey = "savebite-mock-db-v1";
export interface Database {
  responses: Record<string, Json>;
  revision: number;
}
function json(value: unknown): Json {
  return JSON.parse(JSON.stringify(value)) as Json;
}
/** Update only fields that already exist in the wire fixture, never add made-up DTO fields. */
export function patch(target: Json, input: Row): Json {
  if (!object(target)) return target;
  for (const key of Object.keys(target))
    if (key in input) {
      const current = target[key],
        incoming = input[key];
      // Multipart forms stringify coordinates and booleans; retain the response DTO types.
      if (typeof current === "number" && typeof incoming === "string") {
        if (Number.isFinite(Number(incoming))) target[key] = Number(incoming);
      } else if (typeof current === "boolean" && typeof incoming === "string") {
        if (incoming === "true" || incoming === "false") target[key] = incoming === "true";
      } else if (object(current) && object(incoming)) patch(current, incoming);
      else target[key] = clone(incoming);
    }
  return target;
}
function expand(value: Json): void {
  if (Array.isArray(value)) {
    value.forEach(expand);
    return;
  }
  if (!object(value)) return;
  if (Array.isArray(value.items) && value.items.length) {
    const templates = clone(value.items);
    value.items = Array.from({ length: 18 }, (_, index) => {
      const row = clone(templates[index % templates.length]);
      if (object(row)) {
        if ("id" in row) row.id = id(index);
        if (object(row.order)) row.order.id = id(index);
      }
      return row;
    });
    if ("totalItems" in value) value.totalItems = 18;
    if ("total" in value) value.total = 18;
    if ("totalPages" in value) value.totalPages = 2;
    if ("page" in value) value.page = 1;
  }
  Object.values(value).forEach(expand);
}
function seed(): Database {
  const data = json(responses);
  if (!object(data)) throw new Error("Invalid mock seed");
  for (const value of Object.values(data)) {
    if (object(value) && "success" in value) value.success = true;
    if (object(value) && "message" in value) value.message = "Thao tác thành công.";
    expand(value);
  }
  const application = data["shop-applications-shopApplicationApi_getMine_0"];
  if (object(application) && object(application.data)) application.data.status = "NeedsRevision";
  return { responses: data, revision: 0 };
}
export function loadDatabase(): Database {
  try {
    const value: unknown = JSON.parse(sessionStorage.getItem(storageKey) || "null");
    if (object(value) && object(value.responses) && typeof value.revision === "number")
      return { responses: value.responses, revision: value.revision };
  } catch {
    /* Seed again when a preview session is invalid. */
  }
  return seed();
}
export const db = loadDatabase();
export function save() {
  db.revision++;
  sessionStorage.setItem(storageKey, JSON.stringify(db));
}
export function reset() {
  Object.assign(db, seed());
  save();
}
export function bodyFor(key: string): Json {
  const body = db.responses[key];
  if (body === undefined) throw new Error(`No typed mock fixture: ${key}`);
  return clone(body);
}
export function payload(body: Json): Json {
  return object(body) && "data" in body ? body.data : body;
}
export function replacePayload(body: Json, data: Json): Json {
  if (object(body) && "data" in body) {
    body.data = data;
    return body;
  }
  return data;
}
export function findList(value: Json): Json[] | undefined {
  if (Array.isArray(value)) return value;
  if (!object(value)) return;
  if (Array.isArray(value.items)) return value.items;
  if (Array.isArray(value.deals)) return value.deals;
  if (object(value.page)) return findList(value.page);
  return;
}
/** Mutations sync GET projections of the same feature; page fixtures keep their original shape. */
export function updateFeature(feature: string, targetId: string, input: Row, remove = false) {
  for (const route of registry.filter((r) => r.method === "GET" && r.feature === feature)) {
    const stored = db.responses[route.key];
    if (stored === undefined) continue;
    const data = payload(stored);
    const list = findList(data);
    if (list) {
      for (let index = list.length - 1; index >= 0; index--) {
        const item = list[index];
        if (object(item) && item.id === targetId) {
          if (remove) list.splice(index, 1);
          else patch(item, input);
        }
      }
    } else if (object(data) && data.id === targetId) patch(data, input);
  }
  save();
}
