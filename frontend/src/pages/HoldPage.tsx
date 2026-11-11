import { useCallback, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ApiError } from '../api/client';
import { friendlyMessage } from '../api/errors';
import { cancelHold, confirmHold, getHold } from '../api/holds';
import type { Hold } from '../api/types';
import HoldDetails from '../components/HoldDetails';
import { useStudent } from '../context/StudentContext';
import { usePolling } from '../hooks/usePolling';

export default function HoldPage() {
  const { holdId = '' } = useParams();
  const { studentId } = useStudent();

  const [hold, setHold] = useState<Hold | null>(null);
  const [offsetMs, setOffsetMs] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const h = await getHold(holdId);
      setHold(h);
      if (h.serverTime) setOffsetMs(new Date(h.serverTime).getTime() - Date.now());
      setError(null);
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) setNotFound(true);
      else setError(friendlyMessage(err));
    }
  }, [holdId]);

  // Keep polling only while something can still change.
  const stillActive = !notFound && (hold === null || hold.status === 'HELD');
  usePolling(load, 5000, stillActive);

  async function run(action: () => Promise<unknown>) {
    setBusy(true);
    setError(null);
    try {
      await action();
    } catch (err) {
      setError(friendlyMessage(err));
    } finally {
      await load();
      setBusy(false);
    }
  }

  const onConfirm = () => run(() => confirmHold(holdId, studentId));
  const onCancel = () => {
    if (window.confirm('আপনি কি আবেদনটি বাতিল করতে চান?')) void run(() => cancelHold(holdId, studentId));
  };

  if (notFound) return <div className="error">আবেদন পাওয়া যায়নি। <Link to="/my-holds">আমার আবেদন</Link></div>;

  return (
    <div>
      {error && <div className="error">{error}</div>}
      {!hold && !error && <p>লোড হচ্ছে…</p>}
      {hold && <HoldDetails hold={hold} offsetMs={offsetMs} busy={busy} onConfirm={onConfirm} onCancel={onCancel} />}
      <p className="hint">"ভর্তি ফি দিন" চাপলে payment সফল ধরা হবে (demo, কোনো আসল payment নেই)।</p>
    </div>
  );
}
