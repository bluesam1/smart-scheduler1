# 6) Functional Requirements (High Level)

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
