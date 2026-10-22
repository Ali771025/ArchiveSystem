import clsx from 'clsx';
import { ChangeEvent, ClipboardEvent, KeyboardEvent, useEffect, useRef, useState } from 'react';
import { Box } from '@mui/material';

interface OtpInputProps {
  length?: number;
  value: string;
  disabled?: boolean;
  autoFocus?: boolean;
  onChange: (value: string) => void;
  onComplete?: (value: string) => void;
  containerClassName?: string;
  inputClassName?: string;
}

const toDigits = (value: string, length: number): string[] => {
  const normalized = value.replace(/\D/g, '').slice(0, length);
  return Array.from({ length }, (_, index) => normalized[index] ?? '');
};

export const OtpInput = ({
  length = 5,
  value,
  disabled = false,
  autoFocus = false,
  onChange,
  onComplete,
  containerClassName,
  inputClassName,
}: OtpInputProps) => {
  const refs = useRef<Array<HTMLInputElement | null>>([]);
  const [digits, setDigits] = useState<string[]>(() => toDigits(value, length));

  useEffect(() => {
    const normalized = toDigits(value, length);
    const hasExternalChange = normalized.join('') !== digits.join('');
    if (hasExternalChange) {
      setDigits(normalized);
    }
  }, [digits, length, value]);

  const focusIndex = (index: number) => {
    const bounded = Math.max(0, Math.min(length - 1, index));
    refs.current[bounded]?.focus();
  };

  const emitChange = (nextDigits: string[]) => {
    setDigits(nextDigits);
    const joined = nextDigits.join('');
    onChange(joined);

    if (joined.length === length && nextDigits.every((digit) => digit !== '')) {
      onComplete?.(joined);
    }
  };

  const handleChange = (index: number, raw: string) => {
    const clean = raw.replace(/\D/g, '');

    if (!clean) {
      const next = [...digits];
      next[index] = '';
      emitChange(next);
      return;
    }

    const next = [...digits];
    let cursor = index;
    for (const char of clean) {
      if (cursor >= length) {
        break;
      }

      next[cursor] = char;
      cursor += 1;
    }

    emitChange(next);
    focusIndex(Math.min(cursor, length - 1));
  };

  const handleKeyDown = (index: number, event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      focusIndex(index - 1);
      return;
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault();
      focusIndex(index + 1);
      return;
    }

    if (event.key !== 'Backspace') {
      return;
    }

    event.preventDefault();

    const next = [...digits];
    if (next[index]) {
      next[index] = '';
      emitChange(next);
      return;
    }

    const previousIndex = Math.max(0, index - 1);
    next[previousIndex] = '';
    emitChange(next);
    focusIndex(previousIndex);
  };

  const handlePaste = (event: ClipboardEvent<HTMLDivElement>) => {
    event.preventDefault();
    const pasted = event.clipboardData.getData('text').replace(/\D/g, '').slice(0, length);
    if (!pasted) {
      return;
    }

    const next = Array.from({ length }, (_, index) => pasted[index] ?? '');
    emitChange(next);
    focusIndex(Math.min(pasted.length, length - 1));
  };

  return (
    <Box
      dir="ltr"
      className={clsx(containerClassName)}
      onPaste={handlePaste}
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: { xs: 0.75, sm: 1 },
        width: '100%',
      }}
    >
      {digits.map((digit, index) => (
        <Box
          key={index}
          component="input"
          ref={(element: HTMLInputElement | null) => {
            refs.current[index] = element;
          }}
          value={digit}
          onChange={(event: ChangeEvent<HTMLInputElement>) => handleChange(index, event.target.value)}
          onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => handleKeyDown(index, event)}
          inputMode="numeric"
          pattern="[0-9]*"
          maxLength={length}
          disabled={disabled}
          autoFocus={autoFocus && index === 0}
          aria-label={`otp-digit-${index + 1}`}
          className={clsx(inputClassName)}
          sx={{
            height: { xs: 44, sm: 46 },
            width: { xs: 42, sm: 44 },
            borderRadius: 1.5,
            border: '1px solid',
            borderColor: 'divider',
            textAlign: 'center',
            fontSize: 22,
            fontWeight: 700,
            color: 'text.primary',
            outline: 'none',
            backgroundColor: 'background.paper',
            transition: 'all 160ms ease',
            '&:focus': {
              borderColor: 'primary.main',
              boxShadow: '0 0 0 3px rgba(25,118,210,0.16)',
            },
            '&:disabled': {
              backgroundColor: 'action.disabledBackground',
              color: 'text.disabled',
              cursor: 'not-allowed',
            },
            flexShrink: 0,
          }}
        />
      ))}
    </Box>
  );
};
