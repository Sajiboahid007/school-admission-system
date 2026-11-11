import { test, expect } from '@playwright/test';

const API_BASE = 'http://localhost:8080';

test.describe('School Admission System - E2E Tests', () => {

  test('1. Student ID normalization and persistence in localStorage', async ({ page }) => {
    await page.goto('http://127.0.0.1:5173/');

    const input = page.getByPlaceholder('১৭ অঙ্ক');
    await expect(input).toBeVisible();

    // Type Bengali numerals with formatting spaces/dashes
    await input.fill('২০২৫-১২৩৪৫-৬৭৮৯০-১২৩-extra');

    // Should convert to English digits and truncate to 17 digits
    await expect(input).toHaveValue('20251234567890123');

    // Hint for /17 should disappear because it's exactly 17 digits
    await expect(page.locator('.hint:has-text("/17 অঙ্ক দেওয়া হয়েছে")')).toHaveCount(0);

    // Verify localStorage
    const savedId = await page.evaluate(() => localStorage.getItem('admission.studentId'));
    expect(savedId).toBe('20251234567890123');

    // Remove one digit and check that validation hint displays
    await input.fill('2025123456789012');
    await expect(page.locator('.hint:has-text("16/17 অঙ্ক দেওয়া হয়েছে")')).toBeVisible();
  });

  test('2. Admin: create class form, validation, and success', async ({ page }) => {
    let createdClassData: any = null;

    await page.route(`${API_BASE}/classes`, async (route) => {
      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON();
        createdClassData = body;
        return route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'class-test-1',
            name: body.name,
            totalSeats: body.totalSeats,
            heldSeats: 0,
            confirmedSeats: 0,
            availableSeats: body.totalSeats,
          }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('http://127.0.0.1:5173/admin/classes/new');

    // Try submitting with empty fields
    await page.getByRole('button', { name: 'তৈরি করুন' }).click();
    await expect(page.locator('.error')).toContainText('Class-এর নাম দিন');

    // Fill valid class name and invalid seats
    const nameInput = page.locator('label:has-text("নাম") input');
    const seatsInput = page.locator('label:has-text("Seat সংখ্যা") input');

    await nameInput.fill('Class 6 - Morning Shift');
    await seatsInput.fill('0');
    await page.getByRole('button', { name: 'তৈরি করুন' }).click();
    await expect(page.locator('.error')).toContainText('Seat সংখ্যা ১ থেকে ১০০০০ এর মধ্যে একটি পূর্ণ সংখ্যা দিন');

    // Fill valid seats and submit
    await seatsInput.fill('50');
    await page.getByRole('button', { name: 'তৈরি করুন' }).click();

    await expect(page.locator('.success')).toContainText('"Class 6 - Morning Shift" তৈরি হয়েছে (50টি seat)');
    expect(createdClassData).toEqual({
      name: 'Class 6 - Morning Shift',
      totalSeats: 50,
    });
  });

  test('3. Class list: live polling, seat numbers, apply button states and apply navigation', async ({ page }) => {
    const classes = [
      {
        id: 'c1',
        name: 'Class 6 - Morning Shift',
        totalSeats: 50,
        heldSeats: 5,
        confirmedSeats: 10,
        availableSeats: 35,
      },
      {
        id: 'c2',
        name: 'Class 6 - Day Shift (Full)',
        totalSeats: 30,
        heldSeats: 10,
        confirmedSeats: 20,
        availableSeats: 0,
      },
    ];

    await page.route(`${API_BASE}/classes`, async (route) => {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(classes),
      });
    });

    await page.route(`${API_BASE}/classes/c1/holds`, async (route) => {
      const headers = route.request().headers();
      return route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          holdId: 'hold-12345',
          classId: 'c1',
          className: 'Class 6 - Morning Shift',
          studentId: headers['x-student-id'] || '20251234567890123',
          status: 'HELD',
          expiresAt: new Date(Date.now() + 60000).toISOString(),
          serverTime: new Date().toISOString(),
        }),
      });
    });

    await page.goto('http://127.0.0.1:5173/');

    // When student ID is not valid
    await page.getByPlaceholder('১৭ অঙ্ক').fill('');
    await expect(page.locator('.hint:has-text("আবেদন করতে আগে উপরে জন্ম নিবন্ধন নম্বর দিন")')).toBeVisible();

    // Table should show both classes
    await expect(page.getByText('Class 6 - Morning Shift')).toBeVisible();
    await expect(page.getByText('Class 6 - Day Shift (Full)')).toBeVisible();

    // Buttons should be disabled because ID is invalid
    const applyButtons = page.getByRole('button', { name: 'আবেদন করুন' });
    await expect(applyButtons.first()).toBeDisabled();
    await expect(applyButtons.nth(1)).toBeDisabled();

    // Now enter valid 17-digit student ID
    await page.getByPlaceholder('১৭ অঙ্ক').fill('20251234567890123');

    // First class has available seats (35) -> Button enabled
    await expect(applyButtons.first()).toBeEnabled();

    // Second class has 0 available seats -> Button remains disabled
    await expect(applyButtons.nth(1)).toBeDisabled();

    // Mock the hold page route so navigation works smoothly
    await page.route(`${API_BASE}/holds/hold-12345`, async (route) => {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          holdId: 'hold-12345',
          classId: 'c1',
          className: 'Class 6 - Morning Shift',
          status: 'HELD',
          expiresAt: new Date(Date.now() + 60000).toISOString(),
          serverTime: new Date().toISOString(),
        }),
      });
    });

    // Click Apply on first class
    await applyButtons.first().click();

    // Should navigate to hold details page
    await page.waitForURL('**/holds/hold-12345');
    expect(page.url()).toContain('/holds/hold-12345');
  });

  test('4. Class list: network disconnection warning banner during polling', async ({ page }) => {
    let succeed = true;

    await page.route(`${API_BASE}/classes`, async (route) => {
      if (succeed) {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 'c1',
              name: 'Class 6 - Morning Shift',
              totalSeats: 30,
              heldSeats: 2,
              confirmedSeats: 8,
              availableSeats: 20,
            },
          ]),
        });
      } else {
        return route.abort('failed');
      }
    });

    await page.goto('http://127.0.0.1:5173/');
    await expect(page.getByText('Class 6 - Morning Shift')).toBeVisible();

    // Simulate network disconnect on next poll
    succeed = false;

    // Wait for the 5-second polling interval to tick
    await page.waitForTimeout(5500);

    // Wait for the polling error banner to appear
    await expect(page.locator('.error')).toContainText('সংযোগ বিচ্ছিন্ন, শেষ তথ্য দেখানো হচ্ছে');

    // Old data should still remain visible!
    await expect(page.getByText('Class 6 - Morning Shift')).toBeVisible();
  });

  test('5. Hold Details: countdown, payment confirmation, and cancellation', async ({ page }) => {
    const holdState = {
      holdId: 'hold-789',
      classId: 'c1',
      className: 'Class 6 - Morning Shift',
      status: 'HELD',
      expiresAt: new Date(Date.now() + 180000).toISOString(),
      serverTime: new Date().toISOString(),
    };

    await page.route(`${API_BASE}/holds/hold-789`, async (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(holdState),
        });
      }
      if (route.request().method() === 'DELETE') {
        holdState.status = 'CANCELLED';
        return route.fulfill({ status: 204 });
      }
      return route.fallback();
    });

    await page.route(`${API_BASE}/holds/hold-789/confirm`, async (route) => {
      holdState.status = 'CONFIRMED';
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(holdState),
      });
    });

    await page.goto('http://127.0.0.1:5173/holds/hold-789');

    // Displays class name & status badge
    await expect(page.getByRole('heading', { name: 'Class 6 - Morning Shift' })).toBeVisible();
    await expect(page.locator('.badge-HELD')).toContainText('HELD');
    await expect(page.getByTestId('countdown')).toBeVisible();

    // Confirm Payment
    const payBtn = page.getByRole('button', { name: 'ভর্তি ফি দিন' });
    const cancelBtn = page.getByRole('button', { name: 'বাতিল' });

    await expect(payBtn).toBeEnabled();
    await expect(cancelBtn).toBeEnabled();

    await payBtn.click();

    // Status updates to CONFIRMED
    await expect(page.locator('.badge-CONFIRMED')).toContainText('CONFIRMED');
    await expect(page.locator('.success')).toContainText('ভর্তি নিশ্চিত হয়েছে');
    await expect(payBtn).toBeDisabled();
    await expect(cancelBtn).toBeDisabled();
  });

  test('6. Hold Details: dynamic expiry when countdown finishes', async ({ page }) => {
    // Expires in 2 seconds
    const holdState = {
      holdId: 'hold-exp-1',
      classId: 'c1',
      className: 'Class 6 - Day Shift',
      status: 'HELD',
      expiresAt: new Date(Date.now() + 2000).toISOString(),
      serverTime: new Date().toISOString(),
    };

    await page.route(`${API_BASE}/holds/hold-exp-1`, async (route) => {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(holdState),
      });
    });

    await page.goto('http://127.0.0.1:5173/holds/hold-exp-1');

    await expect(page.locator('.badge-HELD')).toBeVisible();
    const payBtn = page.getByRole('button', { name: 'ভর্তি ফি দিন' });
    await expect(payBtn).toBeEnabled();

    // Wait for countdown to reach 0
    await page.waitForTimeout(2500);

    // Should automatically transition to EXPIRED without reloading page
    await expect(page.locator('.badge-EXPIRED')).toBeVisible();
    await expect(page.locator('.warn')).toContainText('সময় শেষ, seat ছেড়ে দেওয়া হয়েছে');
    await expect(payBtn).toBeDisabled();
  });

  test('7. My Applications: lists holds, shows status badges and links to view details', async ({ page }) => {
    const studentHolds = [
      {
        holdId: 'hold-1',
        classId: 'c1',
        className: 'Class 6 - Morning Shift',
        status: 'CONFIRMED',
        expiresAt: new Date(Date.now() + 600000).toISOString(),
        serverTime: new Date().toISOString(),
      },
      {
        holdId: 'hold-2',
        classId: 'c2',
        className: 'Class 7 - Day Shift',
        status: 'HELD',
        expiresAt: new Date(Date.now() + 300000).toISOString(),
        serverTime: new Date().toISOString(),
      },
    ];

    await page.route(`${API_BASE}/me/holds`, async (route) => {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(studentHolds),
      });
    });

    await page.goto('http://127.0.0.1:5173/my-holds');

    // First without student ID
    await page.getByPlaceholder('১৭ অঙ্ক').fill('');
    await expect(page.locator('.hint:has-text("আগে জন্ম নিবন্ধন নম্বর দিন")')).toBeVisible();

    // Set valid student ID
    await page.getByPlaceholder('১৭ অঙ্ক').fill('20251234567890123');

    // Should load the two holds
    await expect(page.getByText('Class 6 - Morning Shift')).toBeVisible();
    await expect(page.getByText('Class 7 - Day Shift')).toBeVisible();

    await expect(page.locator('.badge-CONFIRMED')).toBeVisible();
    await expect(page.locator('.badge-HELD')).toBeVisible();

    // Mock hold-2 route for clicking view
    await page.route(`${API_BASE}/holds/hold-2`, async (route) => {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(studentHolds[1]),
      });
    });

    // Click "দেখুন" on the second hold
    const viewLinks = page.getByRole('link', { name: 'দেখুন' });
    await expect(viewLinks).toHaveCount(2);
    await viewLinks.nth(1).click();

    await page.waitForURL('**/holds/hold-2');
    expect(page.url()).toContain('/holds/hold-2');
  });

});
