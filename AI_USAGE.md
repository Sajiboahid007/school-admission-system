# AI Usage Disclosure

## 1. Tools Used
- **Antigravity CLI / AI Pair Programming Assistant** (powered by Google DeepMind) utilized for architectural planning, code authoring, test design, and debugging.

---

## 2. Where AI Was Used

- **System Architecture & Planning:**
  - Decomposing the admission system requirements into eight structured, verifiable phases with incremental testing gates.
- **Backend Implementation:**
  - Structuring ASP.NET Core 8 minimal endpoints and EF Core entities.
  - Designing the concurrency protection strategy utilizing PostgreSQL `SELECT ... FOR UPDATE` row-level locks within transactions.
  - Implementing the `HoldExpiryWorker` hosted background service.
  - Authoring integration and multi-threaded concurrency tests in xUnit.
- **Frontend Development:**
  - Building React 19 + TypeScript components, including `StudentIdBar`, `ClassListPage`, `HoldPage`, and `MyHoldsPage`.
  - Writing the `usePolling` hook with interval cleanup and `useCountdown` hook with client-server clock offset adjustment.
  - Normalizing Bengali numeral input to ASCII digits and formatting localized timestamps.
- **Testing & Tooling:**
  - Authoring unit test suites with Vitest and `@testing-library/react`.
  - Configuring Playwright for headless browser end-to-end (E2E) testing and designing mock network routes.
- **Dockerization & Configuration:**
  - Crafting multi-stage Dockerfiles for backend (.NET 8 SDK + ASP.NET runtime) and frontend (Node 20 build + Nginx Alpine).
  - Configuring Docker Compose with PostgreSQL health checks and isolated testing profiles.

---

## 3. Where AI Output Encountered Issues & How They Were Resolved

1. **Playwright Route Interception Collision:**
   - **What happened:** Initial Playwright route mocking utilized wildcard patterns such as `page.route('**/holds/hold-789', ...)`. This inadvertently intercepted the browser's top-level navigation request to `http://127.0.0.1:5173/holds/hold-789`, returning raw API JSON to the browser instead of loading the Vite React application document.
   - **How it was caught:** The E2E test timed out waiting for heading elements, and Playwright's `error-context.md` showed the browser rendering raw JSON text.
   - **Fix:** Explicitly qualified all API route handlers with the backend origin: `page.route('${API_BASE}/holds/...', ...)`.

2. **Playwright Strict Mode Locator Collisions:**
   - **What happened:** In E2E tests, querying `page.locator('.hint')` failed under Playwright's strict mode because two hint elements existed concurrently in the DOM (the student ID digit counter and the class application prompt).
   - **How it was caught:** Playwright logged a strict mode violation error identifying two matching DOM nodes.
   - **Fix:** Refined locator queries using specific content selectors, such as `page.locator('.hint:has-text("16/17 অঙ্ক দেওয়া হয়েছে")')`.

3. **Time-Based Expiration Testing Strategy:**
   - **What happened:** Initial attempts to test hold countdown expiration in component tests suffered from timer drift when relying on real wall-clock delays.
   - **How it was caught:** Unit test flakiness under varying CI execution speeds.
   - **Fix:** Used Vitest's `vi.useFakeTimers()` and `vi.advanceTimersByTime()` for unit tests, while reserving short real-time intervals (2 seconds) for end-to-end browser tests in Playwright.

4. **Cross-Platform Script Line Endings in Alpine Linux:**
   - **What happened:** Shell entrypoint scripts (`40-runtime-config.sh`) edited on Windows can inherit CRLF line endings, which causes `/bin/sh` in Alpine containers to fail with `not found` or `bad interpreter`.
   - **How it was addressed:** Enforced LF line endings via `.gitattributes` (`*.sh text eol=lf`) and generated scripts with LF newline characters.

---

## 4. Verification Methodology

Every segment of AI-assisted code was subjected to automated verification gates:
- **Backend:** Verified with unit and concurrency tests (`dotnet test`) validating race safety and hold lifecycle rules.
- **Frontend Unit Tests:** Verified with Vitest (`npm test`) covering component states, error mappings, and digit normalization.
- **Frontend E2E Tests:** Verified with Playwright in headless mode (`npm run test:e2e`) verifying the complete end-to-end user workflow.
- **Build Checks:** Strict TypeScript verification (`tsc -b && vite build`) ensuring zero compiler warnings or type mismatches.
