import { describe, expect, it } from 'vitest';
import { formatFooterDate, formatFooterTime } from '@/utils/footerDateTime';

describe('footer date and time formatting', () => {
  const date = new Date(2026, 6, 14, 12, 45);

  it('formats the Persian date in the requested order without commas', () => {
    expect(formatFooterDate(date)).toBe('سه شنبه 23 تیرماه 1405');
  });

  it('formats the time with Persian digits', () => {
    expect(formatFooterTime(date)).toBe('۱۲:۴۵');
  });
});
