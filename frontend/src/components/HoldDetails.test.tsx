import { act, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import HoldDetails from './HoldDetails';

describe('HoldDetails', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('switches to EXPIRED and disables the pay button when time is over', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-10T10:00:00Z'));

    render(
      <HoldDetails
        hold={{
          holdId: 'h1',
          classId: 'c1',
          className: 'Class 6 - Morning Shift',
          status: 'HELD',
          expiresAt: '2026-11-10T10:00:03Z',
        }}
        onConfirm={() => {}}
        onCancel={() => {}}
      />,
    );

    const payButton = screen.getByRole('button', { name: 'ভর্তি ফি দিন' });
    expect(payButton).toBeEnabled();
    expect(screen.getByTestId('countdown')).toHaveTextContent('00:03');

    act(() => {
      vi.advanceTimersByTime(4000);
    });

    expect(screen.getByText(/EXPIRED/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'ভর্তি ফি দিন' })).toBeDisabled();
  });

  it('does not allow paying for a cancelled application', () => {
    render(
      <HoldDetails
        hold={{
          holdId: 'h1',
          classId: 'c1',
          className: 'X',
          status: 'CANCELLED',
          expiresAt: '2099-01-01T00:00:00Z',
        }}
        onConfirm={() => {}}
        onCancel={() => {}}
      />,
    );
    expect(screen.getByRole('button', { name: 'ভর্তি ফি দিন' })).toBeDisabled();
  });

  it('accounts for offsetMs when calculating remaining time', () => {
    vi.useFakeTimers();
    // Local clock is 10:00:00, but server clock is 10:00:30 (offsetMs = +30000)
    vi.setSystemTime(new Date('2026-11-10T10:00:00Z'));

    render(
      <HoldDetails
        hold={{
          holdId: 'h1',
          classId: 'c1',
          className: 'Class 6',
          status: 'HELD',
          expiresAt: '2026-11-10T10:01:00Z',
        }}
        offsetMs={30000}
        onConfirm={() => {}}
        onCancel={() => {}}
      />,
    );

    // Remaining time is 30 seconds, not 60 seconds
    expect(screen.getByTestId('countdown')).toHaveTextContent('00:30');
  });
});
