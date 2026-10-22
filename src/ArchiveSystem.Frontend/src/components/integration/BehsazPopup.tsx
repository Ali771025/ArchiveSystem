import { BehsazSearchDialog } from '@/components/integration/BehsazSearchDialog';
import { useBehsazConfig } from '@/services/archiveQueries';

interface BehsazPopupProps {
  open: boolean;
  currentExternalCaseNumber: string;
  onClose: () => void;
  onSelect: (externalCaseNumber: string) => void;
}

export const BehsazPopup = ({ open, onClose, onSelect }: BehsazPopupProps) => {
  const behsazConfigQuery = useBehsazConfig();

  return (
    <BehsazSearchDialog
      open={open}
      isEnabled={Boolean(behsazConfigQuery.data)}
      onClose={onClose}
      onRecordSelect={(row) => {
        onSelect(row.systemNumber || row.fileNumber);
      }}
    />
  );
};
