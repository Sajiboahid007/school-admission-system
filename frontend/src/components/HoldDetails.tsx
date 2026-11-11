import type { Hold, HoldStatus } from '../api/types';
import { formatMMSS, useCountdown } from '../hooks/useCountdown';
import StatusBadge from './StatusBadge';

type Props = {
  hold: Hold;
  offsetMs?: number;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
};

export default function HoldDetails({ hold, offsetMs = 0, busy = false, onConfirm, onCancel }: Props) {
  const { secondsLeft, isOver } = useCountdown(hold.expiresAt, offsetMs);

  // Don't wait for the next poll: if the clock says time is over, show EXPIRED now.
  const status: HoldStatus = hold.status === 'HELD' && isOver ? 'EXPIRED' : hold.status;
  const canAct = status === 'HELD' && !busy;

  return (
    <div className="card">
      <h2>{hold.className || 'আবেদন'}</h2>
      <p>অবস্থা: <StatusBadge status={status} /></p>

      {status === 'HELD' && (
        <p>মেয়াদ শেষ হতে বাকি: <strong data-testid="countdown">{formatMMSS(secondsLeft)}</strong></p>
      )}
      {status === 'EXPIRED' && <p className="warn">সময় শেষ, seat ছেড়ে দেওয়া হয়েছে। চাইলে আবার আবেদন করুন।</p>}
      {status === 'CONFIRMED' && <p className="success">ভর্তি নিশ্চিত হয়েছে।</p>}
      {status === 'CANCELLED' && <p>এই আবেদনটি বাতিল করা হয়েছে।</p>}

      <button onClick={onConfirm} disabled={!canAct}>ভর্তি ফি দিন</button>{' '}
      <button className="secondary" onClick={onCancel} disabled={!canAct}>বাতিল</button>
    </div>
  );
}
