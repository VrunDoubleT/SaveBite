import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import ts from "typescript";

// Load the pure TS helpers with a test Services key; no external API requests.
const source = readFileSync(
  new URL("../src/features/addresses/utils/addressLocation.ts", import.meta.url),
  "utf8",
);
const compiled = ts.transpile(source, {
  module: ts.ModuleKind.CommonJS,
  target: ts.ScriptTarget.ES2022,
});
const helpers = {};
new Function("require", "exports", compiled)(
  () => ({ VIETMAP_CONFIG: { SERVICES_API_KEY: "test-services-key" } }),
  helpers,
);
const { parseCoordinates, parseVietmapAddress, reverseGeocodeVietmap, geolocationErrorMessage } =
  helpers;

test("accept valid coordinates including zero and boundary values", () => {
  assert.deepEqual(parseCoordinates("0", "0"), { latitude: 0, longitude: 0 });
  assert.deepEqual(parseCoordinates(" -90 ", "180"), { latitude: -90, longitude: 180 });
  assert.deepEqual(parseCoordinates("10.759221", "106.675901"), {
    latitude: 10.759221,
    longitude: 106.675901,
  });
});
test("reject missing, nonfinite and out-of-range coordinates", () => {
  for (const [lat, lng] of [
    ["", "0"],
    ["10", " "],
    ["NaN", "1"],
    ["Infinity", "1"],
    ["91", "1"],
    ["1", "181"],
  ])
    assert.equal(parseCoordinates(lat, lng), null);
});
test("map VietMap administrative boundaries by type, regardless of order", () => {
  const result = parseVietmapAddress([
    {
      name: "197 Trần Phú",
      address: "administrative text",
      boundaries: [
        { type: 0, full_name: "Thành Phố Hồ Chí Minh" },
        { type: 2, full_name: "Phường 4" },
        { type: 1, full_name: "Quận 5" },
      ],
    },
  ]);
  assert.deepEqual(result, {
    addressLine: "197 Trần Phú",
    ward: "Phường 4",
    district: "Quận 5",
    city: "Thành Phố Hồ Chí Minh",
  });
});
test("support missing district in a two-level address without guessing", () => {
  assert.deepEqual(
    parseVietmapAddress([
      {
        name: "197 Trần Phú",
        boundaries: [
          { type: 2, prefix: "Phường", name: "Chợ Quán" },
          { type: 0, full_name: "Thành Phố Hồ Chí Minh" },
        ],
      },
    ]),
    {
      addressLine: "197 Trần Phú",
      ward: "Phường Chợ Quán",
      district: "",
      city: "Thành Phố Hồ Chí Minh",
    },
  );
});
test("allow partial administrative results for manual completion", () => {
  assert.deepEqual(parseVietmapAddress([{ boundaries: [{ type: 0, full_name: "Hà Nội" }] }]), {
    addressLine: "",
    ward: "",
    district: "",
    city: "Hà Nội",
  });
});
test("reject empty or malformed responses without inventing an address", () => {
  for (const input of [null, {}, [], [null], [{}], [{ name: 123, boundaries: "bad" }]])
    assert.throws(() => parseVietmapAddress(input));
});
test("use Reverse v4 with correct lat/lng, old-format fields and cancellation signal", async () => {
  const previous = globalThis.fetch;
  const controller = new AbortController();
  globalThis.fetch = async (url, options) => {
    assert.equal(url.pathname, "/api/reverse/v4");
    assert.equal(url.searchParams.get("lat"), "10.75");
    assert.equal(url.searchParams.get("lng"), "106.67");
    assert.equal(url.searchParams.get("display_type"), "2");
    assert.equal(options.signal, controller.signal);
    return { ok: true, json: async () => [{ name: "Test street" }] };
  };
  try {
    assert.equal(
      (await reverseGeocodeVietmap({ latitude: 10.75, longitude: 106.67 }, controller.signal))
        .addressLine,
      "Test street",
    );
  } finally {
    globalThis.fetch = previous;
  }
});
test("report HTTP failures, including rate limiting", async () => {
  const previous = globalThis.fetch;
  try {
    globalThis.fetch = async () => ({ ok: false, status: 429 });
    await assert.rejects(
      reverseGeocodeVietmap({ latitude: 0, longitude: 0 }, new AbortController().signal),
      /busy/,
    );
    globalThis.fetch = async () => ({ ok: false, status: 403 });
    await assert.rejects(
      reverseGeocodeVietmap({ latitude: 0, longitude: 0 }, new AbortController().signal),
      /Unable to look up/,
    );
  } finally {
    globalThis.fetch = previous;
  }
});
test("GPS errors offer actionable manual/map alternatives", () => {
  assert.match(geolocationErrorMessage(1), /permission was denied/);
  assert.match(geolocationErrorMessage(2), /unavailable/);
  assert.match(geolocationErrorMessage(3), /timed out/);
  assert.match(geolocationErrorMessage(99), /manually/);
});
