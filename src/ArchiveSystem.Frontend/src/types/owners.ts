export type OwnerType = 1 | 2;

export const OWNER_TYPE_LABELS: Record<OwnerType, string> = {
  1: "حقیقی",
  2: "حقوقی",
};

export interface Owner {
  id: number;
  ownerType: OwnerType;

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

export interface CreateOwnerDTO {
  ownerType: OwnerType;

  mobile?: string;
  postalCode?: string;
  address?: string;
  homePhone?: string;

  firstName?: string;
  lastName?: string;
  nationalCode?: string;
  identityNumber?: string;

  companyName?: string;
  nationalId?: string;
  registrationNumber?: string;
  taxpayerId?: string;
}

export interface OwnersListQuery {
  page: number;
  pageSize: number;
}

export interface PaginatedOwnersResult {
  items: Owner[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface OwnerSearchItem {
  id: number;
  ownerType: OwnerType;
  label: string;
  nameOrTitle: string;
  nationalIdentifier: string;
  taxpayerId?: string | null;
  mobile?: string | null;
}

export interface AttachedOwner {
  id: number;
  ownerType: OwnerType;
  nameOrTitle: string;
  nationalIdentifier: string;
  taxpayerId?: string | null;
  mobile?: string | null;
  homePhone?: string | null;
}

export const getOwnerNameOrTitle = (
  owner: Pick<Owner, "ownerType" | "firstName" | "lastName" | "companyName">,
): string => {
  return owner.ownerType === 1
    ? `${owner.firstName ?? ""} ${owner.lastName ?? ""}`.trim()
    : (owner.companyName?.trim() ?? "");
};

export const getOwnerNationalIdentifier = (
  owner: Pick<Owner, "ownerType" | "nationalCode" | "nationalId">,
): string => {
  return owner.ownerType === 1
    ? (owner.nationalCode?.trim() ?? "")
    : (owner.nationalId?.trim() ?? "");
};
