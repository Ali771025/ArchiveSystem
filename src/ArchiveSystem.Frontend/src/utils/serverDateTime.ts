const hasExplicitTimeZone = (value: string): boolean =>
  /(?:Z|[+-]\d{2}:?\d{2})$/i.test(value);

export const parseServerDateTime = (value: string): Date => {
  const normalizedValue = hasExplicitTimeZone(value) ? value : `${value}Z`;
  return new Date(normalizedValue);
};

export const formatServerDateTime = (value: string): string => {
  const date = parseServerDateTime(value);
  return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString("fa-IR");
};
