import { useEffect, useState } from 'react';

// offsetMs = serverTime - clientTime (phase 7). 0 for now.
export function useCountdown(expiresAt: string | undefined, offsetMs = 0) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  if (!expiresAt) return { secondsLeft: 0, isOver: true };

  const msLeft = new Date(expiresAt).getTime() - (now + offsetMs);
  const secondsLeft = Math.max(0, Math.ceil(msLeft / 1000));
  return { secondsLeft, isOver: secondsLeft === 0 };
}

export function formatMMSS(totalSeconds: number) {
  const m = Math.floor(totalSeconds / 60);
  const s = totalSeconds % 60;
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}
