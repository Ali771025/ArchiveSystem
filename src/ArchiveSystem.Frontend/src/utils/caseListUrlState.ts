import type {
  CaseSearchRequest,
  DynamicFilterExpression,
} from "@/types/archive";

export const CASE_LIST_QUERY_KEYS = {
  classificationId: "classificationId",
  page: "page",
  pageSize: "pageSize",
  ownerName: "ownerName",
  caseNumber: "caseNumber",
  fromDate: "fromDate",
  toDate: "toDate",
  tagId: "tagId",
  sortBy: "sortBy",
  sortDirection: "sortDirection",
  dynamicFilters: "dynamicFilters",
} as const;

const DEFAULT_PAGE = 1;
const DEFAULT_PAGE_SIZE = 10;
const DEFAULT_SORT_BY: CaseSearchRequest["sortBy"] = "updatedAt";
const DEFAULT_SORT_DIRECTION: CaseSearchRequest["sortDirection"] = "desc";
const ALLOWED_PAGE_SIZES = new Set([10, 20, 50]);
const ALLOWED_SORT_FIELDS = new Set<CaseSearchRequest["sortBy"]>([
  "caseNumber",
  "externalCaseNumber",
  "address",
  "updatedAt",
  "classificationTitle",
]);

export interface CaseListUrlState {
  classificationId: number | null;
  page: number;
  pageSize: number;
  ownerName: string;
  caseNumber: string;
  fromDate: string;
  toDate: string;
  tagId: number | null;
  sortBy: CaseSearchRequest["sortBy"];
  sortDirection: CaseSearchRequest["sortDirection"];
  dynamicFilterExpression?: DynamicFilterExpression;
}

const parsePositiveInteger = (
  value: string | null,
  fallback: number | null,
): number | null => {
  if (!value || !/^\d+$/.test(value)) {
    return fallback;
  }

  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : fallback;
};

const isDynamicFilterExpression = (
  value: unknown,
): value is DynamicFilterExpression => {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Partial<DynamicFilterExpression>;
  return (
    (candidate.logic === "and" || candidate.logic === "or") &&
    Array.isArray(candidate.groups) &&
    candidate.groups.every(
      (group) =>
        Boolean(group) &&
        (group.logic === "and" || group.logic === "or") &&
        Array.isArray(group.filters),
    )
  );
};

const parseDynamicFilters = (
  value: string | null,
): DynamicFilterExpression | undefined => {
  if (!value) {
    return undefined;
  }

  try {
    const parsed: unknown = JSON.parse(value);
    return isDynamicFilterExpression(parsed) ? parsed : undefined;
  } catch {
    return undefined;
  }
};

export const parseCaseListUrlState = (
  searchParams: URLSearchParams,
): CaseListUrlState => {
  const parsedPageSize = parsePositiveInteger(
    searchParams.get(CASE_LIST_QUERY_KEYS.pageSize),
    DEFAULT_PAGE_SIZE,
  );
  const requestedSortBy = searchParams.get(CASE_LIST_QUERY_KEYS.sortBy);
  const requestedSortDirection = searchParams.get(
    CASE_LIST_QUERY_KEYS.sortDirection,
  );

  return {
    classificationId: parsePositiveInteger(
      searchParams.get(CASE_LIST_QUERY_KEYS.classificationId),
      null,
    ),
    page:
      parsePositiveInteger(
        searchParams.get(CASE_LIST_QUERY_KEYS.page),
        DEFAULT_PAGE,
      ) ?? DEFAULT_PAGE,
    pageSize:
      parsedPageSize != null && ALLOWED_PAGE_SIZES.has(parsedPageSize)
        ? parsedPageSize
        : DEFAULT_PAGE_SIZE,
    ownerName: searchParams.get(CASE_LIST_QUERY_KEYS.ownerName)?.trim() ?? "",
    caseNumber:
      searchParams.get(CASE_LIST_QUERY_KEYS.caseNumber)?.trim() ?? "",
    fromDate: searchParams.get(CASE_LIST_QUERY_KEYS.fromDate)?.trim() ?? "",
    toDate: searchParams.get(CASE_LIST_QUERY_KEYS.toDate)?.trim() ?? "",
    tagId: parsePositiveInteger(
      searchParams.get(CASE_LIST_QUERY_KEYS.tagId),
      null,
    ),
    sortBy:
      requestedSortBy &&
      ALLOWED_SORT_FIELDS.has(requestedSortBy as CaseSearchRequest["sortBy"])
        ? (requestedSortBy as CaseSearchRequest["sortBy"])
        : DEFAULT_SORT_BY,
    sortDirection:
      requestedSortDirection === "asc" || requestedSortDirection === "desc"
        ? requestedSortDirection
        : DEFAULT_SORT_DIRECTION,
    dynamicFilterExpression: parseDynamicFilters(
      searchParams.get(CASE_LIST_QUERY_KEYS.dynamicFilters),
    ),
  };
};

export const setOptionalQueryParam = (
  searchParams: URLSearchParams,
  key: string,
  value: string | number | null | undefined,
) => {
  if (value == null || value === "") {
    searchParams.delete(key);
    return;
  }

  searchParams.set(key, String(value));
};

export const serializeDynamicFilters = (
  expression?: DynamicFilterExpression,
): string | null => (expression ? JSON.stringify(expression) : null);

