import { LinearProgress, Stack, Typography } from '@mui/material';

interface PasswordStrengthIndicatorProps {
  password: string;
}

const evaluateStrength = (password: string): { score: number; label: string; color: 'error' | 'warning' | 'success' } => {
  if (!password) {
    return { score: 0, label: 'وارد نشده', color: 'error' };
  }

  let score = 0;
  if (password.length >= 12) score += 25;
  if (/[A-Z]/.test(password)) score += 25;
  if (/[a-z]/.test(password)) score += 20;
  if (/[0-9]/.test(password)) score += 15;
  if (/[^a-zA-Z0-9]/.test(password)) score += 15;

  if (score < 50) return { score, label: 'ضعیف', color: 'error' };
  if (score < 80) return { score, label: 'متوسط', color: 'warning' };
  return { score, label: 'قوی', color: 'success' };
};

export const PasswordStrengthIndicator = ({ password }: PasswordStrengthIndicatorProps) => {
  const state = evaluateStrength(password);

  return (
    <Stack spacing={0.5} aria-live="polite">
      <Typography variant="caption" color="text.secondary">
        قدرت رمز عبور: {state.label}
      </Typography>
      <LinearProgress
        variant="determinate"
        value={state.score}
        color={state.color}
        sx={{ height: 8, borderRadius: 8 }}
        aria-label="قدرت رمز عبور"
      />
    </Stack>
  );
};
