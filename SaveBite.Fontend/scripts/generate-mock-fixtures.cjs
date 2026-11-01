/* Generate initial wire-response fixtures from the actual generic types used by services.
   Generated TypeScript uses `satisfies` so DTO changes fail type-check instead of drifting. */
const ts = require("typescript"),
  fs = require("node:fs"),
  path = require("node:path");
const root = path.resolve(__dirname, ".."),
  src = path.join(root, "src");
const config = ts.readConfigFile(path.join(root, "tsconfig.app.json"), ts.sys.readFile);
const parsed = ts.parseJsonConfigFileContent(config.config, ts.sys, root);
const program = ts.createProgram(
    parsed.fileNames.filter((f) => f.includes("/features/") && f.includes("/api/")),
    parsed.options,
  ),
  checker = program.getTypeChecker();
const registry = [];
const enumFields = {};
const uuid = (n) => `00000000-0000-4000-8000-${String(n + 1).padStart(12, "0")}`;
function str(key, n, ctx) {
  if (/^(id|.*Id)$/.test(key)) return uuid(/^(shop|category|level|user)Id$/.test(key) ? 0 : n);
  if (/url|image|photo|logo|avatar|cover|fileUrl/i.test(key))
    return "/images/deals/banh-mi/bm-overview.jpg";
  if (/email/i.test(key)) return `minh${n + 1}@example.test`;
  if (/phone/i.test(key)) return "0901234567";
  if (/name|holder|actor/i.test(key))
    return /shop|store/i.test(key + ctx)
      ? ["Bếp Xanh Sài Gòn", "Tiệm Bánh An Nhiên", "Góc Rau Đà Lạt"][n % 3]
      : /user|customer|actor|replied|fullName|displayName|holder/i.test(key)
        ? `__person_${n}__`
        : /category/i.test(key + ctx)
          ? ["Bánh mì", "Rau củ", "Bánh ngọt"][n % 3]
          : ["Bánh mì rau củ", "Túi bánh cuối ngày", "Hộp rau tươi Đà Lạt"][n % 3];
  if (/latitude/i.test(key)) return 10.7769;
  if (/openingTime/i.test(key)) return "07:00:00";
  if (/closingTime/.test(key)) return "23:00:00";
  if (/startDate|endDate|^date$/.test(key)) return "2026-11-02";
  if (/At$|Time$|Utc$|expires/i.test(key))
    return /end|deadline|expires|due|closing/i.test(key) ? "__future__" : "__past__";
  if (/status/i.test(key))
    return /refund/i.test(ctx)
      ? ["AwaitingCustomerDetails", "AwaitingTransfer", "Transferred"][n % 3]
      : /order/i.test(ctx)
        ? ["Pending", "Confirmed", "Completed"][n % 3]
        : /payment/i.test(ctx)
          ? "Succeeded"
          : "Active";
  if (/role/i.test(key)) return "user";
  if (/customerStatus/.test(key)) return "active";
  if (/address/i.test(key)) return "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh";
  if (/city/i.test(key)) return "TP. Hồ Chí Minh";
  if (/ward/i.test(key)) return "Bến Nghé";
  if (/district/i.test(key)) return "Quận 1";
  if (/description|content|comment|reason|note|body|correction/i.test(key))
    return n % 3 === 2
      ? "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa."
      : "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.";
  if (/title|label/i.test(key)) return ["Đơn hàng mới", "Đã xác nhận", "Cập nhật cửa hàng"][n % 3];
  if (/bankBin/.test(key)) return "970436";
  if (/bankName/.test(key)) return "Vietcombank";
  if (/accountNumber|bankAccountNumber/.test(key)) return "0123456789";
  if (/orderCode/.test(key)) return `SB${String(1000 + n)}`;
  if (/cursor|restriction|unavailable|encodedPolyline/i.test(key)) return "";
  if (/purpose/i.test(key)) return "registration";
  if (/sku/i.test(key)) return `SB-BM-${n + 1}`;
  if (/granularity/.test(key)) return "day";
  if (/preset/.test(key)) return "today";
  return ["Tiêu chuẩn", "Cỡ lớn", "Không cay"][n % 3];
}
function fixture(type, key = "", n = 0, depth = 0, ctx = "", trail = []) {
  const sym = type.aliasSymbol || type.symbol;
  const name = sym?.getName() || "";
  ctx = name === "__type" ? ctx : name || ctx;
  if (type.flags & ts.TypeFlags.Boolean)
    return !/hasMore|hasNext|hasPrevious|hasOlder|hasNewer|isNew|isOverdue|isRead|isDefaultLevel|isLoggingOut|blocked|reviewRequired/i.test(
      key,
    );
  if (type.isUnion()) {
    const candidates = type.types.filter(
      (t) => !(t.flags & (ts.TypeFlags.Undefined | ts.TypeFlags.Null)),
    );
    if (!candidates.length) return null;
    if (candidates.every((t) => t.flags & ts.TypeFlags.BooleanLiteral))
      return !/hasMore|hasNext|hasPrevious|hasOlder|hasNewer|isNew|isOverdue|isRead|isDefaultLevel|blocked|reviewRequired/i.test(
        key,
      );
    return fixture(candidates[n % candidates.length], key, n, depth, ctx, trail);
  }
  if (type.flags & ts.TypeFlags.StringLiteral) return type.value;
  if (type.flags & ts.TypeFlags.NumberLiteral) return type.value;
  if (type.flags & ts.TypeFlags.BooleanLiteral) return type.intrinsicName === "true";
  if (type.flags & ts.TypeFlags.String) return str(key, n, ctx);
  if (type.flags & ts.TypeFlags.Number) {
    if (/latitude/i.test(key)) return 10.7769 + n * 0.001;
    if (/longitude/i.test(key)) return 106.7009 + n * 0.001;
    if (/rating/i.test(key)) return 4 + (n % 2);
    if (/status/i.test(key)) return /flashdeal/i.test(ctx) ? 1 : 0;
    if (/pageSize|limit/.test(key)) return 10;
    if (/page|rank|month/.test(key)) return 1;
    if (/year/i.test(key)) return 2026;
    if (/percent|rate/i.test(key)) return 25;
    if (/price|amount|revenue|subtotal|fee|deposit/i.test(key)) return 25000 * (n + 1);
    return /quantity/i.test(key) ? (/available|total/i.test(key) ? 20 : 1) : n + 1;
  }
  if (type.flags & ts.TypeFlags.Boolean)
    return !/hasMore|hasNext|hasPrevious|hasOlder|hasNewer|isNew|isOverdue|isRead|isDefaultLevel|isLoggingOut|blocked|reviewRequired/i.test(
      key,
    );
  if (type.flags & (ts.TypeFlags.Null | ts.TypeFlags.Undefined)) return null;
  if (depth > 13 || trail.filter((t) => t === type).length > 1) return null;
  const args = checker.getTypeArguments(type);
  if (checker.isArrayType(type) || checker.isTupleType(type)) {
    const el = args[0];
    if (!el) return [];
    const enumWidth = Math.max(
      0,
      ...checker.getPropertiesOfType(el).map((p) => {
        const t = checker.getTypeOfSymbolAtLocation(p, p.valueDeclaration || p.declarations?.[0]);
        return t.isUnion()
          ? t.types.filter(
              (x) => x.flags & (ts.TypeFlags.StringLiteral | ts.TypeFlags.NumberLiteral),
            ).length
          : 0;
      }),
    );
    const count = Math.max(
      enumWidth,
      key === "items" || (/\[\]$/.test(checker.typeToString(type)) && depth < 2) ? 3 : 2,
    );
    return Array.from({ length: count }, (_, i) =>
      fixture(el, key, i, depth + 1, ctx, [...trail, type]),
    );
  }
  const props = checker.getPropertiesOfType(type);
  if (!props.length) {
    if (
      checker.getIndexTypeOfType(type, ts.IndexKind.String) ||
      checker.getIndexTypeOfType(type, ts.IndexKind.Number)
    )
      return {};
    return null;
  }
  const result = {};
  for (const p of props) {
    const k = p.getName();
    if (k.startsWith("__")) continue;
    if (
      p.flags & ts.SymbolFlags.Optional &&
      (/activeWarning|warningComparison|latestReview|restriction|unavailableReason|rejectReason|cancellationUnavailableReason|cancellationPolicy|reply|refund|^route$/.test(
        k,
      ) ||
        n % 3 === 2)
    )
      continue;
    const pt = checker.getTypeOfSymbolAtLocation(p, p.valueDeclaration || p.declarations?.[0]);
    result[k] = fixture(pt, k, n, depth + 1, ctx, [...trail, type]);
  }
  return result;
}
function enumPaths(type, routePath = [], depth = 0) {
  if (depth > 12) return [];
  if (type.isUnion()) {
    const literals = type.types.filter(
      (t) => t.flags & (ts.TypeFlags.StringLiteral | ts.TypeFlags.NumberLiteral),
    );
    if (literals.length > 1) return [{ path: routePath, values: literals.map((t) => t.value) }];
    const nonNull = type.types.find(
      (t) => !(t.flags & (ts.TypeFlags.Null | ts.TypeFlags.Undefined)),
    );
    return nonNull ? enumPaths(nonNull, routePath, depth + 1) : [];
  }
  if (checker.isArrayType(type))
    return enumPaths(checker.getTypeArguments(type)[0], [...routePath, "*"], depth + 1);
  if (type.flags & ts.TypeFlags.Object)
    return checker
      .getPropertiesOfType(type)
      .flatMap((p) =>
        enumPaths(
          checker.getTypeOfSymbolAtLocation(p, p.valueDeclaration || p.declarations?.[0]),
          [...routePath, p.getName()],
          depth + 1,
        ),
      );
  return [];
}
function resolve(from, s) {
  return s.startsWith("@/")
    ? s
    : s.startsWith(".")
      ? "@/" + path.relative(src, path.resolve(path.dirname(from), s)).replaceAll("\\", "/")
      : s;
}
function rootMethod(node) {
  let p = node.parent;
  while (p) {
    if (ts.isMethodDeclaration(p) || ts.isFunctionDeclaration(p)) return p;
    p = p.parent;
  }
  return null;
}
function walk(n, fn) {
  fn(n);
  ts.forEachChild(n, (x) => walk(x, fn));
}
// Symbolic URL evaluation mirrors endpoint helpers without executing service modules.
const epFile = program.getSourceFile(path.join(src, "shared/api/endpoints.ts"));
let ep = {};
function evalNode(n, sf, env = {}, seen = []) {
  if (!n) return "?";
  if (ts.isStringLiteral(n) || ts.isNoSubstitutionTemplateLiteral(n)) return n.text;
  if (ts.isNumericLiteral(n)) return Number(n.text);
  if (ts.isTemplateExpression(n))
    return (
      n.head.text +
      n.templateSpans
        .map((x) => String(evalNode(x.expression, sf, env, seen)) + x.literal.text)
        .join("")
    );
  if (ts.isPropertyAccessExpression(n)) {
    const obj = evalNode(n.expression, sf, env, seen);
    return typeof obj === "object" && obj ? obj[n.name.text] : `:${n.name.text}`;
  }
  if (ts.isObjectLiteralExpression(n))
    return Object.fromEntries(
      n.properties
        .filter(ts.isPropertyAssignment)
        .map((p) => [p.name.getText(sf), evalNode(p.initializer, sf, env, seen)]),
    );
  if (ts.isAsExpression(n) || ts.isParenthesizedExpression(n))
    return evalNode(n.expression, sf, env, seen);
  if (ts.isArrowFunction(n))
    return (...args) =>
      evalNode(
        n.body,
        sf,
        {
          ...env,
          ...Object.fromEntries(
            n.parameters.map((p, i) => [p.name.getText(sf), args[i] ?? ":" + p.name.getText(sf)]),
          ),
        },
        seen,
      );
  if (ts.isConditionalExpression(n))
    return evalNode(n.whenTrue, sf, env, seen) + "||" + evalNode(n.whenFalse, sf, env, seen);
  if (ts.isCallExpression(n)) {
    if (n.expression.getText(sf) === "encodeURIComponent")
      return evalNode(n.arguments[0], sf, env, seen);
    const fn = evalNode(n.expression, sf, env, seen);
    return typeof fn === "function"
      ? fn(...n.arguments.map((x) => evalNode(x, sf, env, seen)))
      : ":id";
  }
  if (ts.isIdentifier(n)) {
    if (n.text === "API_ENDPOINTS") return ep;
    if (n.text in env) return env[n.text];
    let init;
    walk(sf, (x) => {
      if (ts.isVariableDeclaration(x) && x.name.getText(sf) === n.text) init = x.initializer;
    });
    if (init && !seen.includes(n.text)) return evalNode(init, sf, env, [...seen, n.text]);
    return ":" + n.text;
  }
  return ":id";
}
walk(epFile, (n) => {
  if (ts.isVariableDeclaration(n) && n.name.getText(epFile) === "API_ENDPOINTS")
    ep = evalNode(n.initializer, epFile);
});
for (const sf of program
  .getSourceFiles()
  .filter(
    (f) =>
      f.fileName.startsWith(src + "/features/") &&
      f.fileName.includes("/api/") &&
      f.fileName.endsWith(".ts"),
  )) {
  const calls = [];
  walk(sf, (n) => {
    if (
      ts.isCallExpression(n) &&
      ts.isPropertyAccessExpression(n.expression) &&
      n.expression.expression.getText(sf) === "httpClient" &&
      ["get", "post", "put", "delete", "patch"].includes(n.expression.name.text)
    )
      calls.push(n);
  });
  if (!calls.length) continue;
  const feature = path.relative(src + "/features", sf.fileName).split(path.sep)[0];
  const filename = feature + "-" + path.basename(sf.fileName, ".ts");
  const generics = calls.map((n) => n.typeArguments?.[0]?.getText(sf) || "unknown");
  const locals = sf.statements.filter(
    (n) => ts.isInterfaceDeclaration(n) || ts.isTypeAliasDeclaration(n),
  );
  let used = generics.join("\n");
  let relevant = [];
  for (let i = 0; i < 4; i++)
    for (const l of locals)
      if (new RegExp("\\b" + l.name.text + "\\b").test(used) && !relevant.includes(l)) {
        relevant.push(l);
        used += "\n" + l.getText(sf);
      }
  const imports = [];
  for (const node of sf.statements.filter(ts.isImportDeclaration)) {
    const c = node.importClause;
    if (!c?.namedBindings || !ts.isNamedImports(c.namedBindings)) continue;
    const selected = c.namedBindings.elements.filter((e) =>
      new RegExp("\\b" + e.name.text + "\\b").test(used),
    );
    if (selected.length)
      imports.push(
        `import type { ${selected.map((e) => e.getText(sf)).join(", ")} } from ${JSON.stringify(resolve(sf.fileName, node.moduleSpecifier.text))};`,
      );
  }
  let out =
    '// Generated from real service response DTOs. Regenerate with npm run mock:generate.\nimport { person, past, future } from "./values";\n' +
    imports.join("\n") +
    "\n" +
    relevant.map((x) => x.getText(sf)).join("\n") +
    "\n";
  const keys = [];
  calls.forEach((call, i) => {
    const m = rootMethod(call),
      methodName = m?.name?.getText(sf) || "request";
    const key = filename + "_" + methodName + "_" + i;
    const generic = call.typeArguments?.[0];
    if (generic) enumFields[key] = enumPaths(checker.getTypeFromTypeNode(generic));
    let data = generic
      ? fixture(checker.getTypeFromTypeNode(generic), "", 0, 0)
      : { success: true, message: "Thao tác thành công." };
    let body = JSON.stringify(data, null, 2)
      .replace(/"__person_(\d+)__"/g, "person($1)")
      .replace(/"__future__"/g, "future")
      .replace(/"__past__"/g, "past");
    out += `export const response${i} = ${body} satisfies ${generic ? generic.getText(sf) : "unknown"};\n`;
    keys.push(`  ${JSON.stringify(key)}: response${i},`);
    const url = evalNode(call.arguments[0], sf);
    const alternatives = String(url).split("||");
    for (const raw of alternatives) {
      const u = raw.startsWith("/") ? raw : alternatives[0].replace(/[^/]+$/, raw);
      registry.push({
        key,
        feature,
        method: call.expression.name.text.toUpperCase(),
        path: u,
        name: methodName,
        source: path.relative(root, sf.fileName),
        type: generic?.getText(sf) || "unknown",
      });
    }
  });
  out += "export default {\n" + keys.join("\n") + "\n};\n";
  out = out
    .replace(
      'import { person, past, future } from "./values";',
      `import { ${["person", "past", "future"].filter((x) => new RegExp("\\b" + x + "\\b").test(out.slice(out.indexOf("\n") + 1).replace('import { person, past, future } from "./values";', ""))).join(", ")} } from "./values";`,
    )
    .replace('import {  } from "./values";\n', "");
  fs.writeFileSync(path.join(src, "mocks/factories", filename + ".ts"), out);
}
const names = [...new Set(registry.map((r) => r.key.replace(/_[^_]+_\d+$/, "")))];
let index = names.map((name, i) => `import fixture${i} from "./${name}";`).join("\n");
index +=
  "\nexport const responses = {\n" + names.map((_, i) => `...fixture${i},`).join("\n") + "\n};\n";
fs.writeFileSync(path.join(src, "mocks/factories/index.ts"), index);
fs.writeFileSync(
  path.join(src, "mocks/handlers/registry.ts"),
  "// Generated from real HTTP callsites; route order is resolved by specificity.\nexport const registry = " +
    JSON.stringify(registry, null, 2) +
    " as const;\n",
);
fs.writeFileSync(
  path.join(src, "mocks/handlers/enum-fields.ts"),
  "// Finite values extracted from the real DTO unions.\nexport const enumFields: Record<string, {path:string[];values:(string|number)[]}[]> = " +
    JSON.stringify(enumFields, null, 2) +
    ";\n",
);
console.log(
  `Generated ${registry.length} HTTP route variants in ${names.length} typed fixture modules.`,
);
