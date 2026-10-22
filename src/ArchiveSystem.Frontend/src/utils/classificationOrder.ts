export interface ClassificationOrderItem {
  title?: string;
  name?: string;
  systemKey?: string | null;
}

export const ARCHIVED_CASES_SYSTEM_KEY = 'ArchivedCases';
export const ARCHIVED_CASES_TITLE = 'بایگانی راکد';

export const isArchivedCasesClassification = (item: ClassificationOrderItem): boolean =>
  item.systemKey === ARCHIVED_CASES_SYSTEM_KEY ||
  (item.title ?? item.name ?? '').trim() === ARCHIVED_CASES_TITLE;

export const compareClassificationsArchiveLast = (
  left: ClassificationOrderItem,
  right: ClassificationOrderItem,
): number => {
  const leftIsArchive = isArchivedCasesClassification(left);
  const rightIsArchive = isArchivedCasesClassification(right);

  if (leftIsArchive !== rightIsArchive) {
    return leftIsArchive ? 1 : -1;
  }

  return (left.title ?? left.name ?? '').localeCompare(right.title ?? right.name ?? '', 'fa');
};
