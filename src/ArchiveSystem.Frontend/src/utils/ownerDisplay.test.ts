import { describe, expect, it } from 'vitest';
import { formatOwnerNames, normalizeOwnerNames } from './ownerDisplay';

describe('ownerDisplay', () => {
  it('formats JSON owner snapshots without exposing JSON', () => {
    const value = JSON.stringify([
      { FullName: 'علی رضایی' },
      { firstName: 'مریم', lastName: 'احمدی' },
      { companyName: 'شرکت نمونه' },
    ]);

    expect(formatOwnerNames(value)).toBe('علی رضایی، مریم احمدی، شرکت نمونه');
  });

  it('normalizes mixed arrays and removes duplicate names', () => {
    expect(normalizeOwnerNames(['علی رضایی', { fullName: 'علی رضایی' }, { nameOrTitle: 'شرکت نمونه' }]))
      .toEqual(['علی رضایی', 'شرکت نمونه']);
  });
});
