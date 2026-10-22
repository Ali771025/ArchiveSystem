import { useRef } from "react";

export const useStableRowCount = (rowCount: number | undefined) => {
  const rowCountRef = useRef(rowCount ?? 0);

  if (rowCount !== undefined) {
    rowCountRef.current = rowCount;
  }

  return rowCountRef.current;
};
