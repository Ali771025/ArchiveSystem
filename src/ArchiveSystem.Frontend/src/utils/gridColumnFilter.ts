import type { GridFilterModel } from "@mui/x-data-grid";

export interface ServerColumnFilter {
  columnFilterField?: string;
  columnFilterOperator?: string;
  columnFilterValue?: string;
}

export const getServerColumnFilter = (
  model: GridFilterModel,
): ServerColumnFilter => {
  const item = model.items.find(
    (candidate) =>
      candidate.field &&
      candidate.operator &&
      candidate.value !== undefined &&
      candidate.value !== null &&
      String(candidate.value).trim() !== "",
  );

  if (!item) return {};

  return {
    columnFilterField: item.field,
    columnFilterOperator: item.operator,
    columnFilterValue: String(item.value).trim(),
  };
};
