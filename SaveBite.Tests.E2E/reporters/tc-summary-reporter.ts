import fs from "node:fs";
import path from "node:path";
import type { FullResult, Reporter, TestCase, TestResult } from "@playwright/test/reporter";

type ExcelStatus = "Passed" | "Failed" | "Untested";

interface RecordedCase {
  tcId: string;
  tcNumber: number;
  suite: string;
  title: string;
  status: ExcelStatus;
  durationMs: number;
  note: string;
}

function parseTcInfo(rawTitle: string): { tcId: string; tcNumber: number; cleanTitle: string } {
  const match = rawTitle.match(/^(TC-\d+)\s*:\s*(.+)$/i) ?? rawTitle.match(/\b(TC-\d+)\b(.*)$/i);
  if (!match) {
    return {
      tcId: "-",
      tcNumber: Number.MAX_SAFE_INTEGER,
      cleanTitle: rawTitle.trim(),
    };
  }

  const tcId = match[1].toUpperCase();
  const tcNumber = Number.parseInt(tcId.replace("TC-", ""), 10);
  const cleanTitle = match[2].replace(/^[\s:–-]+/, "").trim() || rawTitle.trim();

  return { tcId, tcNumber, cleanTitle };
}

function mapExcelStatus(test: TestCase, result: TestResult): { status: ExcelStatus; note: string } {
  if (result.status === "skipped") {
    const skipAnnotation = test.annotations.find((item) => item.type === "skip");
    return {
      status: "Untested",
      note: skipAnnotation?.description ? `Manual / Skip: ${skipAnnotation.description}` : "Skipped",
    };
  }

  if (test.ok() && result.status === "passed") {
    return { status: "Passed", note: "" };
  }

  const rawFirstLine = result.error?.message?.split("\n")[0]?.trim() ?? "";
  const firstError = rawFirstLine.replace(/\x1B\[[0-9;]*m/g, "");
  return {
    status: "Failed",
    note: firstError,
  };
}

function formatDuration(durationMs: number): string {
  if (durationMs <= 0) {
    return "-";
  }
  if (durationMs < 1000) {
    return `${durationMs}ms`;
  }
  return `${(durationMs / 1000).toFixed(1)}s`;
}

/**
 * Custom Playwright reporter:
 * 1) In bang ket qua cuoi buoi chay sap xep dung thu tu TC-001 -> TC-xxx de doi chieu Excel.
 * 2) Luu file TSV tai test-results/tc-summary.tsv de de copy/mo bang Excel.
 */
class TcSummaryReporter implements Reporter {
  private readonly records = new Map<string, RecordedCase>();

  onTestEnd(test: TestCase, result: TestResult): void {
    const { tcId, tcNumber, cleanTitle } = parseTcInfo(test.title);
    const { status, note } = mapExcelStatus(test, result);

    this.records.set(test.id, {
      tcId,
      tcNumber,
      suite: test.parent?.title ?? "",
      title: cleanTitle,
      status,
      durationMs: result.duration,
      note,
    });
  }

  onEnd(_result: FullResult): void {
    if (this.records.size === 0) {
      return;
    }

    const sorted = Array.from(this.records.values()).sort((a, b) => {
      if (a.tcNumber !== b.tcNumber) {
        return a.tcNumber - b.tcNumber;
      }
      return a.title.localeCompare(b.title);
    });

    const passedCount = sorted.filter((item) => item.status === "Passed").length;
    const failedCount = sorted.filter((item) => item.status === "Failed").length;
    const untestedCount = sorted.filter((item) => item.status === "Untested").length;

    const lines: string[] = [
      "",
      "==================== EXCEL ROUND SUMMARY (TC-001 -> ...) ====================",
    ];

    for (const item of sorted) {
      const idCol = item.tcId.padEnd(7);
      const statusCol = item.status.padEnd(9);
      const durationCol = formatDuration(item.durationMs).padStart(6);
      const suffix = item.note ? ` [${item.note}]` : "";
      lines.push(`${idCol} | ${statusCol} | ${durationCol} | ${item.title}${suffix}`);
    }

    lines.push("-----------------------------------------------------------------------------");
    lines.push(`Tong ket: ${passedCount} Passed | ${failedCount} Failed | ${untestedCount} Untested`);

    const outputDir = path.resolve(process.cwd(), "test-results");
    fs.mkdirSync(outputDir, { recursive: true });
    const tsvPath = path.join(outputDir, "tc-summary.tsv");

    const tsvRows = [
      "TC_ID\tStatus\tDuration\tSuite\tTitle\tNote",
      ...sorted.map((item) =>
        [
          item.tcId,
          item.status,
          formatDuration(item.durationMs),
          item.suite.replace(/\t|\r?\n/g, " "),
          item.title.replace(/\t|\r?\n/g, " "),
          item.note.replace(/\t|\r?\n/g, " "),
        ].join("\t"),
      ),
    ];

    fs.writeFileSync(tsvPath, `${tsvRows.join("\n")}\n`, "utf8");
    lines.push(`Da luu file Excel/TSV: ${path.relative(process.cwd(), tsvPath)}`);
    lines.push("=============================================================================");

    console.log(lines.join("\n"));
  }
}

export default TcSummaryReporter;
