import { ApiError } from './client';

const MESSAGES: Record<string, string> = {
  NETWORK_ERROR: 'Server-এর সাথে যোগাযোগ করা যাচ্ছে না। ইন্টারনেট বা server বন্ধ কিনা দেখুন, একটু পরে আবার চেষ্টা করুন।',
  NO_SEATS: 'দুঃখিত, এই class-এ এখন কোনো seat খালি নেই।',
  ALREADY_APPLIED: 'এই শিক্ষার্থী এই class-এ আগেই আবেদন করেছে। "আমার আবেদন" পাতায় দেখুন।',
  HOLD_EXPIRED: 'আবেদনের মেয়াদ শেষ হয়ে গেছে। চাইলে আবার আবেদন করুন।',
  HOLD_CANCELLED: 'এই আবেদনটি বাতিল করা হয়েছে।',
  ALREADY_CONFIRMED: 'ভর্তি ইতিমধ্যে নিশ্চিত হয়েছে, এখন আর বাতিল করা যাবে না।',
  NOT_YOUR_HOLD: 'এই আবেদনটি অন্য শিক্ষার্থীর। উপরে সঠিক জন্ম নিবন্ধন নম্বর দিন।',
  STUDENT_ID_REQUIRED: 'আগে উপরের ঘরে শিক্ষার্থীর ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর দিন।',
  INVALID_STUDENT_ID: 'জন্ম নিবন্ধন নম্বর ঠিক ১৭ অঙ্কের হতে হবে।',
  CLASS_NOT_FOUND: 'Class পাওয়া যায়নি।',
  HOLD_NOT_FOUND: 'আবেদন পাওয়া যায়নি।',
  CLASS_EXISTS: 'এই নামে একটি class আগে থেকেই আছে।',
  VALIDATION_ERROR: 'দেওয়া তথ্য সঠিক নয়। আবার দেখে নিন।',
};

export function friendlyMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (MESSAGES[error.code]) return MESSAGES[error.code];
    if (error.status >= 500) return 'Server-এ সমস্যা হয়েছে। একটু পরে আবার চেষ্টা করুন।';
  }
  return 'কিছু একটা ভুল হয়েছে। আবার চেষ্টা করুন।';
}
