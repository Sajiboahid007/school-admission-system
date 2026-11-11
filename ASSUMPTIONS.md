# System Assumptions and Technical Decisions

This document outlines key technical decisions, data assumptions, and concurrency safeguards implemented in the School Admission System.

---

## 1. Data and Validation

- **Student Identification (`X-Student-Id`):**
  - Modeled on the Bangladeshi digital birth registration number format: exactly 17 digits (`^[0-9]{17}$`).
  - Missing header on authenticated routes yields `400 STUDENT_ID_REQUIRED`.
  - Non-conforming lengths or non-numeric characters yield `400 INVALID_STUDENT_ID`.
  - The frontend automatically normalizes Bengali digits (`০-৯`) to ASCII digits (`0-9`), strips formatting dashes/spaces, and caps input at 17 digits.
- **Class Model:**
  - Every shift or section is created as its own distinct class (e.g., *"Class 6 - Morning Shift"*).
  - Class names are trimmed and enforced unique across the school; duplicate names return `409 CLASS_EXISTS`.
  - `totalSeats` is restricted to positive integers between 1 and 10,000.
- **Identifiers:**
  - Resource IDs (`classId`, `holdId`) are GUIDs. Malformed UUID strings in route parameters are treated as non-existent resources and return `404`, avoiding internal parsing errors.
- **Error Format:**
  - All API errors strictly follow the uniform JSON schema: `{ "error": "CODE", "message": "Human readable explanation" }`.

---

## 2. Seat Holds & Application Rules

- **Active Application Quota:**
  - A student can have at most one active application (`HELD` or `CONFIRMED`) per class.
  - A student may hold seats in different classes simultaneously (e.g. Morning Shift and Day Shift).
  - A partial database unique index `(class_id, student_id) WHERE status IN ('Held', 'Confirmed')` provides hard database-level enforcement of this rule.
- **Re-application After Expiry or Cancellation:**
  - Once a hold reaches `EXPIRED` or `CANCELLED`, the student is permitted to apply for that class again.
- **Strict Expiration Enforcement:**
  - An expired hold can never be confirmed, even if seats later become free in that class, because the seat was relinquished and may have already been claimed by another applicant. Attempts to confirm an expired hold return `409 HOLD_EXPIRED`.
- **Idempotency & Authorization:**
  - Confirming an already confirmed hold returns `200` with the current hold data and makes no duplicate changes.
  - Cancelling an already cancelled hold is idempotent and returns `204 No Content`.
  - Cancelling a confirmed admission returns `409 ALREADY_CONFIRMED`.
  - Any confirmation or cancellation attempted by a different student returns `403 NOT_YOUR_HOLD`.
- **Public Hold Inspection:**
  - `GET /holds/{holdId}` does not require the `X-Student-Id` header (per assignment contract), and deliberately omits the private `studentId` from the public response payload.
- **Server Clock Synchronization (`serverTime`):**
  - Hold responses return `serverTime` (UTC). The frontend calculates the clock offset (`serverTime - localTime`) so that countdowns and expiry transitions remain completely accurate even if the user's mobile device clock is incorrect.

---

## 3. Concurrency & Multi-Process Scaling (Rule 1 & Bonus)

- **Pessimistic Row Locking (`SELECT ... FOR UPDATE`):**
  - Hold creation executes inside an explicit database transaction with row-level locking on the parent class:
    ```sql
    SELECT * FROM classes WHERE id = @classId FOR UPDATE;
    ```
  - Incoming requests competing for seats in the same class queue sequentially on that class record. Requests for other classes execute concurrently without blocking.
- **Multi-Instance Safety:**
  - Because lock arbitration is handled directly by PostgreSQL, multiple backend container processes running behind a load balancer share the identical concurrency guarantee. The total confirmed and held seats will never exceed `totalSeats`.
- **Atomic State Transitions:**
  - Confirmations and cancellations execute via atomic conditional updates:
    ```sql
    UPDATE holds
    SET status = 'Confirmed'
    WHERE id = @holdId AND status = 'Held' AND expires_at > @now;
    ```
  - This eliminates time-of-check to time-of-use (TOCTOU) race conditions during high-volume concurrent confirmations.

---

## 4. Seat Recovery & Expiration Strategy

- **Immediate Query-Time Recovery:**
  - In all read queries and seat count calculations, any hold with `status = 'Held'` whose `expires_at <= now` is dynamically treated as `Expired`. As a result, expired seats become instantly available for new applicants without requiring an external trigger or background delay.
- **Background Cleanup Worker (`HoldExpiryWorker`):**
  - A hosted background service executes every 5 seconds to sweep the database and write `status = 'Expired'` to rows whose expiry window has elapsed, ensuring data consistency and indexing efficiency.

---

## 5. Frontend & Resilience Decisions

- **Session Continuity Without Auth:**
  - Since user authentication was out of scope for the brief, the validated 17-digit student birth registration ID is persisted in `localStorage`.
- **Live Seat Availability (5-Second Polling):**
  - The class list and active hold pages poll the backend API every 5 seconds. Polling stops automatically on hold pages once a terminal state (`CONFIRMED`, `CANCELLED`, `EXPIRED`) is reached.
- **Connection Loss Protection:**
  - If a poll request fails due to network dropouts or backend restart, the UI retains the existing seat counts on-screen and displays a localized alert (*"সংযোগ বিচ্ছিন্ন, শেষ তথ্য দেখানো হচ্ছে। আবার সংযোগের চেষ্টা চলছে…"*). Once connectivity is restored, the warning clears automatically.
- **Zero-Reload Dynamic Expiration:**
  - The countdown timer component tracks remaining time. When seconds reach 0, the UI immediately flips the badge to `EXPIRED` and disables the payment button without requiring a page refresh.
- **Mobile Responsive Design:**
  - All tables are wrapped in scrollable containers (`.table-wrap { overflow-x: auto; }`) ensuring clean rendering on mobile viewports.

---

## 6. Docker & Infrastructure

- **Dynamic Frontend Configuration:**
  - The frontend Docker container uses an Nginx entrypoint script (`40-runtime-config.sh`) to inject runtime environment variables (`API_URL`) into `/usr/share/nginx/html/config.js` when the container starts. This eliminates the need to rebuild the Docker image across environments.
- **SPA Fallback Routing:**
  - Nginx is configured with `try_files $uri $uri/ /index.html;` to ensure direct navigation and page reloads on deep React Router paths (e.g., `/holds/:id` and `/my-holds`) succeed with `200 OK`.
- **Port Mapping:**
  - PostgreSQL is mapped to host port `5433` to prevent port collisions with any local PostgreSQL instances running on port `5432`.
