import { describe, expect, it } from "vitest";
import {
  CLASSIFICATION_MOVE_MASK,
  CLASSIFICATION_READ_MASK,
  CLASSIFICATION_WRITE_MASK,
  normalizeEditableClassificationMask,
} from "@/components/access/classificationAclMask";

describe("normalizeEditableClassificationMask", () => {
  it("removes the legacy hidden Manage bit from creator ACLs", () => {
    const legacyCreatorMask = 1 | 2 | 4 | 8;

    expect(normalizeEditableClassificationMask(legacyCreatorMask)).toBe(
      CLASSIFICATION_READ_MASK |
        CLASSIFICATION_WRITE_MASK |
        CLASSIFICATION_MOVE_MASK,
    );
    expect(normalizeEditableClassificationMask(4)).toBe(0);
  });

  it("keeps Read while Write or Move is enabled", () => {
    expect(
      normalizeEditableClassificationMask(CLASSIFICATION_WRITE_MASK),
    ).toBe(CLASSIFICATION_READ_MASK | CLASSIFICATION_WRITE_MASK);
    expect(normalizeEditableClassificationMask(CLASSIFICATION_MOVE_MASK)).toBe(
      CLASSIFICATION_READ_MASK | CLASSIFICATION_MOVE_MASK,
    );
  });
});

