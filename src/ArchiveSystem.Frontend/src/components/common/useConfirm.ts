import { useConfirmContext } from '@/components/common/ConfirmContext';

export const useConfirm = () => useConfirmContext().confirm;
