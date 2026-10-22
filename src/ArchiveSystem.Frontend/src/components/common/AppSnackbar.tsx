import Alert from '@mui/material/Alert';
import Snackbar from '@mui/material/Snackbar';
import { useTheme } from '@mui/material/styles';
import { useToastStore } from '@/stores/useToastStore';

export const AppSnackbar = () => {
  const theme = useTheme();
  const queue = useToastStore((state) => state.queue);
  const shift = useToastStore((state) => state.shift);

  const active = queue[0];

  return (
    <Snackbar
      open={Boolean(active)}
      autoHideDuration={3200}
      anchorOrigin={{ vertical: 'top', horizontal: theme.direction === 'rtl' ? 'right' : 'left' }}
      onClose={shift}
    >
      <Alert severity={active?.severity ?? 'info'} onClose={shift} variant="filled" sx={{ width: '100%' }}>
        {active?.message}
      </Alert>
    </Snackbar>
  );
};
