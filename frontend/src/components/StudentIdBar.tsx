import { useStudent } from '../context/StudentContext';

const BN_DIGITS = '০১২৩৪৫৬৭৮৯';

// Parents may type Bangla digits; convert them, drop everything else.
export function normalizeDigits(value: string) {
  return value
    .replace(/[০-৯]/g, (d) => String(BN_DIGITS.indexOf(d)))
    .replace(/[^0-9]/g, '')
    .slice(0, 17);
}

export default function StudentIdBar() {
  const { studentId, isValid, setStudentId } = useStudent();

  return (
    <div>
      <label>
        শিক্ষার্থীর জন্ম নিবন্ধন নম্বর:{' '}
        <input
          value={studentId}
          inputMode="numeric"
          placeholder="১৭ অঙ্ক"
          onChange={(e) => setStudentId(normalizeDigits(e.target.value))}
          aria-invalid={studentId !== '' && !isValid}
        />
      </label>
      {studentId !== '' && !isValid && (
        <div className="hint">{studentId.length}/17 অঙ্ক দেওয়া হয়েছে</div>
      )}
    </div>
  );
}
