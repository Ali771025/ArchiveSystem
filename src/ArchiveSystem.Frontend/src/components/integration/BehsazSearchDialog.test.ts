import { describe, expect, it } from 'vitest';
import {
  buildRequest,
  resolveBehsazPageNumber,
  type BehsazSearchFormValues,
} from './BehsazSearchDialog';

const baseValues: BehsazSearchFormValues = {
  instaurationFilterType: '1',
  instaurationArea: '',
  instaurationZone: '',
  instaurationBlock: '',
  instaurationLand: '',
  instaurationBuilding: '',
  instaurationApartment: '',
  instaurationBusiness: '',
  systemNumber: '',
  fileNumber: '',
  ghateCode: '',
  mainPlate: '',
  subPlate: '',
  address: '',
  gisIndicated: '',
  taxpayerGroup: '',
  firstName: '',
  lastName: '',
  nationalCode: '',
  companyName: '',
  nationalCompanyId: '',
};

describe('buildRequest', () => {
  it('sends each natural taxpayer filter independently', () => {
    const request = buildRequest(
      {
        ...baseValues,
        taxpayerGroup: 1,
        firstName: ' علی ',
        lastName: ' احمدی ',
        nationalCode: ' 4113695181 ',
      },
      { page: 0, pageSize: 20 },
    );

    expect(request).toMatchObject({
      taxpayerGroup: 1,
      firstName: 'علی',
      lastName: 'احمدی',
      nationalCode: '4113695181',
    });
  });

  it('sends legal company name and national or economic identifier', () => {
    const request = buildRequest(
      {
        ...baseValues,
        taxpayerGroup: 2,
        companyName: ' شرکت نمونه ',
        nationalCompanyId: ' 4113695181 ',
      },
      { page: 0, pageSize: 20 },
    );

    expect(request).toMatchObject({
      taxpayerGroup: 2,
      companyName: 'شرکت نمونه',
      nationalCompanyId: '4113695181',
    });
  });
});

describe('resolveBehsazPageNumber', () => {
  it('accepts Persian digits and clamps a page to the available range', () => {
    expect(resolveBehsazPageNumber('۱۱۷۳', 1173, 1)).toBe(1173);
    expect(resolveBehsazPageNumber('9999', 1173, 1)).toBe(1173);
    expect(resolveBehsazPageNumber('0', 1173, 20)).toBe(1);
  });

  it('keeps the current page for invalid input', () => {
    expect(resolveBehsazPageNumber('صفحه آخر', 1173, 25)).toBe(25);
    expect(resolveBehsazPageNumber('', 1173, 25)).toBe(25);
  });
});
