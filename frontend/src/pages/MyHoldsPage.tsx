import { useCallback, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { friendlyMessage } from '../api/errors';
import { getMyHolds } from '../api/holds';
import type { Hold, HoldStatus } from '../api/types';
import StatusBadge from '../components/StatusBadge';
import { useStudent } from '../context/StudentContext';
import { usePolling } from '../hooks/usePolling';

export default function MyHoldsPage() {
  const { studentId, isValid } = useStudent();
  const [holds, setHolds] = useState<Hold[] | null>(null);
  const [offsetMs, setOffsetMs] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const holdsRef = useRef<Hold[] | null>(null);

  const load = useCallback(async () => {
    if (!isValid) return;
    try {
      const data = await getMyHolds(studentId);
      setHolds(data);
      holdsRef.current = data;
      const withServerTime = data.find((h) => h.serverTime);
      if (withServerTime?.serverTime) {
        setOffsetMs(new Date(withServerTime.serverTime).getTime() - Date.now());
      }
      setError(null);
    } catch (err) {
      if (holdsRef.current !== null) {
        setError('সংযোগ বিচ্ছিন্ন, শেষ তথ্য দেখানো হচ্ছে। আবার সংযোগের চেষ্টা চলছে…');
      } else {
        setError(friendlyMessage(err));
      }
    }
  }, [studentId, isValid]);

  usePolling(load, 5000, isValid);

  const prevStudentId = useRef(studentId);
  useEffect(() => {
    if (prevStudentId.current !== studentId) {
      prevStudentId.current = studentId;
      holdsRef.current = null;
      if (isValid) {
        void load();
      } else {
        setHolds(null);
      }
    }
  }, [studentId, isValid, load]);

  return (
    <div>
      <h2>আমার আবেদন</h2>
      {!isValid && <div className="hint">আগে জন্ম নিবন্ধন নম্বর দিন।</div>}
      {error && <div className="error">{error}</div>}

      {isValid && holds === null && !error && <p>লোড হচ্ছে…</p>}
      {isValid && holds?.length === 0 && <p>কোনো আবেদন নেই।</p>}

      {isValid && holds && holds.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th>অবস্থা</th>
                <th>মেয়াদ</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {holds.map((h) => {
                const isOver = !h.expiresAt || new Date(h.expiresAt).getTime() <= (Date.now() + offsetMs);
                const status: HoldStatus = h.status === 'HELD' && isOver ? 'EXPIRED' : h.status;
                const expiry = h.expiresAt
                  ? new Date(h.expiresAt).toLocaleTimeString('bn-BD')
                  : '—';

                return (
                  <tr key={h.holdId}>
                    <td>{h.className || 'Class'}</td>
                    <td><StatusBadge status={status} /></td>
                    <td>{expiry}</td>
                    <td>
                      <Link to={`/holds/${h.holdId}`}>দেখুন</Link>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
