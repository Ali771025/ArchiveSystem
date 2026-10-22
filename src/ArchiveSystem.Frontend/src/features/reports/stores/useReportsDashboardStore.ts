import { create } from 'zustand';
import type {
  AuditActionType,
  AuditLogResponseDto,
  AuditTableFilters,
  SecurityTableFilters,
  TablePaginationState,
} from '@/features/reports/types';

interface ReportsDashboardState {
  activeTab: number;
  auditFilters: AuditTableFilters;
  auditPagination: TablePaginationState;
  fileDossierFilters: AuditTableFilters;
  fileDossierPagination: TablePaginationState;
  securityFilters: SecurityTableFilters;
  securityPagination: TablePaginationState;
  selectedDiffLog: AuditLogResponseDto | null;
  isDiffModalOpen: boolean;
  setActiveTab: (tab: number) => void;
  setAuditFilters: (filters: Partial<AuditTableFilters>) => void;
  setAuditPagination: (pagination: Partial<TablePaginationState>) => void;
  setFileDossierFilters: (filters: Partial<AuditTableFilters>) => void;
  setFileDossierPagination: (pagination: Partial<TablePaginationState>) => void;
  setSecurityFilters: (filters: Partial<SecurityTableFilters>) => void;
  setSecurityPagination: (pagination: Partial<TablePaginationState>) => void;
  openDiffModal: (log: AuditLogResponseDto) => void;
  closeDiffModal: () => void;
}

const defaultAuditFilters: AuditTableFilters = {
  startDate: '',
  endDate: '',
  userId: '',
  entityTypes: [],
  actionTypes: [],
};

const defaultPagination: TablePaginationState = {
  pageNumber: 1,
  pageSize: 20,
  sortBy: 'ActionDate',
  sortDirection: 'desc',
};

const defaultFileDossierFilters: AuditTableFilters = {
  ...defaultAuditFilters,
  entityTypes: ['Cases', 'Dossiers', 'Files'],
};

const defaultSecurityFilters: SecurityTableFilters = {
  startDate: '',
  endDate: '',
  actorUserId: '',
  targetUserId: '',
  eventTypes: [],
};

const normalizeActionTypes = (actionTypes: AuditActionType[]) => {
  const order: AuditActionType[] = ['Create', 'Update', 'Delete'];
  const selectedActions = Array.from(
    new Set(actionTypes.map((item) => item.trim()).filter(Boolean)),
  );

  return [
    ...order.filter((item) => selectedActions.includes(item)),
    ...selectedActions.filter((item) => !order.includes(item)),
  ];
};

export const useReportsDashboardStore = create<ReportsDashboardState>((set) => ({
  activeTab: 0,
  auditFilters: defaultAuditFilters,
  auditPagination: { ...defaultPagination },
  fileDossierFilters: defaultFileDossierFilters,
  fileDossierPagination: { ...defaultPagination },
  securityFilters: defaultSecurityFilters,
  securityPagination: { ...defaultPagination },
  selectedDiffLog: null,
  isDiffModalOpen: false,
  setActiveTab: (tab) => set({ activeTab: tab }),
  setAuditFilters: (filters) =>
    set((state) => ({
      auditFilters: {
        ...state.auditFilters,
        ...filters,
        actionTypes: filters.actionTypes ? normalizeActionTypes(filters.actionTypes) : state.auditFilters.actionTypes,
      },
      auditPagination: {
        ...state.auditPagination,
        pageNumber: 1,
      },
    })),
  setAuditPagination: (pagination) =>
    set((state) => ({
      auditPagination: {
        ...state.auditPagination,
        ...pagination,
      },
    })),
  setFileDossierFilters: (filters) =>
    set((state) => ({
      fileDossierFilters: {
        ...state.fileDossierFilters,
        ...filters,
        actionTypes: filters.actionTypes
          ? normalizeActionTypes(filters.actionTypes)
          : state.fileDossierFilters.actionTypes,
      },
      fileDossierPagination: {
        ...state.fileDossierPagination,
        pageNumber: 1,
      },
    })),
  setFileDossierPagination: (pagination) =>
    set((state) => ({
      fileDossierPagination: {
        ...state.fileDossierPagination,
        ...pagination,
      },
    })),
  setSecurityFilters: (filters) =>
    set((state) => ({
      securityFilters: {
        ...state.securityFilters,
        ...filters,
      },
      securityPagination: {
        ...state.securityPagination,
        pageNumber: 1,
      },
    })),
  setSecurityPagination: (pagination) =>
    set((state) => ({
      securityPagination: {
        ...state.securityPagination,
        ...pagination,
      },
    })),
  openDiffModal: (log) => set({ selectedDiffLog: log, isDiffModalOpen: true }),
  closeDiffModal: () => set({ selectedDiffLog: null, isDiffModalOpen: false }),
}));
