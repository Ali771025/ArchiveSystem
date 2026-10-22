import DateObject from 'react-date-object';

const pad = (value: number): string => value.toString().padStart(2, '0');

export const parseDateLikeValue = (value: string | null | undefined): Date | null => {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  const dateMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(trimmed);
  if (dateMatch) {
    const [, year, month, day] = dateMatch;
    return new Date(Number(year), Number(month) - 1, Number(day));
  }

  const localDateTimeMatch = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/.exec(trimmed);
  if (localDateTimeMatch) {
    const [, year, month, day, hour, minute, second] = localDateTimeMatch;
    return new Date(
      Number(year),
      Number(month) - 1,
      Number(day),
      Number(hour),
      Number(minute),
      Number(second ?? '0'),
      0,
    );
  }

  const parsed = new Date(trimmed);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
};

export const toDateObject = (value: string | null | undefined): DateObject | null => {
  const parsed = parseDateLikeValue(value);
  return parsed ? new DateObject(parsed) : null;
};

export const toGregorianDateString = (date: Date): string => {
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
};

export const toGregorianDateTimeString = (date: Date): string => {
  return `${toGregorianDateString(date)}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

export const toIsoUtcString = (value: string | null | undefined): string | undefined => {
  if (!value) {
    return undefined;
  }

  const trimmed = value.trim();
  if (!trimmed) {
    return undefined;
  }

  if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
    return `${trimmed}T00:00:00.000Z`;
  }

  const parsed = parseDateLikeValue(value);
  return parsed ? parsed.toISOString() : undefined;
};
