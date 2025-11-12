# 10) Assumptions

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
