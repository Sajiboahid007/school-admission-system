# API notes

Working notes taken from the brief before writing code.

## Endpoints

| # | Method & path | Header | Success | Notes |
|---|---|---|---|---|
| 1 | `POST /classes` | – | 201 `{ id, name, totalSeats }` | admin creates a class (one per shift) |
| 2 | `GET /classes` | – | 200 list with `heldSeats`, `confirmedSeats`, `availableSeats` | counts are live |
| 3 | `GET /classes/{classId}` | – | 200 one class, same shape | |
| 4 | `POST /classes/{classId}/holds` | `X-Student-Id` | 201 `{ holdId, classId, studentId, status, expiresAt }` | 409 when full |
| 5 | `GET /holds/{holdId}` | – | 200 `{ holdId, classId, status, expiresAt }` | no header, so no personal data |
| 6 | `POST /holds/{holdId}/confirm` | `X-Student-Id` | 200 `{ holdId, status: "CONFIRMED" }` | "pay fee" |
| 7 | `DELETE /holds/{holdId}` | `X-Student-Id` | 204 | cancel |
| 8 | `GET /me/holds` | `X-Student-Id` | 200 list `{ holdId, classId, className, status, expiresAt }` | |

Status values: `HELD`, `CONFIRMED`, `EXPIRED`, `CANCELLED`.
`X-Student-Id` is the 17-digit birth registration number.

## Rules

Backend:

1. `held + confirmed <= totalSeats` at all times, also under heavy parallel load.
2. Hold time comes from `HOLD_DURATION_SECONDS` (default 600). Expired seats return on their own.
3. One student can have at most one active (HELD/CONFIRMED) application per class.
4. Only the owner's own, not expired, HELD application can be confirmed.
5. Confirming the same hold many times counts once.
6. Data survives `docker compose down` / `up`.
7. Bad input is always 4xx, never 500.

Frontend:

8. Every action is possible from the UI.
9. When time runs out the UI shows EXPIRED and disables "ভর্তি ফি দিন" without reload.
10. Seat numbers refresh every few seconds.
11. After refresh / reopening the browser the student still sees their applications.
12. Every error is shown in plain language.

## Open questions (decisions go to ASSUMPTIONS.md)

- Can a confirmed admission be cancelled? -> no, 409.
- Can a student hold seats in two different classes at once? -> yes.
- After EXPIRED/CANCELLED, can the same student apply again to the same class? -> yes.
- Can an expired hold be confirmed if seats are still free? -> no, apply again.
- Malformed class/hold id: 400 or 404? -> 404 (it can never exist).
- Cancel twice? -> 204 again.
- Should `GET /holds/{id}` show the student id? -> no, it has no header.
