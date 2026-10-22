import AddLinkIcon from '@mui/icons-material/AddLink';
import CloseIcon from '@mui/icons-material/Close';
import {
  Autocomplete,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { useTaxpayerSearch, type TaxpayerSearchItem } from '@/features/taxpayer-management/hooks/useTaxpayerSearch';
import { ownersApi } from '@/services/ownersApi';
import { useToastStore } from '@/stores/useToastStore';
import { OWNER_TYPE_LABELS } from '@/types/owners';

interface AttachOwnerModalProps {
  isOpen: boolean;
  onClose: () => void;
  dossierId: number;
  caseNumber?: string | null;
  onSuccess: () => void;
}

export const AttachOwnerModal = ({
  isOpen,
  onClose,
  dossierId,
  caseNumber,
  onSuccess,
}: AttachOwnerModalProps) => {
  const [inputValue, setInputValue] = useState('');
  const [selectedTaxpayer, setSelectedTaxpayer] = useState<TaxpayerSearchItem | null>(null);

  const pushToast = useToastStore((state) => state.push);

  const taxpayerSearch = useTaxpayerSearch(inputValue, {
    debounceMs: 450,
    minLength: 2,
    take: 25,
    enabled: isOpen,
  });
  const searchOptions = taxpayerSearch.shouldSearch ? taxpayerSearch.results : [];

  const attachMutation = useMutation({
    mutationFn: async () => {
      if (!selectedTaxpayer) {
        throw new Error('No taxpayer selected.');
      }

      if (caseNumber?.trim()) {
        await ownersApi.attachTaxpayerToCaseNumber(caseNumber.trim(), selectedTaxpayer.taxpayerId);
        return;
      }

      await ownersApi.attachTaxpayerToDossier(dossierId, selectedTaxpayer.taxpayerId);
    },
    onSuccess: () => {
      pushToast('مودی با موفقیت به پرونده متصل شد.', 'success');
      onSuccess();
      onClose();
    },
    onError: () => {
      pushToast('اتصال مودی به پرونده ناموفق بود.', 'error');
    },
  });

  useEffect(() => {
    if (!isOpen) {
      setInputValue('');
      setSelectedTaxpayer(null);
    }
  }, [isOpen]);

  return (
    <Dialog
      open={isOpen}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      PaperProps={{
        sx: {
          borderRadius: 3,
        },
      }}
    >
      <DialogTitle sx={{ pb: 1.5 }}>اتصال مودی به پرونده</DialogTitle>
      <DialogContent sx={{ pt: 1 }}>
        <Stack spacing={2}>
          <Typography variant="body2" color="text.secondary">
            نام، کد ملی یا شناسه ملی مودی را جستجو کنید و یک مورد را برای اتصال انتخاب کنید.
          </Typography>

          <Autocomplete<TaxpayerSearchItem, false, false, false>
            options={searchOptions}
            filterOptions={(options) => options}
            value={selectedTaxpayer}
            inputValue={inputValue}
            loading={taxpayerSearch.isFetching}
            isOptionEqualToValue={(option, value) => option.taxpayerId === value.taxpayerId}
            getOptionLabel={(option) => option.label}
            noOptionsText={taxpayerSearch.shouldSearch ? 'نتیجه‌ای یافت نشد.' : 'حداقل 2 کاراکتر وارد کنید.'}
            onInputChange={(_event, value, reason) => {
              if (reason === 'reset') {
                setInputValue(value);
                return;
              }

              setInputValue(value);
            }}
            onChange={(_event, value) => {
              setSelectedTaxpayer(value);
            }}
            renderOption={(props, option) => {
              const { key, ...restProps } = props;
              return (
                <Box component="li" key={key} {...restProps}>
                  <Stack sx={{ width: '100%' }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {option.label}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {OWNER_TYPE_LABELS[option.ownerType]}
                      {option.nationalIdentifier ? ` - ${option.nationalIdentifier}` : ''}
                    </Typography>
                  </Stack>
                </Box>
              );
            }}
            renderInput={(params) => (
              <TextField
                {...params}
                label="جستجوی مودی"
                placeholder="مثال: علی رضایی یا 0012345678"
                helperText={!taxpayerSearch.shouldSearch && inputValue.trim().length > 0 ? 'حداقل 2 کاراکتر وارد کنید.' : ' '}
                InputProps={{
                  ...params.InputProps,
                  endAdornment: (
                    <>
                      {taxpayerSearch.isFetching ? <CircularProgress color="inherit" size={18} /> : null}
                      {params.InputProps.endAdornment}
                    </>
                  ),
                }}
              />
            )}
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button startIcon={<CloseIcon />} onClick={onClose} disabled={attachMutation.isPending}>
          انصراف
        </Button>
        <Button
          variant="contained"
          startIcon={attachMutation.isPending ? <CircularProgress size={16} color="inherit" /> : <AddLinkIcon />}
          onClick={() => attachMutation.mutate()}
          disabled={!selectedTaxpayer || attachMutation.isPending}
        >
          اتصال
        </Button>
      </DialogActions>
    </Dialog>
  );
};
