import { api } from '@/services/api';
import type {
  AttachedOwner,
  CreateOwnerDTO,
  Owner,
  OwnerSearchItem,
  OwnersListQuery,
  PaginatedOwnersResult,
} from '@/types/owners';
import { normalizeOwnerName } from '@/utils/ownerDisplay';

interface OwnerApi {
  id: number;
  ownerType: 1 | 2;
  mobile?: string | null;
  postalCode?: string | null;
  address?: string | null;
  homePhone?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  nationalCode?: string | null;
  identityNumber?: string | null;
  companyName?: string | null;
  nationalId?: string | null;
  registrationNumber?: string | null;
  taxpayerId?: string | null;
}

interface DossierOwnerApi {
  ownerId: number;
  ownerType: 1 | 2;
  nameOrTitle: unknown;
  nationalIdentifier: string;
  taxpayerId?: string | null;
  mobile?: string | null;
  homePhone?: string | null;
}

interface OwnerSearchApi {
  id: number;
  ownerType: 1 | 2;
  label: unknown;
  nameOrTitle: unknown;
  nationalIdentifier: string;
  mobile?: string | null;
}

const mapOwnerApi = (owner: OwnerApi): Owner => ({
  id: owner.id,
  ownerType: owner.ownerType,
  mobile: owner.mobile ?? null,
  postalCode: owner.postalCode ?? null,
  address: owner.address ?? null,
  homePhone: owner.homePhone ?? null,
  firstName: owner.firstName ?? null,
  lastName: owner.lastName ?? null,
  nationalCode: owner.nationalCode ?? null,
  identityNumber: owner.identityNumber ?? null,
  companyName: owner.companyName ?? null,
  nationalId: owner.nationalId ?? null,
  registrationNumber: owner.registrationNumber ?? null,
  taxpayerId: owner.taxpayerId ?? null,
});

const mapDossierOwnerApi = (owner: DossierOwnerApi): AttachedOwner => ({
  id: owner.ownerId,
  ownerType: owner.ownerType,
  nameOrTitle: normalizeOwnerName(owner.nameOrTitle),
  nationalIdentifier: owner.nationalIdentifier,
  taxpayerId: owner.taxpayerId ?? null,
  mobile: owner.mobile ?? null,
  homePhone: owner.homePhone ?? null,
});

const mapOwnerSearchApi = (owner: OwnerSearchApi): OwnerSearchItem => ({
  id: owner.id,
  ownerType: owner.ownerType,
  label: normalizeOwnerName(owner.label ?? owner.nameOrTitle),
  nameOrTitle: normalizeOwnerName(owner.nameOrTitle ?? owner.label),
  nationalIdentifier: owner.nationalIdentifier,
  mobile: owner.mobile ?? null,
});

export const ownersApi = {
  async createOwner(payload: CreateOwnerDTO): Promise<Owner> {
    const response = await api.post<OwnerApi>('/owners', payload);
    return mapOwnerApi(response.data);
  },

  async getOwnerById(ownerId: number): Promise<Owner> {
    const response = await api.get<OwnerApi>(`/owners/${ownerId}`);
    return mapOwnerApi(response.data);
  },

  async updateOwner(ownerId: number, payload: CreateOwnerDTO): Promise<Owner> {
    const response = await api.put<OwnerApi>(`/owners/${ownerId}`, payload);
    return mapOwnerApi(response.data);
  },

  async listOwners(query: OwnersListQuery): Promise<PaginatedOwnersResult> {
    const response = await api.get<PaginatedOwnersResult>('/owners', {
      params: {
        page: query.page,
        pageSize: query.pageSize,
      },
    });

    return {
      ...response.data,
      items: response.data.items.map(mapOwnerApi),
    };
  },

  async searchOwners(searchTerm: string, take = 20): Promise<OwnerSearchItem[]> {
    const response = await api.get<OwnerSearchApi[]>('/owners/search', {
      params: {
        query: searchTerm,
        take,
      },
    });

    return response.data.map(mapOwnerSearchApi);
  },

  async getDossierOwners(dossierId: number): Promise<AttachedOwner[]> {
    const response = await api.get<DossierOwnerApi[]>(`/dossiers/${dossierId}/owners`);
    return response.data.map(mapDossierOwnerApi);
  },

  async getDossierOwnersByCaseNumber(caseNumber: string): Promise<AttachedOwner[]> {
    const response = await api.get<DossierOwnerApi[]>(`/dossiers/by-case-number/${encodeURIComponent(caseNumber)}/owners`);
    return response.data.map(mapDossierOwnerApi);
  },

  async attachOwnerToDossier(dossierId: number, ownerId: number): Promise<void> {
    await api.post(`/dossiers/${dossierId}/owners`, { ownerId });
  },

  async attachOwnerToCaseNumber(caseNumber: string, ownerId: number): Promise<void> {
    await api.post(`/dossiers/by-case-number/${encodeURIComponent(caseNumber)}/owners`, { ownerId });
  },

  async attachTaxpayerToDossier(dossierId: number, taxpayerId: string): Promise<void> {
    await api.post(`/dossiers/${dossierId}/owners`, { taxpayerId });
  },

  async attachTaxpayerToCaseNumber(caseNumber: string, taxpayerId: string): Promise<void> {
    await api.post(`/dossiers/by-case-number/${encodeURIComponent(caseNumber)}/owners`, { taxpayerId });
  },

  async detachOwnerFromDossier(dossierId: number, ownerId: number): Promise<void> {
    await api.delete(`/dossiers/${dossierId}/owners/${ownerId}`);
  },

  async detachOwnerFromCaseNumber(caseNumber: string, ownerId: number): Promise<void> {
    await api.delete(`/dossiers/by-case-number/${encodeURIComponent(caseNumber)}/owners/${ownerId}`);
  },
};
