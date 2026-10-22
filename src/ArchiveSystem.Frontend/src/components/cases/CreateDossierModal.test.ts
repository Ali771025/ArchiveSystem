import { describe, expect, it } from 'vitest';
import type { BehsazSearchRow } from '@/types/archive';
import { getDossierFieldsFromBehsaz } from './CreateDossierModal';

const createRow = (overrides: Partial<BehsazSearchRow> = {}): BehsazSearchRow => ({
  zoningCode: '',
  systemNumber: 'SYS-100',
  fileNumber: 'FILE-100',
  archiveType: '',
  status: '',
  taxpayerGroup: '',
  taxpayerDetails: '',
  user: '',
  registrationPlate: '',
  address: ' تهران، خیابان نمونه ',
  phone: '',
  ...overrides,
});

describe('getDossierFieldsFromBehsaz', () => {
  it('copies the selected address and normalizes a valid postal code', () => {
    expect(
      getDossierFieldsFromBehsaz(
        createRow({ postalCode: ' ۱۲۳۴۵۶۷۸۹۰ ' }),
      ),
    ).toEqual({
      externalCaseNumber: 'SYS-100',
      address: 'تهران، خیابان نمونه',
      postalCode: '1234567890',
    });
  });

  it('leaves an unavailable or invalid postal code empty', () => {
    expect(
      getDossierFieldsFromBehsaz(createRow({ postalCode: '1234' })).postalCode,
    ).toBe('');
  });
});
