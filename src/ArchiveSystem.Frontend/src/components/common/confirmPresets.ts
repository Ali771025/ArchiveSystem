import type { ConfirmOptions } from '@/components/common/ConfirmContext';

export const buildDeleteConfirmOptions = (entityLabel: string): ConfirmOptions => ({
  title: 'تأیید حذف',
  message: `این عملیات قابل بازگشت نیست. آیا از حذف ${entityLabel} مطمئن هستید؟`,
  confirmText: 'بله، حذف کن',
  cancelText: 'انصراف',
  variant: 'danger',
});
