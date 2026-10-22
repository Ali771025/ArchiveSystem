import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

interface UseResilientTimerOptions {
  storageKey: string;
  initialDurationSeconds: number;
  autoStart?: boolean;
  onExpire?: () => void;
}

interface ResilientTimerResult {
  remainingSeconds: number;
  isRunning: boolean;
  start: (durationSeconds?: number) => void;
  stop: () => void;
  reset: (durationSeconds?: number) => void;
  formatted: string;
}

const now = () => Date.now();

const getStoredEndTime = (storageKey: string): number | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  const raw = window.localStorage.getItem(storageKey);
  if (!raw) {
    return null;
  }

  const parsed = Number(raw);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    window.localStorage.removeItem(storageKey);
    return null;
  }

  return parsed;
};

const setStoredEndTime = (storageKey: string, endTimeEpochMs: number | null): void => {
  if (typeof window === 'undefined') {
    return;
  }

  if (endTimeEpochMs === null) {
    window.localStorage.removeItem(storageKey);
    return;
  }

  window.localStorage.setItem(storageKey, String(endTimeEpochMs));
};

const computeRemainingSeconds = (endTimeEpochMs: number | null): number => {
  if (!endTimeEpochMs) {
    return 0;
  }

  const deltaMs = endTimeEpochMs - now();
  if (deltaMs <= 0) {
    return 0;
  }

  return Math.ceil(deltaMs / 1000);
};

export const useResilientTimer = ({
  storageKey,
  initialDurationSeconds,
  autoStart = false,
  onExpire,
}: UseResilientTimerOptions): ResilientTimerResult => {
  const [endTimeEpochMs, setEndTimeEpochMs] = useState<number | null>(() => getStoredEndTime(storageKey));
  const [remainingSeconds, setRemainingSeconds] = useState<number>(() =>
    computeRemainingSeconds(getStoredEndTime(storageKey)),
  );

  const expireHandledRef = useRef(false);

  const tick = useCallback(() => {
    const currentRemaining = computeRemainingSeconds(endTimeEpochMs);
    setRemainingSeconds(currentRemaining);

    if (currentRemaining === 0 && endTimeEpochMs !== null) {
      if (!expireHandledRef.current) {
        expireHandledRef.current = true;
        onExpire?.();
      }

      setEndTimeEpochMs(null);
      setStoredEndTime(storageKey, null);
    }
  }, [endTimeEpochMs, onExpire, storageKey]);

  useEffect(() => {
    tick();
  }, [tick]);

  useEffect(() => {
    if (!endTimeEpochMs) {
      return;
    }

    const timerId = window.setInterval(tick, 250);

    const onVisibilityChange = () => {
      tick();
    };

    document.addEventListener('visibilitychange', onVisibilityChange);

    return () => {
      window.clearInterval(timerId);
      document.removeEventListener('visibilitychange', onVisibilityChange);
    };
  }, [endTimeEpochMs, tick]);

  const start = useCallback(
    (durationSeconds = initialDurationSeconds) => {
      const duration = Math.max(1, durationSeconds);
      const nextEndTime = now() + duration * 1000;
      expireHandledRef.current = false;
      setEndTimeEpochMs(nextEndTime);
      setRemainingSeconds(duration);
      setStoredEndTime(storageKey, nextEndTime);
    },
    [initialDurationSeconds, storageKey],
  );

  const stop = useCallback(() => {
    setEndTimeEpochMs(null);
    setRemainingSeconds(0);
    setStoredEndTime(storageKey, null);
  }, [storageKey]);

  const reset = useCallback(
    (durationSeconds = initialDurationSeconds) => {
      start(durationSeconds);
    },
    [initialDurationSeconds, start],
  );

  useEffect(() => {
    if (!autoStart || endTimeEpochMs) {
      return;
    }

    start(initialDurationSeconds);
  }, [autoStart, endTimeEpochMs, initialDurationSeconds, start]);

  const formatted = useMemo(() => {
    const mm = String(Math.floor(remainingSeconds / 60)).padStart(2, '0');
    const ss = String(remainingSeconds % 60).padStart(2, '0');
    return `${mm}:${ss}`;
  }, [remainingSeconds]);

  return {
    remainingSeconds,
    isRunning: remainingSeconds > 0,
    start,
    stop,
    reset,
    formatted,
  };
};
