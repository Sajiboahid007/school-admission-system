import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import { StudentProvider } from '../context/StudentContext';
import StudentIdBar, { normalizeDigits } from './StudentIdBar';

describe('StudentIdBar and normalizeDigits', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('normalizeDigits', () => {
    it('converts Bangla digits and removes spaces or hyphens', () => {
      expect(normalizeDigits('২০১৫ 269-1')).toBe('20152691');
      expect(normalizeDigits('০১২৩৪৫৬৭৮৯')).toBe('0123456789');
    });

    it('strips non-digit characters', () => {
      expect(normalizeDigits('abc-xyz !@#$%^&*()_+')).toBe('');
      expect(normalizeDigits('ID: 2026-BD')).toBe('2026');
    });

    it('slices input to a maximum of 17 digits', () => {
      const twentyDigits = '12345678901234567890';
      const result = normalizeDigits(twentyDigits);
      expect(result).toHaveLength(17);
      expect(result).toBe('12345678901234567');
    });
  });

  describe('StudentIdBar component', () => {
    it('renders input with label and placeholder', () => {
      render(
        <StudentProvider>
          <StudentIdBar />
        </StudentProvider>,
      );

      const input = screen.getByPlaceholderText('১৭ অঙ্ক');
      expect(input).toBeInTheDocument();
      expect(screen.getByLabelText(/শিক্ষার্থীর জন্ম নিবন্ধন নম্বর/)).toBeInTheDocument();
    });

    it('normalizes typed input and saves 17-digit id to localStorage', async () => {
      const user = userEvent.setup();
      render(
        <StudentProvider>
          <StudentIdBar />
        </StudentProvider>,
      );

      const input = screen.getByPlaceholderText('১৭ অঙ্ক') as HTMLInputElement;

      // Type partial Bengali digits
      await user.type(input, '২০১৫');
      expect(input.value).toBe('2015');
      expect(screen.getByText('4/17 অঙ্ক দেওয়া হয়েছে')).toBeInTheDocument();

      // Clear and type full 17 digits
      await user.clear(input);
      await user.type(input, '12345678901234567');
      expect(input.value).toBe('12345678901234567');
      expect(localStorage.getItem('admission.studentId')).toBe('12345678901234567');
      expect(screen.queryByText(/অঙ্ক দেওয়া হয়েছে/)).not.toBeInTheDocument();
    });
  });
});
