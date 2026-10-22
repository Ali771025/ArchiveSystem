import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ownersApi } from '@/services/ownersApi';
import { useToastStore } from '@/stores/useToastStore';
import type { AttachedOwner, CreateOwnerDTO, OwnerSearchItem } from '@/types/owners';

export const dossierOwnersQueryKey = (dossierId: number) => ['dossier-owners', dossierId] as const;
export const dossierOwnersByCaseNumberQueryKey = (caseNumber: string) => ['dossier-owners-by-case-number', caseNumber] as const;

interface AttachOwnerVariables {
  ownerId: number;
  ownerSnapshot?: OwnerSearchItem;
}

interface DetachOwnerVariables {
  ownerId: number;
}

interface OwnersMutationContext {
  previousOwners: AttachedOwner[];
}

export const useDossierOwners = (dossierId: number | null, caseNumber?: string | null) => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);
  const normalizedCaseNumber = caseNumber?.trim() ?? '';
  const hasCaseNumber = normalizedCaseNumber.length > 0;
  const key = hasCaseNumber
    ? dossierOwnersByCaseNumberQueryKey(normalizedCaseNumber)
    : dossierId
      ? dossierOwnersQueryKey(dossierId)
      : (['dossier-owners', 'empty'] as const);

  const ownersQuery = useQuery({
    queryKey: key,
    queryFn: async () => {
      if (hasCaseNumber) {
        return ownersApi.getDossierOwnersByCaseNumber(normalizedCaseNumber);
      }

      return ownersApi.getDossierOwners(dossierId ?? 0);
    },
    enabled: hasCaseNumber || dossierId !== null,
  });

  const attachOwnerMutation = useMutation<void, Error, AttachOwnerVariables, OwnersMutationContext>({
    mutationFn: (variables: AttachOwnerVariables) => {
      if (!hasCaseNumber && dossierId === null) {
        return Promise.reject(new Error('Dossier id is required.'));
      }

      if (hasCaseNumber) {
        return ownersApi.attachOwnerToCaseNumber(normalizedCaseNumber, variables.ownerId);
      }

      return ownersApi.attachOwnerToDossier(dossierId!, variables.ownerId);
    },
    onMutate: async (variables) => {
      if (!hasCaseNumber && dossierId === null) {
        return { previousOwners: [] as AttachedOwner[] };
      }

      await queryClient.cancelQueries({ queryKey: key });
      const previousOwners = queryClient.getQueryData<AttachedOwner[]>(key) ?? [];

      const isAlreadyAttached = previousOwners.some((owner) => owner.id === variables.ownerId);
      if (!isAlreadyAttached && variables.ownerSnapshot) {
        queryClient.setQueryData<AttachedOwner[]>(key, [
          ...previousOwners,
          {
            id: variables.ownerSnapshot.id,
            ownerType: variables.ownerSnapshot.ownerType,
            nameOrTitle: variables.ownerSnapshot.nameOrTitle,
            nationalIdentifier: variables.ownerSnapshot.nationalIdentifier,
            mobile: variables.ownerSnapshot.mobile ?? null,
            homePhone: null,
          },
        ]);
      }

      return { previousOwners };
    },
    onError: (_error, _variables, context) => {
      if (!hasCaseNumber && dossierId === null) {
        return;
      }

      if (context?.previousOwners) {
        queryClient.setQueryData(key, context.previousOwners);
      }

      pushToast('اتصال مالک به پرونده ناموفق بود.', 'error');
    },
    onSuccess: () => {
      pushToast('مالک با موفقیت به پرونده متصل شد.', 'success');
    },
    onSettled: async () => {
      if (!hasCaseNumber && dossierId === null) {
        return;
      }

      await queryClient.invalidateQueries({ queryKey: key });
    },
  });

  const detachOwnerMutation = useMutation<void, Error, DetachOwnerVariables, OwnersMutationContext>({
    mutationFn: async (variables: DetachOwnerVariables) => {
      if (!hasCaseNumber && dossierId === null) {
        return Promise.reject(new Error('Dossier id is required.'));
      }

      if (hasCaseNumber) {
        await ownersApi.detachOwnerFromCaseNumber(normalizedCaseNumber, variables.ownerId);
        return;
      }

      await ownersApi.detachOwnerFromDossier(dossierId!, variables.ownerId);
    },
    onMutate: async (variables) => {
      if (!hasCaseNumber && dossierId === null) {
        return { previousOwners: [] as AttachedOwner[] };
      }

      await queryClient.cancelQueries({ queryKey: key });
      const previousOwners = queryClient.getQueryData<AttachedOwner[]>(key) ?? [];

      queryClient.setQueryData<AttachedOwner[]>(
        key,
        previousOwners.filter((owner) => owner.id !== variables.ownerId),
      );

      return { previousOwners };
    },
    onError: (_error, _variables, context) => {
      if (!hasCaseNumber && dossierId === null) {
        return;
      }

      if (context?.previousOwners) {
        queryClient.setQueryData(key, context.previousOwners);
      }

      pushToast('حذف مالک از پرونده ناموفق بود.', 'error');
    },
    onSuccess: () => {
      pushToast('مالک از پرونده حذف شد.', 'success');
    },
    onSettled: async () => {
      if (!hasCaseNumber && dossierId === null) {
        return;
      }

      await queryClient.invalidateQueries({ queryKey: key });
    },
  });

  return {
    owners: ownersQuery.data ?? [],
    ownersQuery,
    attachOwner: attachOwnerMutation.mutateAsync,
    detachOwner: detachOwnerMutation.mutateAsync,
    isAttachPending: attachOwnerMutation.isPending,
    isDetachPending: detachOwnerMutation.isPending,
  };
};

export const useCreateOwner = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: (payload: CreateOwnerDTO) => ownersApi.createOwner(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['owners-search'] });
      await queryClient.invalidateQueries({ queryKey: ['owners'] });
      pushToast('مالک جدید با موفقیت ایجاد شد.', 'success');
    },
    onError: () => {
      pushToast('ایجاد مالک جدید ناموفق بود.', 'error');
    },
  });
};

export const useUpdateOwner = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: ({ ownerId, payload }: { ownerId: number; payload: CreateOwnerDTO }) => ownersApi.updateOwner(ownerId, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['owners-search'] });
      await queryClient.invalidateQueries({ queryKey: ['owners'] });
      await queryClient.invalidateQueries({ queryKey: ['dossier-owners'] });
      await queryClient.invalidateQueries({ queryKey: ['dossier-owners-by-case-number'] });
      pushToast('اطلاعات مالک با موفقیت ویرایش شد.', 'success');
    },
    onError: () => {
      pushToast('ویرایش اطلاعات مالک ناموفق بود.', 'error');
    },
  });
};
