import { useCallback, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createHold, getClasses } from '../api/classes';
import { friendlyMessage } from '../api/errors';
import type { SchoolClass } from '../api/types';
import { useStudent } from '../context/StudentContext';
import { usePolling } from '../hooks/usePolling';

export default function ClassListPage() {
  const { studentId, isValid } = useStudent();
  const navigate = useNavigate();

  const [classes, setClasses] = useState<SchoolClass[] | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [applyingId, setApplyingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setClasses(await getClasses());
      setLoadError(null);
    } catch (err) {
      // keep showing the last data we had, just show a warning on top
      setLoadError(friendlyMessage(err));
    }
  }, []);

  usePolling(load, 5000);

  async function apply(classId: string) {
    setActionError(null);
    setApplyingId(classId);
    try {
      const hold = await createHold(classId, studentId);
      navigate(`/holds/${hold.holdId}`);
    } catch (err) {
      setActionError(friendlyMessage(err));
      void load(); // numbers probably changed
    } finally {
      setApplyingId(null);
    }
  }

  return (
    <div>
      <h2>Class তালিকা</h2>
      {!isValid && <div className="hint">আবেদন করতে আগে উপরে জন্ম নিবন্ধন নম্বর দিন।</div>}
      {loadError && <div className="error">{loadError}</div>}
      {actionError && <div className="error">{actionError}</div>}

      {classes === null && !loadError && <p>লোড হচ্ছে…</p>}
      {classes?.length === 0 && <p>এখনো কোনো class তৈরি হয়নি।</p>}

      {classes && classes.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Class</th><th>মোট</th><th>Hold-এ</th><th>নিশ্চিত</th><th>খালি</th><th></th>
              </tr>
            </thead>
            <tbody>
              {classes.map((c) => (
                <tr key={c.id}>
                  <td>{c.name}</td>
                  <td>{c.totalSeats}</td>
                  <td>{c.heldSeats}</td>
                  <td>{c.confirmedSeats}</td>
                  <td><strong>{c.availableSeats}</strong></td>
                  <td>
                    <button
                      onClick={() => apply(c.id)}
                      disabled={!isValid || c.availableSeats === 0 || applyingId !== null}
                    >
                      {applyingId === c.id ? 'অপেক্ষা করুন…' : 'আবেদন করুন'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
