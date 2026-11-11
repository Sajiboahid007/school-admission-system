import { request } from './client';
import type { Hold, SchoolClass } from './types';

export const getClasses = () => request<SchoolClass[]>('/classes');

export const createClass = (name: string, totalSeats: number) =>
  request<SchoolClass>('/classes', { method: 'POST', body: { name, totalSeats } });

export const createHold = (classId: string, studentId: string) =>
  request<Hold>(`/classes/${encodeURIComponent(classId)}/holds`, { method: 'POST', studentId });
