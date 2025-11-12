# SmartScheduler — High‑Level PRD (Editable)

## 1) Overview

SmartScheduler automates matching flooring jobs to the best qualified contractor using availability, distance, and quality signals, with real-time updates to dispatchers and contractors.

## 2) Objectives & Impact (KPIs)

* ↓ Manual scheduling time by ~40%
* ↑ Contractor utilization by ~25%
* ↑ Assignment speed by ~20%
* Improve customer satisfaction (faster, more accurate matches)

## 3) In Scope (MVP)

* Contractor CRUD (type, rating, base location, working hours)
* Availability engine (derive open slots from working hours + existing jobs)
* Distance/proximity (mapping API for travel distance/ETA)
* **Address validation (Google Places Autocomplete) with structured address storage**
* **Timezone tracking for job locations (derived from lat/long)**
* Weighted scoring & ranking (availability, rating, distance)
* Recommendation API: input (job type, desired date, location) → ranked contractors + suggested time slots
* **Dashboard statistics API (active contractors, pending jobs, utilization, assignment time)**
* **Activity feed API (recent system events for dashboard)**
* **Settings API (job types and skills management)**
* **Event publishing (JobCreated, RecommendationRequested, JobAssigned, JobRescheduled, JobCancelled, ContractorRated)**
* Dispatcher UI to view jobs, request recommendations, confirm bookings; real-time updates via SignalR

## 4) Out of Scope (MVP)

* Complex contract/billing workflows
* Native mobile apps
* Advanced ML ranking beyond baseline formula (can be phase 2 augmentation)

## 5) Users & Primary Use Cases

* Dispatcher: requests recommendations, reviews ranked list with scores/slots, confirms booking.
* Contractor: receives assignments/updates in real time; schedule reflects changes.
* Admin/Operations: manages contractor profiles and schedules.

## 6) Functional Requirements (High Level)

* **Domain Management:** Contractor CRUD (type/rating/base/schedule, skills & certifications as normalized tags).
* **Scheduling:** Determine open slots from calendars + constraints; enforce buffers between jobs (see Travel Buffer Policy) and soft/hard hour caps.
* **Distance/Proximity:** Map API for distance/ETA between jobs/sites; day start/end travel optional.
* **Scoring & Ranking:** Tunable weighted formula with tie-breakers and soft rotation; versioned config.
* **Recommendation API:** Returns ranked qualified contractors plus up to 3 suggested time slots (earliest, lowest-travel, highest-confidence).
* **Event-Driven Updates:** Publish domain events for UI and integrations (JobCreated, RecommendationRequested, JobAssigned, JobRescheduled, JobCancelled, ContractorRated).
* **Frontend:** Interactive dispatcher view with ETA, travel time, score breakdown and brief rationale; controls to pin/ban per job (with audit).

### Travel Buffer Policy

Buffer between sequential legs = **max(10m, min(45m, ETA × 0.25))**, region-configurable; applied base→first, job→job, and (optional) last→base. Buffers are part of feasibility checks and time-block carving.

### Work Hours & Fatigue Limits

Target 8h/day; soft cap 10h; hard stop 12h; max 4 consecutive jobs without a 15m break. Rush jobs may bypass soft caps but never hard ones.

### Tie-breakers & Fairness

Tie-breakers in order: (1) earliest feasible start, (2) lower same-day utilization, (3) shortest next-leg travel. Apply a small soft-rotation boost to underutilized qualified contractors to avoid monopolies.

## Domain Events & Integrations

* **Published events:** JobCreated, RecommendationRequested, JobAssigned, JobRescheduled, JobCancelled, ContractorRated.
* **Consumers:** BI/warehouse, notifications (Teams/Slack), invoicing/billing, customer comms.
* **Webhooks:** HMAC-signed callbacks for key events; customer-configurable endpoints; per-subscriber retry with jitter.
* **Delivery guarantees:** At-least-once with idempotency keys; exponential backoff; DLQ + alerts; replay tooling for ops.
* **MVP delivery:** Use **in-process publisher + Outbox table + EventLog** to satisfy event publication now; wire EventBridge/webhooks later with no API changes.

## 7) Non-Functional Requirements

* **Latency:** p95 recommendation response < 500ms; rush requests target < 5s end-to-end including UI.
* **Availability:** 99.9% monthly.
* **Reliability:** Event delivery at-least-once with idempotency; monitored retries; DLQ with alerts within 5 minutes.
* **Auditability:** Persist request payload, candidate set, per-factor scores, final score, rationale, selected contractor, actor (auto/manual), and config version; retain 12 months (configurable).
* **Security:** Amazon Cognito SSO (OIDC/OAuth2); role-based access (Admin, Dispatcher, Contractor); least-privilege; contractor self-view with field filtering.
* **Privacy:** Log redaction for PII; keys rotated; optional regional data residency.
* **Observability:** OpenTelemetry traces of recommendation path; metrics on match rate, override rate, on-time arrival; structured logs.

## 8) Tech & Constraints

* **Architecture:** Option A — Modular Monolith (DDD + CQRS, Clean Architecture layers, MediatR handlers, EF Core, Redis caching, SignalR, Outbox for events).
* **Backend:** C# / .NET 8
* **Frontend:** TypeScript with React or Next.js
* **Real-time:** SignalR
* **DB:** PostgreSQL (preferred for PostGIS) or SQL Server
* **Cloud:** AWS
* **Auth:** Amazon Cognito (User Pool + Hosted UI; OIDC/OAuth2)
* **Maps:** OpenRouteService (primary) for distance/ETA calculations
* **Address Validation:** Google Places Autocomplete (New) for address validation and structured address parsing
* **Timezone Lookup:** Timezone lookup from lat/long coordinates (using timezone API or library)
* **Caching:** In-memory + Redis for hot reads and distance/feature caching.
* **Config & Weights:** Versioned JSON stored in a secure config store (e.g., SSM Parameter Store/AppConfig) with audit notes.
* **Regional Settings:** Travel-buffer multipliers and min/max by region; rush-mode override flags.
* **Observability:** OpenTelemetry + CloudWatch/X-Ray; dashboards for latency and success metrics.
* **AI (optional v2):** Offline GBM re-ranker after 3–6 months of data; rules engine remains as deterministic fallback.

## 9) Success Criteria (Verification)

* Integration tests covering contractor setup → job creation → ranked output → booking event flow, with documentation of scoring algorithm and architecture decisions. Demo + brief technical write-up.

## 10) Assumptions

### Core Business Assumptions
* Contractors provide reliable working hours and accept travel within a radius.
* Jobs have clear type, duration estimate, and location.
* Dispatchers can override recommendations when needed.

### MVP Scope Assumptions (2-Day Timeline)

#### Baseline Metrics & KPI Measurement
* Baseline metrics are not required for MVP — System will track metrics going forward, but historical baselines are not needed to demonstrate the 40%/25%/20% improvements.
* KPI tracking: System will log key metrics (scheduling time, utilization, assignment speed) for future comparison, but MVP focuses on functional delivery.

#### Contractor Matching & Availability
* **No matches scenario:** If no qualified contractors are found, recommendation API returns empty list with message: "No qualified contractors available for this job."
* **Maximum search radius:** 50 miles (80 km) default radius for contractor matching (configurable post-MVP).
* **Skills/certifications:** Self-reported for MVP; no external verification required.
* **Minimum contractor profile (required):** Name/ID, base location, working hours (at least one day/time range), at least one skill/certification tag, rating (defaults to 50/100 if no history).

#### Rush Jobs & Priority Handling
* **Rush job identification:** Identified by `priority` field with values: `Normal`, `High`, `Rush`.
* **Rush job definition:** Job is "rush" when `priority = "Rush"` OR desired date is within 24 hours of job creation.
* **Reserved capacity:** "10% reserved capacity" feature is out of scope for MVP.

#### Assignment Workflow & Conflicts
* **Contractor rejection:** For MVP, contractors cannot reject assignments. Assignments are auto-confirmed when dispatcher confirms booking.
* **Concurrent conflicts:** Use optimistic concurrency with database-level locking. First transaction succeeds; second receives `409 Conflict`.
* **Availability re-validation:** System re-validates availability immediately before persisting assignment.
* **No reservation locks:** Assignment is atomic: validate → persist → publish event.

#### Contractor Ratings & Initial State
* **Initial rating:** New contractors start with default rating of 50/100 (neutral midpoint).
* **Rating components:** All components (on-time, first-pass, CSAT, documentation) default to 50% until real data accumulates.
* **Historical data:** Can be imported via admin interface (manual entry or CSV import).

#### Service Windows & Scheduling Constraints
* **Outside business hours:** Jobs can be scheduled outside contractor business hours with dispatcher override. System warns but does not block.
* **No available contractors:** Returns empty recommendation list with message: "No contractors available for requested time window."
* **Customer-specific requirements:** No customer-specific service window overrides for MVP.

#### Data Retention & Governance
* **Default retention:** 12 months for audit logs and recommendation history.
* **Retention configuration:** Globally configurable (not per-region). MVP uses 12 months default; configuration UI is post-MVP.
* **Data archival:** After retention expires, data is soft-deleted (marked as archived), not physically deleted.

#### Regional Configuration
* **Region definition:** MVP supports single default region ("Default") with hardcoded travel buffer policy.
* **Default region configuration:** Travel buffer `max(10m, min(45m, ETA × 0.25))`, rush mode enabled, no region-specific overrides.
* **Multi-region support:** Post-MVP feature.

#### Distance & Proximity (Mapping API) — **REQUIRED**
* **Mapping API integration is MANDATORY** (per Project Brief section 2.2): System MUST integrate with OpenRouteService to calculate real-time travel distances and ETAs between job sites.
* **Distance scoring is REQUIRED** (per Project Brief section 2.3): Distance must be included in weighted scoring formula.
* **Implementation strategy (coarse→refine):**
  * Step 1 (Coarse): Use Haversine distance for initial sorting/filtering of all candidates.
  * Step 2 (Refine): Use OpenRouteService matrix/ETA API for top 5–8 candidates to get accurate travel times.
* **Distance calculations required:** Base location → Job site, Job site → Job site, all used in scoring and availability calculations.
* **Caching:** Cache ORS results in Redis with TTL 10–20 minutes (time-bucketed for rush-hour variations).
* **Error handling:** If OpenRouteService unavailable, fallback to Haversine distance (degraded mode, not primary approach).

#### Address Validation & Structured Address — **REQUIRED**
* **Google Places Autocomplete (New) integration:** System MUST use Google Places Autocomplete for address validation and structured address parsing.
* **Cost model:** Autocomplete sessions are no-charge; only terminating calls (Place Details or Address Validation) are charged.
* **Address structure:** All addresses must be stored with structured components: street address, city, state, postal code, country, and formatted address.
* **Validation flow:**
  * Frontend uses Google Places Autocomplete for address input
  * On selection, call Place Details API to get structured address components
  * Backend stores structured address + lat/long coordinates
  * Cache Place Details results to minimize API calls
* **Timezone lookup:** Job locations must include timezone (IANA timezone identifier) derived from lat/long coordinates.
* **Error handling:** If Google Places API unavailable, allow manual address entry with lat/long lookup for timezone.

#### Contractor Interaction & Notifications
* **Contractor actions:** For MVP, contractors have read-only view of their schedule. They receive real-time updates via SignalR but cannot accept/reject/reschedule.
* **Assignment confirmation:** Assignments are auto-confirmed when dispatcher confirms booking. No contractor acceptance required.
* **Contractor schedule view:** Read-only view of assigned jobs, working hours, profile, and real-time notifications.

#### Audit & Compliance
* **Audit trail detail:** Manual overrides require actor, timestamp, original recommendation (top 3), selected contractor (if different), and optional reason/note.
* **Compliance:** MVP assumes no specific compliance requirements beyond basic audit logging. GDPR/industry-specific features are post-MVP.
* **PII redaction:** Basic redaction (email addresses and phone numbers) from logs. Full redaction strategy is post-MVP.

#### MVP Scope Simplifications
* **Travel buffer:** For 48-hour MVP, use constant 15-minute buffer (simplified). Full Travel Buffer Policy implemented in Epic 3.
* **Config storage:** Weights and config stored in `appsettings.json` or environment variables. SSM Parameter Store/AppConfig integration is post-MVP.
* **Pin/ban controls:** Out of scope for 48-hour MVP. Dispatchers can manually override by selecting any contractor from ranked list.
* **Soft rotation:** Simplified for MVP — basic implementation with fixed boost value (3 points). Full decay logic is post-MVP.
* **Tie-breakers:** For MVP, use single tie-breaker (earliest feasible start). Full tie-breaker chain is post-MVP.

#### Authentication & Authorization
* **Cognito setup:** Simplified Cognito Setup (User Pool + Hosted UI + JWT validation):
  * Create Cognito User Pool with 3 groups: `Admin`, `Dispatcher`, `Contractor`.
  * Use Cognito Hosted UI for login (no custom UI needed).
  * Pre-create 3 test users (one per role) for demo.
  * API validates JWT tokens from Cognito issuer using standard .NET JWT middleware.
  * Frontend stores tokens in memory/session storage.
  * MFA disabled for MVP.
* **Role-based access:** Three roles exist, but MVP focuses on Dispatcher workflow. Admin and Contractor views are minimal.

#### Testing & Validation
* **Integration tests:** MVP includes core integration tests covering: Contractor CRUD → Job creation → Recommendation request → Assignment flow, basic scoring algorithm validation, event publishing verification.
* **Load testing:** Out of scope for 48-hour MVP. Focus on functional correctness.
* **E2E tests:** Basic E2E smoke test (UI → API → DB) included in Epic 0.

### Technical Stack Decisions

* **Authentication:** Simplified Cognito Setup (User Pool + Hosted UI + JWT validation).
* **Database:** PostgreSQL 15+ with PostGIS extension (preferred per PRD Section 8).
* **Frontend:** Next.js 14+ (App Router) — per Epic 0 and PRD folder layout.

### Project Brief Requirements Compliance

All Project Brief requirements are met:
* ✅ Domain Management (Contractor CRUD)
* ✅ Scheduling Engine Logic (Availability Engine + Distance & Proximity Check with OpenRouteService)
* ✅ Intelligent Scoring and Ranking (weighted formula with distance scoring)
* ✅ Event-Driven Updates (JobAssigned, ScheduleUpdated, ContractorRated)
* ✅ Architecture Principles (DDD, CQRS, Layer Separation)
* ✅ Technical Stack (C# .NET 8, Next.js, SignalR, PostgreSQL, AWS, OpenRouteService)
* ✅ Performance (<500ms latency)
* ✅ Dispatcher UI (view jobs, request recommendations, confirm booking)
* ✅ Code Quality (DDD/CQRS, documentation, integration tests)

---

# Resolved Product Decisions

## A. Domain & Data

* **Job attributes:** type, duration, required skills/certs, geo-coded address, service window, access/parking notes, tools, priority.
* **Contractor rating:** 0–100 composite (40% on-time, 30% first-pass completion, 20% CSAT last 90 days, 10% documentation); event-driven updates; admin adjustments require audit.
* **Working-hours exceptions:** Per-contractor calendar with holidays/blackouts and a daily unpaid break (default 30m).
* **Skills/certifications:** Hard compatibility rule—job must-have ⊆ contractor profile.

## B. Scheduling & Constraints

* **Travel buffers:** max(10m, min(45m, ETA × 0.25)), region-configurable; applied to all legs.
* **Daily hours / consecutive jobs:** Target 8h; soft cap 10h; hard stop 12h; ≤4 in a row without a 15m break.
* **Rush jobs:** Distance weight +15%, availability +10%, rating unchanged; bypass soft caps, never hard ones; optional 10% reserved capacity if frequent.
* **Long sequential hops:** Apply score penalty when ETA(A→B) > 35m; do not hard-block.

## C. Scoring & Tuning

* **Weight control:** Ops-admin UI for versioned JSON weights; audited changes.
* **Tie-breakers:** earliest feasible start → lower same-day utilization → shortest next-leg travel.
* **Fairness/rotation:** Small boost to underutilized qualified contractors with decay toward zero as utilization equalizes.

## D. Recommendation UX

* **Dispatcher view:** ETA, travel time, score breakdown (availability/rating/distance/rotation), short rationale.
* **Suggested slots:** Up to 3 per contractor: earliest, lowest-travel, highest-confidence.
* **Pin/ban controls:** Per-job with optional expiry and note; audited.

## E. Events & Integrations

* **External consumers:** BI/warehouse, notifications (Teams/Slack), invoicing/billing, customer comms.
* **Delivery & retries:** At-least-once, idempotency keys, exponential backoff, DLQ + alerts, replay tool.
* **Webhooks:** HMAC-signed for key events; per-subscriber retry with jitter; configurable endpoints.

## F. Data Governance & Ops

* **Audit ‘why selected’:** Store request, candidate set, per-factor scores, final score, rationale, selection actor, config version (12-month retention configurable).
* **Access controls:** Roles—Admin, Dispatcher, Contractor; Amazon Cognito SSO; least-privilege; contractor self-view with field filtering.
* **SLOs & privacy:** 99.9% availability; p95 <500ms; alert <5m; incident response <1h; PII redaction; optional regional residency.

## G. AI Augmentation (Optional)

* **Human-readable rationale:** Deterministic, templated explanation using only scored inputs (≤200 chars).
* **Guardrails:** No external lookup; include numeric factors; deterministic templates.
* **V2 model:** After 3–6 months, train a GBM re-ranker; keep rules as fallback.

# Selected Architecture — Option A (Modular Monolith)

## Summary

A single deployable .NET 8 application with strict internal boundaries: **Domain**, **Application**, **Infrastructure**, **API** (REST + SignalR), plus a separate **React/Next.js UI**. Uses **DDD + CQRS** with MediatR handlers, EF Core for persistence, Redis for hot caches, and an **Outbox** worker to publish domain events to AWS.

## Layers & Boundaries

* **Domain:** Entities, value objects, aggregates, domain events (Contracts, Jobs, Scheduling, Recommendations).
* **Application:** Commands/queries, handlers, validators; orchestrates use cases; no IO.
* **Infrastructure:** EF Core repositories, OpenRouteService Maps HTTP client (Polly resilience), Redis cache, Outbox publisher.
* **API:** Minimal APIs/Controllers, AuthZ policies, SignalR hubs for dispatcher/contractor channels.

## Core Modules

* **Contracts:** Contractor profiles, skills/certs, calendars.
* **Jobs:** Job definitions, windows, status.
* **Scheduling:** Availability engine (hours, buffers, feasibility checks).
* **Recommendations:** Scoring, tie-breakers, rotation, slot generation (up to 3 per contractor).
* **Events:** Outbox table + background worker → EventBridge/Webhooks.
* **Read Models:** Denormalized tables/materialized views for fast UI queries.

## Request Flows

1. **Get Recommendations** → validate skills/certs → compute availability (buffers/hours) → ETA matrix → score & tie-break → rank + slots → audit → return + SignalR push.
2. **Confirm Booking** → re‑validate feasibility → persist assignment (transaction) → raise `JobAssigned` → Outbox publish → contractor/dispatcher notified via SignalR.

## Data Model (simplified)

* Contractor, ContractorSkill, ContractorCalendar
* Job, Assignment
* WeightsConfig (versioned JSON)
* AuditRecommendation (inputs, candidates, scores, selection, config version)
* Outbox (event type, payload, timestamps)

## Caching & Performance

* Redis for contractor snapshots, skills maps, and distance matrix entries (short TTL).
* **Coarse→refine strategy:** Always perform initial coarse sort by **haversine distance**, then refine the **top 5–8 candidates** with **OpenRouteService matrix/ETA**.
* Precompute daily availability windows; cache config by version; batch distance lookups; fall back to coarse-only sort if ORS is degraded.

## Real‑time

* **Groups:** `/dispatch/{region}` (recommendations, job status) and `/contractor/{id}` (assignments/updates).
* **MVP scope:** Implement **two SignalR events only** — `RecommendationReady` (notify dispatcher UI to refresh ranked list) and `JobAssigned` (update job/contractor cards). All other data fetches may use HTTP polling.

### SignalR Message Contracts (MVP)

#### 1) `RecommendationReady`

**Channel:** `/dispatch/{region}`
**Semantics:** Server notifies that recommendations for a job have been computed/refreshed; client fetches via HTTP.

**Payload (JSON):**

```json
{
  "type": "RecommendationReady",
  "jobId": "uuid",
  "requestId": "uuid",
  "region": "string",
  "configVersion": 1,
  "generatedAt": "2025-11-08T12:34:56Z"
}
```

**Notes:** `requestId` correlates to the HTTP POST /recommendations call; `configVersion` helps cache-busting.

**TypeScript**

```ts
export type RecommendationReady = {
  type: "RecommendationReady";
  jobId: string;
  requestId: string;
  region: string;
  configVersion: number;
  generatedAt: string; // ISO8601
};
```

#### 2) `JobAssigned`

**Channel(s):** `/dispatch/{region}`, `/contractor/{contractorId}`
**Semantics:** A job was assigned; UIs update status and calendars immediately.

**Payload (JSON):**

```json
{
  "type": "JobAssigned",
  "jobId": "uuid",
  "contractorId": "uuid",
  "assignmentId": "uuid",
  "startUtc": "2025-11-08T15:00:00Z",
  "endUtc": "2025-11-08T17:00:00Z",
  "region": "string",
  "source": "auto|manual",
  "auditId": "uuid"
}
```

**Notes:** `auditId` links to the AuditRecommendation record for drill‑down.

**TypeScript**

```ts
export type JobAssigned = {
  type: "JobAssigned";
  jobId: string;
  contractorId: string;
  assignmentId: string;
  startUtc: string; // ISO8601
  endUtc: string;   // ISO8601
  region: string;
  source: "auto" | "manual";
  auditId: string;
};
```

**General Guidelines**

* Include a `type` discriminator for forward compatibility.
* All timestamps UTC ISO8601.
* Keep payloads < 2KB; heavy data is fetched via HTTP after the signal.
* Version via hub route (e.g., `/hub/v1`) or add `schemaVersion` if needed later.

## Security & Privacy

* Amazon Cognito SSO (OIDC/OAuth2; roles: Admin, Dispatcher, Contractor); policy-based auth; field-level filtering for contractor self‑view; PII redaction in logs.

## Observability

* OpenTelemetry tracing (end‑to‑end request paths); metrics: p95 latency, override rate, on‑time arrival, outbox lag; structured logs with JobId/ContractorId/ConfigVersion.

## Folder Layout (repo)

```
src/
  Api/
  Application/
  Domain/
    Contracts/
    Jobs/
    Scheduling/
    Recommendations/
    Shared/
  Infrastructure/
  Realtime/   # SignalR hub
  Workers/    # Outbox publisher, cleanup
ui/
  web/        # Next.js
```

## Risks & Mitigations

* **Coupling growth:** enforce dependency rules, unit tests around boundaries.
* **Hot path CPU:** precompute availability, cache ETAs, refine only top‑K with live maps.
* **Maps latency spikes:** batch + cache; fallback to straight‑line for coarse sort.

## Migration Path (if needed later)

Peel out **Recommendations** first as a separate service (stateless, compute-heavy), then **Scheduling** as traffic grows; preserve contracts via the Outbox/event model.

---

# 48‑Hour Compliant MVP (Scope Lock)

**Goal:** Deliver a working end‑to‑end demo that satisfies the brief while minimizing build surface.

* **UI:** Single dispatcher page to create jobs, request recommendations, view **scores + up to 3 slots**, and **Assign**.
* **Signals:** SignalR limited to `RecommendationReady` and `JobAssigned`; other updates via HTTP.
* **Mapping:** Haversine for coarse ranking; **ORS matrix** only for top 5–8 candidates.
* **Scoring:** Fixed JSON weights (distance/rating/availability); single tie-breaker (earliest feasible start) acceptable for MVP.
* **Scheduling:** Constant buffer 15m acceptable if regional policy not yet configured.
* **Events:** Outbox + in-process publisher + EventLog (simulate bus) to satisfy “publish events”.
* **Audit:** One `AuditRecommendation` JSON row per request.
* **Perf:** Target p95 < 500ms for recommendations.

---

# Vertical Slice Epics (Testable & Demoable)

Each epic is an end‑to‑end slice (API, domain, data, UI, events) that ships visible value and measurable telemetry. Order is intentional so progress is always demoable.

## Epic 0 — Foundations & Hello Dispatch

**Scope:** Repo, CI/CD, trunk-based flow; auth (Cognito), roles; empty DB; feature flags; skeleton Next.js app; SignalR handshake. **Demo:** Dispatcher logs in, sees empty “Jobs” and “Contractors” lists that live‑update on a mocked event. **Tests:** AuthZ policy tests; health checks; E2E smoke (UI→API→DB round‑trip). **Telemetry:** Uptime, API p95, Web Vitals.

## Epic 1 — Contractor Profiles (CRUD + Skills/Calendars)

**Scope:** Contractor create/edit; skills/certs tagging; base location; working hours & blackout calendar. **Demo:** Add a contractor and see profile + calendar in UI. **Tests:** Domain validation (skills, hours), API contract tests, DB migrations; UI form tests. **Telemetry:** Count of active contractors; validation error rate.

## Epic 2 — Job Intake (Attributes & Windows)

**Scope:** Job CRUD with type, duration, service window, address geocoding, priority, required skills/certs. **Demo:** Create a job; it appears in job board with map pin. **Tests:** Address → geo pipeline; compatibility validation; migration tests. **Telemetry:** Job creation rate; geocode success/latency.

## Epic 3 — Availability Engine v1 (Hours + Buffers)

**Scope:** Compute feasible slots per contractor from hours, existing jobs, and **Travel Buffer Policy**. **Demo:** Given one contractor and two jobs, UI shows feasible/blocked windows changing as jobs are added. **Tests:** Deterministic slot calculations; edge cases (breaks, blackout); property‑based tests for overlaps. **Telemetry:** Slot computation time; late‑arrival simulation checks.

## Epic 4 — Distance & ETA Service

**Scope:** OpenRouteService client, ETA matrix, caching (Redis), fallback to straight‑line for coarse sort; batch + retry (Polly). **Demo:** Show ETA between base↔job and job↔job; cache hit/miss indicators. **Tests:** Contract tests against mock maps; cache TTL/eviction; resilience tests (timeouts, circuit breaker). **Telemetry:** Maps latency, cache hit %, fallback usage.

## Epic 5 — Scoring & Ranking v1 (Rules)

**Scope:** Tunable weights (JSON, versioned); tie‑breakers; soft rotation; long‑hop penalty. **Demo:** Same job inputs produce a ranked list with visible score breakdown. **Tests:** Deterministic scores for fixtures; weight‑change rollback; tie‑breaker order. **Telemetry:** Distribution of scores; override frequency (manual vs auto).

## Epic 6 — Recommendations API + Audit Trail

**Scope:** `POST /recommendations` returns ranked contractors with **up to 3 slots** each; full audit row persisted. **Demo:** Call API from UI; see ranked cards with rationale; inspect audit detail. **Tests:** API schema tests; audit completeness; p95 < 500ms under baseline load. **Telemetry:** Latency, error rate, audit write success.

## Epic 7 — Booking Flow (Assign/Confirm)

**Scope:** `POST /jobs/{id}/assign` with re‑validation; create Assignment; emit `JobAssigned` via Outbox; SignalR updates. **Demo:** Dispatcher confirms a recommendation; contractor and dispatcher UIs update in real time. **Tests:** Idempotent assignment; outbox → EventBridge/webhook delivery; UI optimistic update. **Telemetry:** Assignment success rate; event publish lag.

## Epic 8 — Reschedule/Cancel + Calendar Integrity

**Scope:** Reschedule with feasibility checks; cancel job; emit `JobRescheduled`/`JobCancelled`; calendar consistency. **Demo:** Move a job; affected slots recompute; signals broadcast. **Tests:** No orphaned overlaps; audit trail; webhook retries. **Telemetry:** Reschedule frequency; conflict rate.

## Epic 9 — Admin Weights Console & Feature Flags

**Scope:** UI to edit weights/config with notes; environment scoping; safe rollout via flags. **Demo:** Change weights and immediately see effect on next recommendation (config version visible). **Tests:** AuthZ (Admin‑only); config validation; rollback behavior. **Telemetry:** Config change log; impact on ranking variance.

## Epic 10 — Observability & SLA Guardrails

**Scope:** OpenTelemetry traces, metrics dashboards, alerts (p95 > 500ms, webhook DLQ growth, outbox lag > threshold), redaction. **Demo:** Induce a maps timeout; see alert & trace waterfall. **Tests:** Alerting rules in CI; redaction unit tests. **Telemetry:** Covered by dashboards.

## Epic 11 — Quality Gate & Launch Readiness

**Scope:** Security review, load test, data retention & PII policies, incident runbook, DR plan; “minimum viable docs.” **Demo:** Load test report; runbook walkthrough; DR drill summary. **Tests:** Soak tests; role/permission checks; backup/restore. **Telemetry:** Capacity headroom; error budget burn.

---

## Tracking & Visibility

* **Definition of Done (per epic):** Working vertical slice in prod‑like env, automated tests passing, dashboards updated, and a short Loom‑style demo.
* **Progress view:** Kanban lanes by epic with % complete; weekly burn‑up by count of passing E2E scenarios.
* **Test artifacts:** For each epic, store fixtures and golden files to guarantee determinism.

## Suggested Order of Execution

0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11

##

---

# Configuration Appendix

## A) OpenRouteService (ORS)

**Purpose:** Distance matrices and ETAs for routing, used by Availability/Recommendations.

**Key Endpoints**

* **Route/Matrix:** `POST /v2/matrix/driving-car` (primary)
* **Geocode (optional):** Use your chosen geocoder; ORS offers `/geocode/search` if needed.

**Environment Variables**

* `ORS_BASE_URL` (e.g., `https://api.openrouteservice.org`)
* `ORS_API_KEY` (stored in AWS Secrets Manager; read at startup)
* `ORS_TIMEOUT_MS` (e.g., 3500)
* `ORS_MAX_BATCH` (max origins×destinations per call, e.g., 25×25)

**Resilience & Caching**

* Polly: timeout + retry (2 attempts, jittered backoff), circuit breaker (30s) on `HttpRequestException` and 5xx.
* Redis cache keys: `eta:{originHash}:{destHash}:{timeBucket}` with TTL **10–20 min**; include a small time bucket (e.g., 15 min) to avoid stale rush-hour data.
* Fallback: When ORS fails or latency spikes, coarse sort by haversine distance; refine top **K=8** with live ORS.

**Quotas & Cost Guardrails**

* Start with a conservative QPS (e.g., **5 rps**); throttle in code (token bucket).
* Batch matrix requests; prefer one matrix over N single routes per recommendation.
* Telemetry to track: ORS latency p95, error rate, cache hit %, fallback usage.

**Security**

* Store `ORS_API_KEY` in **AWS Secrets Manager**; load on boot and rotate quarterly.
* Never log keys; scrub headers in logs.

---

## B) Amazon Cognito (User Pool + Hosted UI)

**Purpose:** AuthN/AuthZ for Admin, Dispatcher, Contractor.

**Setup Checklist**

1. **Create User Pool** with attributes: email (required), given/family name (optional). Disable public sign‑up if internal.
2. **App Client** (no secret for SPA): enable authorization code flow; allowed OAuth scopes: `openid`, `email`, `profile`.
3. **Hosted UI**: configure domain (e.g., `smartscheduler.auth.us-east-1.amazoncognito.com`).
4. **Callback/Sign‑out URLs**: `https://app.<your-domain>/auth/callback`, `https://app.<your-domain>/auth/signout`.
5. **Groups → Roles**: create groups `Admin`, `Dispatcher`, `Contractor`; map to app roles/claims.
6. **JWKS/JWT validation** in API: cache JWKS; validate `iss`, `aud`, `exp`, and `groups` claim.
7. **Token lifetimes**: default is fine (60m access/1d refresh); adjust per security policy.
8. **Local Dev**: create a dev pool and app client; set environment variables below.

**Environment Variables**

* `COGNITO_USER_POOL_ID`
* `COGNITO_APP_CLIENT_ID`
* `COGNITO_REGION` (e.g., `us-east-1`)
* `COGNITO_AUTH_DOMAIN` (Hosted UI domain)
* `JWT_ALLOWED_AUDIENCE` (App Client ID)
* `JWT_ALLOWED_ISSUER` (`https://cognito-idp.<region>.amazonaws.com/<poolId>`)

**API Integration (ASP.NET Core)**

* Use `AddAuthentication().AddJwtBearer(...)` pointed at Cognito issuer.
* Map Cognito `groups` claim → policy‑based authorization: `RequireRole("Admin")`, etc.

**Frontend (Next.js)**

* Use Hosted UI (redirect) or Cognito OAuth libraries; store tokens in memory; refresh via silent renew.

**Security**

* Enforce MFA for Admin; optional for others.
* Rotate app client secret if you ever enable confidential clients (backend‑to‑backend).

---

## C) Weights & Regional Settings (AppConfig/Parameter Store)

**Storage**: JSON documents in AWS AppConfig (with deployment strategies) or SSM Parameter Store (versioned).

**1) Scoring Weights (example v1)**

```json
{
  "version": 1,
  "weights": {
    "distance": 0.45,
    "rating": 0.35,
    "availability": 0.20
  },
  "tieBreakers": ["earliestStart", "lowerDayUtilization", "shortestNextLeg"],
  "rotation": { "enabled": true, "boost": 3, "underUtilizationThreshold": 0.20 }
}
```

**2) Travel Buffer Policy (per region)**

```json
{
  "version": 1,
  "default": { "minMinutes": 10, "multiplier": 0.25, "maxMinutes": 45 },
  "overrides": {
    "downtown": { "minMinutes": 15, "multiplier": 0.30, "maxMinutes": 45 }
  }
}
```

**Change Management**

* Admin Console edits → create a new version with a change note; broadcast `ConfigChanged` event; cache‑bust on version.

---

## D) Secrets & Config Loading

* **Secrets in Secrets Manager**: `ORS_API_KEY`, DB creds.
* **Config in AppConfig/SSM**: weights, buffer policy, feature flags.
* **Bootstrap order**: load secrets → load config (with version) → warm caches.

---

## E) Operational Runbooks (Short)

* **Outage: ORS down/high latency** → circuit breaker opens → fallback to distance → raise alert; investigate, then gradually close breaker.
* **Auth outage (Cognito)** → API returns 401/403; switch to dev pool for testing only; monitor Cognito status page.
* **Config rollback** → select prior AppConfig version; verify via health endpoint that `ConfigVersion` reverted.
