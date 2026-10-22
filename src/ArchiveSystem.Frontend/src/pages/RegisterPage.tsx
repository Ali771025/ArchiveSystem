import PersonAddAlt1Icon from '@mui/icons-material/PersonAddAlt1';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { FormEvent, useMemo, useState } from 'react';
import { Link as RouterLink, Navigate, useNavigate } from 'react-router-dom';
import { authApi } from '@/services/authApi';
import { useSessionStore } from '@/stores/useSessionStore';
import { useToastStore } from '@/stores/useToastStore';

export const RegisterPage = () => {
  const navigate = useNavigate();
  const isAuthenticated = useSessionStore((state) => state.isAuthenticated);
  const setSession = useSessionStore((state) => state.setSession);
  const pushToast = useToastStore((state) => state.push);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const passwordMismatch = useMemo(() => {
    if (!confirmPassword) {
      return false;
    }

    return password !== confirmPassword;
  }, [confirmPassword, password]);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');

    if (password !== confirmPassword) {
      setError('گذرواژه و تکرار گذرواژه یکسان نیست.');
      return;
    }

    setLoading(true);
    try {
      const result = await authApi.register({ username, password, confirmPassword });
      setSession(result.token, result.user);
      pushToast('ثبت‌نام با موفقیت انجام شد.', 'success');
      navigate('/');
    } catch {
      setError('ثبت‌نام ناموفق بود. لطفا اطلاعات را بررسی کنید.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        background: 'radial-gradient(circle at 20% 20%, #d4f6f1 0%, #e9f7f5 45%, #f5faf9 100%)',
        p: { xs: 1.5, sm: 2.5 },
      }}
    >
      <Card sx={{ width: 'min(100%, 28rem)', borderRadius: { xs: 2, sm: 3 } }}>
        <CardHeader
          avatar={<PersonAddAlt1Icon color="primary" />}
          title="ایجاد حساب کاربری"
          subheader="نام کاربری و گذرواژه خود را برای ثبت‌نام وارد کنید"
        />
        <CardContent>
          <Stack component="form" spacing={1.5} onSubmit={submit}>
            <TextField
              label="نام کاربری"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              required
              autoFocus
              inputProps={{ minLength: 3 }}
            />
            <TextField
              label="گذرواژه"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
              inputProps={{ minLength: 8 }}
            />
            <TextField
              label="تکرار گذرواژه"
              type="password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              required
              error={passwordMismatch}
              helperText={passwordMismatch ? 'گذرواژه‌ها باید یکسان باشند.' : ' '}
              inputProps={{ minLength: 8 }}
            />
            {error && <Alert severity="error">{error}</Alert>}
            <Button type="submit" variant="contained" disabled={loading || passwordMismatch}>
              {loading ? <CircularProgress size={20} color="inherit" /> : 'ثبت‌نام'}
            </Button>
            <Typography variant="body2" color="text.secondary">
              حساب کاربری دارید؟{' '}
              <Link component={RouterLink} to="/login" underline="hover">
                ورود
              </Link>
            </Typography>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
};
