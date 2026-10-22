import { describe, expect, it } from "vitest";
import { formatServerDateTime } from "@/utils/serverDateTime";

describe("formatServerDateTime", () => {
  it("treats server timestamps without an offset as UTC", () => {
    expect(formatServerDateTime("2026-07-26T09:57:03")).toBe(
      formatServerDateTime("2026-07-26T09:57:03Z"),
    );
  });

  it("returns a placeholder for invalid values", () => {
    expect(formatServerDateTime("invalid")).toBe("-");
  });
});
