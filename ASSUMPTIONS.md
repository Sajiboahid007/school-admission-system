# Assumptions

Things the brief does not say clearly, what I decided, and why.

## Data and validation

- Student ID: Must be exactly 17 digits (Bangladeshi digital birth registration certificate format).
- Class names: Must be non-empty and unique across classes.

## Holds and seats

- A hold lasts for `HOLD_DURATION_SECONDS` (default 600 seconds).
- Expired holds cannot be confirmed even if seats are currently free, because after expiry the seat may have already been granted to another applicant. The student/parent must apply again.
- Expired and cancelled holds immediately release their seats back to the available pool.
- Hold status is dynamically calculated from `expires_at` on query, and a background worker updates the database rows periodically to keep data clean.

## Concurrency

- PostgreSQL row locking (`SELECT ... FOR UPDATE` on the class row) inside a database transaction ensures race-free seat counting and hold creation, even when scaled across multiple backend instances.
- Hold confirmation and cancellation use atomic conditional updates (`UPDATE ... WHERE status = 'HELD' AND expires_at > now`) to guarantee idempotency and avoid double confirmation.

## Frontend

## Running / Docker

- CORS is open to any origin because this is a local assignment; in production I would allow only the frontend's domain.
