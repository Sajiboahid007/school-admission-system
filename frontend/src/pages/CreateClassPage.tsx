import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { createClass } from '../api/classes';
import { friendlyMessage } from '../api/errors';

export default function CreateClassPage() {
  const [name, setName] = useState('');
  const [seats, setSeats] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  function validate(): string | null {
    if (name.trim() === '') return 'Class-এর নাম দিন।';
    if (name.trim().length > 100) return 'নাম ১০০ অক্ষরের মধ্যে রাখুন।';
    const n = Number(seats);
    if (!Number.isInteger(n) || n < 1 || n > 10000) return 'Seat সংখ্যা ১ থেকে ১০০০০ এর মধ্যে একটি পূর্ণ সংখ্যা দিন।';
    return null;
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    const problem = validate();
    if (problem) {
      setError(problem);
      return;
    }

    setSaving(true);
    try {
      const created = await createClass(name.trim(), Number(seats));
      setSuccess(`"${created.name}" তৈরি হয়েছে (${created.totalSeats}টি seat)।`);
      setName('');
      setSeats('');
    } catch (err) {
      setError(friendlyMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="card">
      <h2>নতুন Class তৈরি (School admin)</h2>
      <p className="hint">প্রতিটি shift আলাদা class, যেমন "Class 6 - Morning Shift"।</p>
      {error && <div className="error">{error}</div>}
      {success && <div className="success">{success} <Link to="/">Class তালিকা দেখুন</Link></div>}
      <form onSubmit={onSubmit}>
        <p><label>নাম<br /><input value={name} onChange={(e) => setName(e.target.value)} maxLength={100} /></label></p>
        <p><label>Seat সংখ্যা<br /><input value={seats} onChange={(e) => setSeats(e.target.value)} inputMode="numeric" /></label></p>
        <button type="submit" disabled={saving}>{saving ? 'তৈরি হচ্ছে…' : 'তৈরি করুন'}</button>
      </form>
    </div>
  );
}
