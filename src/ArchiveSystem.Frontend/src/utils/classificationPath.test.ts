import { describe, expect, it } from "vitest";
import { buildClassificationTitleMap } from "@/utils/classificationPath";

describe("buildClassificationTitleMap", () => {
  it("builds the complete classification title from root to leaf", () => {
    const titles = buildClassificationTitleMap([
      { id: 1, title: "شهرسازی", parentId: null },
      { id: 2, title: "پرونده‌های شهرسازی", parentId: 1 },
      { id: 3, title: "ناحیه یک", parentId: 2 },
    ]);

    expect(titles.get(1)).toBe("شهرسازی");
    expect(titles.get(2)).toBe("شهرسازی / پرونده‌های شهرسازی");
    expect(titles.get(3)).toBe(
      "شهرسازی / پرونده‌های شهرسازی / ناحیه یک",
    );
  });
});
