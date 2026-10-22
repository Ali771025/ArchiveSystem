import CloseIcon from '@mui/icons-material/Close';
import SearchIcon from '@mui/icons-material/Search';
import { Dialog, DialogContent, DialogTitle, IconButton, Stack, Typography } from '@mui/material';
import { useEffect } from 'react';
import { AdvancedSearchForm } from '@/components/search/AdvancedSearchForm';
import { SearchResults } from '@/components/search/SearchResults';
import { useAdvancedSearchStore } from '@/stores/useAdvancedSearchStore';

interface GlobalCaseSearchDialogProps {
  open: boolean;
  onClose: () => void;
}

export const GlobalCaseSearchDialog = ({ open, onClose }: GlobalCaseSearchDialogProps) => {
  const syncStateFromUrl = useAdvancedSearchStore((state) => state.syncStateFromUrl);

  useEffect(() => {
    if (open) {
      void syncStateFromUrl();
    }
  }, [open, syncStateFromUrl]);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="lg">
      <DialogTitle sx={{ position: 'relative', pl: 7 }}>
        <IconButton
          aria-label="بستن"
          onClick={onClose}
          size="small"
          sx={{ position: 'absolute', left: 16, top: 16 }}
        >
          <CloseIcon fontSize="small" />
        </IconButton>
        <Stack direction="row" spacing={1} alignItems="center">
          <SearchIcon color="primary" />
          <Stack spacing={0.25}>
            <Typography variant="h6" fontWeight={800}>
              جستجوی پیشرفته
            </Typography>
            <Typography variant="body2" color="text.secondary">
              جستجو با Ctrl+K از همان فرم و نتایج جستجوی پیشرفته سامانه استفاده می‌کند.
            </Typography>
          </Stack>
        </Stack>
      </DialogTitle>
      <DialogContent dividers>
        <Stack spacing={1.5}>
          <AdvancedSearchForm />
          <SearchResults />
        </Stack>
      </DialogContent>
    </Dialog>
  );
};
