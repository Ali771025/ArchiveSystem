import { Button, Paper, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

export const NotFoundPage = () => {
  return (
    <Paper variant="outlined" sx={{ p: { xs: 2.5, sm: 3.5, md: 4 } }}>
      <Stack spacing={1} alignItems="center">
        <Typography variant="h5">صفحه موردنظر پیدا نشد</Typography>
        <Button component={Link} to="/" variant="contained">
          بازگشت به داشبورد
        </Button>
      </Stack>
    </Paper>
  );
};

