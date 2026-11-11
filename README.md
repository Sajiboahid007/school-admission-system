# School Admission System

A robust, concurrent online admission system with seat holds for school classes on a first-come, first-served basis.

- **Backend:** ASP.NET Core 8 Web API + Entity Framework Core 8 + PostgreSQL
- **Frontend:** React 19 + TypeScript + Vite + React Router
- **Testing:** xUnit (integration & concurrency), Vitest + React Testing Library (unit & component), Playwright (headless browser E2E)
- **Deployment:** Docker & Docker Compose with multi-stage builds

---

## Quick Start (Docker)

Only Docker and Docker Compose are needed to run the complete stack:

```bash
docker compose up --build
```

### Services & URLs

| Service | Host URL / Port | Credentials / Target |
|---|---|---|
| **Frontend** | http://localhost:3000 | React Single Page Application |
| **Backend API** | http://localhost:8080 | ASP.NET Core REST API |
| **PostgreSQL** | `localhost:5433` | User: `admission` / Password: `admission` / Database: `admission` |

> [!NOTE]
> Data is stored in the persistent Docker volume `pgdata`. Running `docker compose down` and `docker compose up` preserves all data. To completely reset the database, run `docker compose down -v`.

---

## Configuration

Environment variables can be configured directly or overridden via `.env`:

| Variable | Target Service | Default | Description |
|---|---|---|---|
| `HOLD_DURATION_SECONDS` | `backend` | `600` | Duration (in seconds) for temporary seat holds before automatic expiration. |
| `ConnectionStrings__Default` | `backend` | `Host=db;Port=5432;...` | PostgreSQL connection string for the backend API. |
| `API_URL` | `frontend` | `http://localhost:8080` | Backend API URL accessed by the user's browser (injected dynamically at container start). |

> [!TIP]
> For quick local demonstrations of countdown expiration, start with a 60-second hold duration:
> ```bash
> HOLD_DURATION_SECONDS=60 docker compose up --build
> ```

---

## Running Tests

### 1. Automated Tests via Docker Compose

```bash
# Run backend integration, concurrency, and expiry test suite (xUnit against Postgres)
docker compose --profile test run --rm backend-tests

# Run frontend unit and component tests (Vitest)
docker compose --profile test run --rm frontend-tests
```

### 2. Running Tests Locally

#### Backend Tests:
```bash
# Ensure Postgres is running on port 5433
docker compose up -d db
cd backend
dotnet test
```

#### Frontend Unit Tests (Vitest):
```bash
cd frontend
npm ci
npm test
```

#### Frontend E2E Tests (Playwright):
```bash
cd frontend
npm run test:e2e
```
*Full Playwright execution reports and artifacts are generated in [`test-results/`](./test-results).*

---

## How to Use the System

1. **Admin Class Setup:** Navigate to **"নতুন Class (Admin)"** (`/admin/classes/new`) to define available school classes and total seat quotas (e.g. *"Class 6 - Morning Shift"* with 50 seats).
2. **Student Identification:** Enter the student's 17-digit digital birth registration certificate number in the global header bar. The input automatically converts Bengali numerals (`০১২৩৪...`) to standard numbers.
3. **Class Overview & Seat Availability:** Open **"Class তালিকা"** (`/`) to view live total, held, confirmed, and available seat counts, polling every 5 seconds.
4. **Apply for a Seat:** Click **"আবেদন করুন"** next to a class with available seats to place a temporary hold.
5. **Confirm Fee Payment:** On the application page (`/holds/:holdId`), observe the real-time countdown. Click **"ভর্তি ফি দিন"** before time runs out to confirm admission. If the countdown reaches zero, the hold expires automatically without page refresh and releases the seat.
6. **My Applications:** View all previous and active applications under **"আমার আবেদন"** (`/my-holds`).

---

## Design Decisions & Documentation

- [ASSUMPTIONS.md](./ASSUMPTIONS.md): Details concurrency controls, database locking mechanisms, hold lifecycles, and architectural decisions.
- [AI_USAGE.md](./AI_USAGE.md): Full disclosure of AI tools utilized, prompt methodologies, error detections, and output validations.
