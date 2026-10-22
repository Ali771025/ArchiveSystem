import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { caseLoansApi } from '@/features/case-loan-management/api/caseLoansApi';
import { useToastStore } from '@/stores/useToastStore';
import type {
  CaseLoanListQueryParams,
  CreateCaseLoanPayload,
  UpdateCaseLoanPayload,
} from '@/features/case-loan-management/types';

export const caseLoanQueryKeys = {
  caseLoans: (query: CaseLoanListQueryParams) => ['case-loans-v1', query] as const,
  caseLoanById: (caseLoanId: string) => ['case-loans-v1', caseLoanId] as const,
};

export const useCaseLoansQuery = (query: CaseLoanListQueryParams) =>
  useQuery({
    queryKey: caseLoanQueryKeys.caseLoans(query),
    queryFn: () => caseLoansApi.getCaseLoans(query),
    placeholderData: keepPreviousData,
  });

export const useCaseLoanByIdQuery = (caseLoanId: string | null, enabled = true) =>
  useQuery({
    queryKey: caseLoanId ? caseLoanQueryKeys.caseLoanById(caseLoanId) : ['case-loans-v1', 'null'],
    queryFn: () => caseLoansApi.getCaseLoanById(caseLoanId ?? ''),
    enabled: enabled && Boolean(caseLoanId),
  });

export const useCreateCaseLoanMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: (payload: CreateCaseLoanPayload) => caseLoansApi.createCaseLoan(payload),
    onSuccess: () => {
      pushToast('امانت پرونده با موفقیت ثبت شد.', 'success');
      queryClient.invalidateQueries({ queryKey: ['case-loans-v1'] });
    },
  });
};

export const useUpdateCaseLoanMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: ({ caseLoanId, payload }: { caseLoanId: string; payload: UpdateCaseLoanPayload }) =>
      caseLoansApi.updateCaseLoan(caseLoanId, payload),
    onSuccess: (_result, variables) => {
      pushToast('امانت پرونده با موفقیت ویرایش شد.', 'success');
      queryClient.invalidateQueries({ queryKey: ['case-loans-v1'] });
      queryClient.invalidateQueries({ queryKey: caseLoanQueryKeys.caseLoanById(variables.caseLoanId) });
    },
  });
};

export const useDeleteCaseLoanMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (caseLoanId: string) => caseLoansApi.deleteCaseLoan(caseLoanId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['case-loans-v1'] });
    },
  });
};
