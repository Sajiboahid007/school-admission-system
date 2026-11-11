import { useEffect, useRef } from 'react';

// Calls fn now, then every intervalMs. Always uses the latest fn.
export function usePolling(fn: () => void | Promise<void>, intervalMs: number, enabled = true) {
  const saved = useRef(fn);

  useEffect(() => {
    saved.current = fn;
  }, [fn]);

  useEffect(() => {
    if (!enabled) return;
    let stopped = false;

    const tick = () => {
      if (!stopped) void saved.current();
    };

    tick();
    const id = setInterval(tick, intervalMs);
    return () => {
      stopped = true;
      clearInterval(id);
    };
  }, [intervalMs, enabled]);
}
