const footerDateFormatter = new Intl.DateTimeFormat(
  'fa-IR-u-ca-persian-nu-latn',
  {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  },
);

const footerTimeFormatter = new Intl.DateTimeFormat('fa-IR', {
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

export const formatFooterDate = (date: Date): string => {
  const parts = Object.fromEntries(
    footerDateFormatter
      .formatToParts(date)
      .filter((part) => part.type !== 'literal')
      .map((part) => [part.type, part.value]),
  );
  const weekday = parts.weekday?.replace(/\u200c/g, ' ') ?? '';

  return `${weekday} ${parts.day} ${parts.month}ماه ${parts.year}`;
};

export const formatFooterTime = (date: Date): string =>
  footerTimeFormatter.format(date);
