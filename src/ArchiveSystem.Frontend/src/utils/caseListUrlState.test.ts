import { describe, expect, it } from "vitest";
import {
  parseCaseListUrlState,
  serializeDynamicFilters,
} from "@/utils/caseListUrlState";
import type { DynamicFilterExpression } from "@/types/archive";

describe("case list URL state", () => {
  it("restores classification, paging, sorting and standard filters", () => {
    const state = parseCaseListUrlState(
      new URLSearchParams({
        classificationId: "42",
        page: "3",
        pageSize: "20",
        ownerName: "علی رضایی",
        caseNumber: "P-1405-17",
        fromDate: "1405/01/01",
        toDate: "1405/02/01",
        tagId: "7",
        sortBy: "caseNumber",
        sortDirection: "asc",
      }),
    );

    expect(state).toMatchObject({
      classificationId: 42,
      page: 3,
      pageSize: 20,
      ownerName: "علی رضایی",
      caseNumber: "P-1405-17",
      fromDate: "1405/01/01",
      toDate: "1405/02/01",
      tagId: 7,
      sortBy: "caseNumber",
      sortDirection: "asc",
    });
  });

  it("uses safe defaults for malformed public URL values", () => {
    const state = parseCaseListUrlState(
      new URLSearchParams({
        classificationId: "-1",
        page: "zero",
        pageSize: "500",
        tagId: "NaN",
        sortBy: "notAColumn",
        sortDirection: "sideways",
        dynamicFilters: "{broken",
      }),
    );

    expect(state).toMatchObject({
      classificationId: null,
      page: 1,
      pageSize: 10,
      tagId: null,
      sortBy: "updatedAt",
      sortDirection: "desc",
      dynamicFilterExpression: undefined,
    });
  });

  it("round-trips a valid dynamic filter expression", () => {
    const expression: DynamicFilterExpression = {
      logic: "and",
      groups: [
        {
          logic: "or",
          filters: [{ fieldId: "owner-code", op: "contains", value: "123" }],
        },
      ],
    };
    const serialized = serializeDynamicFilters(expression);
    const state = parseCaseListUrlState(
      new URLSearchParams({ dynamicFilters: serialized ?? "" }),
    );

    expect(state.dynamicFilterExpression).toEqual(expression);
  });
});
