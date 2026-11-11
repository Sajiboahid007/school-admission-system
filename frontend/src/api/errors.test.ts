import { describe, expect, it } from 'vitest';
import { ApiError } from './client';
import { friendlyMessage } from './errors';

describe('friendlyMessage', () => {
  it('maps NO_SEATS to friendly Bengali seat message', () => {
    const error = new ApiError(409, 'NO_SEATS', 'No seats available');
    const msg = friendlyMessage(error);
    expect(msg).toContain('seat');
  });

  it('maps network error (status 0, code NETWORK_ERROR) to network message', () => {
    const error = new ApiError(0, 'NETWORK_ERROR', 'Network error');
    const msg = friendlyMessage(error);
    expect(msg).toContain('Server-এর সাথে যোগাযোগ করা যাচ্ছে না');
  });

  it('maps 503 error with unknown code to generic server error message', () => {
    const error = new ApiError(503, 'SERVICE_UNAVAILABLE', 'Service unavailable');
    const msg = friendlyMessage(error);
    expect(msg).toContain('Server-এ সমস্যা হয়েছে');
  });

  it('falls back to generic error message for standard Error or unknown types', () => {
    expect(friendlyMessage(new Error('Random failure'))).toBe('কিছু একটা ভুল হয়েছে। আবার চেষ্টা করুন।');
    expect(friendlyMessage('something went wrong')).toBe('কিছু একটা ভুল হয়েছে। আবার চেষ্টা করুন।');
    expect(friendlyMessage(null)).toBe('কিছু একটা ভুল হয়েছে। আবার চেষ্টা করুন।');
  });
});
