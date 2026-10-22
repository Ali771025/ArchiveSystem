import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  Stack,
  Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';

interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmText: string;
  cancelText: string;
  variant?: 'danger' | 'primary';
  onConfirm: () => void;
  onCancel: () => void;
}

export const ConfirmDialog = ({
  isOpen,
  title,
  message,
  confirmText,
  cancelText,
  variant = 'primary',
  onConfirm,
  onCancel,
}: ConfirmDialogProps) => {
  const isDanger = variant === 'danger';
  const titleId = 'confirm-dialog-title';
  const descriptionId = 'confirm-dialog-description';

  return (
    <Dialog
      open={isOpen}
      onClose={onCancel}
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      fullWidth
      maxWidth="xs"
      dir="rtl"
      PaperProps={{
        sx: {
          borderRadius: 2,
          overflow: 'hidden',
        },
      }}
    >
      <DialogTitle id={titleId} sx={{ p: 2, pb: 1 }}>
        <Stack direction="row" alignItems="center" spacing={1.25}>
          <Stack
            alignItems="center"
            justifyContent="center"
            sx={{
              width: 40,
              height: 40,
              borderRadius: 1.5,
              bgcolor: isDanger ? 'error.light' : 'primary.light',
              color: isDanger ? 'error.contrastText' : 'primary.contrastText',
              flexShrink: 0,
            }}
          >
            {isDanger ? <DeleteOutlineIcon fontSize="small" /> : <HelpOutlineIcon fontSize="small" />}
          </Stack>
          <Typography component="span" variant="h6" fontWeight={700} sx={{ flex: 1 }}>
            {title}
          </Typography>
          <IconButton aria-label="بستن" onClick={onCancel} edge="end" size="small">
            <CloseIcon fontSize="small" />
          </IconButton>
        </Stack>
      </DialogTitle>

      <DialogContent sx={{ px: 2, pb: 1.5 }}>
        <DialogContentText id={descriptionId} sx={{ color: 'text.secondary', lineHeight: 1.9 }}>
          {message}
        </DialogContentText>
      </DialogContent>

      <DialogActions sx={{ px: 2, pb: 2, gap: 1, justifyContent: 'flex-start' }}>
        <Button variant="outlined" color="inherit" onClick={onCancel}>
          {cancelText}
        </Button>
        <Button variant="contained" color={isDanger ? 'error' : 'primary'} onClick={onConfirm} autoFocus>
          {confirmText}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
