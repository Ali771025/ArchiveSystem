import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { taxpayersApi } from '@/features/taxpayer-management/api/taxpayersApi';
import { useToastStore } from '@/stores/useToastStore';
import type {
  CreateTaxpayerPayload,
  TaxpayerListQueryParams,
  UpdateTaxpayerPayload,
} from '@/features/taxpayer-management/types';

export const taxpayerQueryKeys = {
  taxpayers: (query: TaxpayerListQueryParams) => ['taxpayers-v1', query] as const,
  taxpayerById: (taxpayerId: string) => ['taxpayers-v1', taxpayerId] as const,
};

export const useTaxpayersQuery = (query: TaxpayerListQueryParams) =>
  useQuery({
    queryKey: taxpayerQueryKeys.taxpayers(query),
    queryFn: () => taxpayersApi.getTaxpayers(query),
    placeholderData: keepPreviousData,
  });

export const useTaxpayerByIdQuery = (taxpayerId: string | null, enabled = true) =>
  useQuery({
    queryKey: taxpayerId ? taxpayerQueryKeys.taxpayerById(taxpayerId) : ['taxpayers-v1', 'null'],
    queryFn: () => taxpayersApi.getTaxpayerById(taxpayerId ?? ''),
    enabled: enabled && Boolean(taxpayerId),
  });

export const useCreateTaxpayerMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: (payload: CreateTaxpayerPayload) => taxpayersApi.createTaxpayer(payload),
    onSuccess: () => {
      pushToast('مودی با موفقیت ایجاد شد.', 'success');
      queryClient.invalidateQueries({ queryKey: ['taxpayers-v1'] });
    },
  });
};

export const useUpdateTaxpayerMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: ({ taxpayerId, payload }: { taxpayerId: string; payload: UpdateTaxpayerPayload }) =>
      taxpayersApi.updateTaxpayer(taxpayerId, payload),
    onSuccess: (_result, variables) => {
      pushToast('مودی با موفقیت ویرایش شد.', 'success');
      queryClient.invalidateQueries({ queryKey: ['taxpayers-v1'] });
      queryClient.invalidateQueries({ queryKey: taxpayerQueryKeys.taxpayerById(variables.taxpayerId) });
    },
  });
};

export const useDeleteTaxpayerMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (taxpayerId: string) => taxpayersApi.deleteTaxpayer(taxpayerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taxpayers-v1'] });
    },
  });
};
