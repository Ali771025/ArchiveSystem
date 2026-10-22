import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { backupsApi, CreateBackupPayload } from '@/features/backups/api/backupsApi';
import { useToastStore } from '@/stores/useToastStore';

export const backupQueryKeys = {
  all: ['backups'] as const,
};

export const useBackupsQuery = () =>
  useQuery({
    queryKey: backupQueryKeys.all,
    queryFn: backupsApi.getBackups,
  });

export const useCreateBackupMutation = () => {
  const queryClient = useQueryClient();
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: (payload: CreateBackupPayload) => backupsApi.createBackup(payload),
    onSuccess: async () => {
      pushToast('پشتیبان‌گیری با موفقیت انجام شد.', 'success');
      await queryClient.invalidateQueries({ queryKey: backupQueryKeys.all });
    },
  });
};

export const useDownloadBackupMutation = () => {
  const pushToast = useToastStore((state) => state.push);

  return useMutation({
    mutationFn: async (fileName: string) => {
      const blob = await backupsApi.downloadBackup(fileName);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    },
    onSuccess: () => {
      pushToast('دانلود فایل پشتیبان آغاز شد.', 'success');
    },
  });
};
