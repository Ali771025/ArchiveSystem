const OWNER_NAME_KEYS = ['fullName', 'nameOrTitle', 'companyName', 'ownerName', 'label', 'name'] as const;

const collectOwnerNames = (value: unknown, names: string[]): void => {
  if (value == null) return;

  if (Array.isArray(value)) {
    value.forEach((item) => collectOwnerNames(item, names));
    return;
  }

  if (typeof value === 'string') {
    const text = value.trim();
    if (!text) return;

    if ((text.startsWith('[') && text.endsWith(']')) || (text.startsWith('{') && text.endsWith('}'))) {
      try {
        collectOwnerNames(JSON.parse(text), names);
        return;
      } catch {
        // Older records can contain ordinary text that happens to start with a bracket.
      }
    }

    names.push(text);
    return;
  }

  if (typeof value !== 'object') return;

  const owner = value as Record<string, unknown>;
  for (const key of OWNER_NAME_KEYS) {
    const matchingKey = Object.keys(owner).find((candidate) => candidate.toLowerCase() === key.toLowerCase());
    if (matchingKey && typeof owner[matchingKey] === 'string' && owner[matchingKey].trim()) {
      names.push(owner[matchingKey].trim());
      return;
    }
  }

  const read = (key: string): string => {
    const matchingKey = Object.keys(owner).find((candidate) => candidate.toLowerCase() === key.toLowerCase());
    const result = matchingKey ? owner[matchingKey] : undefined;
    return typeof result === 'string' ? result.trim() : '';
  };
  const personName = [read('firstName'), read('lastName')].filter(Boolean).join(' ');
  if (personName) names.push(personName);
};

export const normalizeOwnerNames = (value: unknown): string[] => {
  const names: string[] = [];
  collectOwnerNames(value, names);
  return Array.from(new Set(names.map((name) => name.trim()).filter(Boolean)));
};

export const formatOwnerNames = (value: unknown, emptyValue = '-'): string =>
  normalizeOwnerNames(value).join('، ') || emptyValue;

export const normalizeOwnerName = (value: unknown, emptyValue = ''): string =>
  normalizeOwnerNames(value)[0] ?? emptyValue;
