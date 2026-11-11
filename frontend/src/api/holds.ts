import { request } from './client';
import type { Hold } from './types';

const enc = encodeURIComponent;

export const getHold = (holdId: string) => request<Hold>(`/holds/${enc(holdId)}`);

export const confirmHold = (holdId: string, studentId: string) =>
  request<Hold>(`/holds/${enc(holdId)}/confirm`, { method: 'POST', studentId });

export const cancelHold = (holdId: string, studentId: string) =>
  request<void>(`/holds/${enc(holdId)}`, { method: 'DELETE', studentId });

export const getMyHolds = (studentId: string) => request<Hold[]>('/me/holds', { studentId });
