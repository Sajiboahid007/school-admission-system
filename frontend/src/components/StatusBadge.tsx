import type { HoldStatus } from '../api/types';

const LABELS: Record<HoldStatus, string> = {
  HELD: 'Seat hold করা আছে',
  CONFIRMED: 'ভর্তি নিশ্চিত',
  EXPIRED: 'মেয়াদ শেষ',
  CANCELLED: 'বাতিল',
};

export default function StatusBadge({ status }: { status: HoldStatus }) {
  return <span className={`badge badge-${status}`}>{status} ({LABELS[status]})</span>;
}
