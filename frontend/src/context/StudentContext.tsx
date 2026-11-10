import { createContext, useContext, useState, type ReactNode } from 'react';

const STORAGE_KEY = 'admission.studentId';

export const isValidStudentId = (value: string) => /^[0-9]{17}$/.test(value);

type StudentContextValue = {
  studentId: string;
  isValid: boolean;
  setStudentId: (value: string) => void;
};

const StudentContext = createContext<StudentContextValue | null>(null);

function readSaved(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? '';
  } catch {
    return ''; // storage can be blocked (private mode, settings)
  }
}

export function StudentProvider({ children }: { children: ReactNode }) {
  const [studentId, setState] = useState(readSaved);

  const setStudentId = (value: string) => {
    setState(value);
    try {
      if (isValidStudentId(value)) localStorage.setItem(STORAGE_KEY, value);
      else if (value === '') localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
  };

  return (
    <StudentContext.Provider value={{ studentId, isValid: isValidStudentId(studentId), setStudentId }}>
      {children}
    </StudentContext.Provider>
  );
}

export function useStudent() {
  const ctx = useContext(StudentContext);
  if (!ctx) throw new Error('useStudent must be used inside StudentProvider');
  return ctx;
}
