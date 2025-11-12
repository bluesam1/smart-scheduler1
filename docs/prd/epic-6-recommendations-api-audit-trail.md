# Epic 6 — Recommendations API + Audit Trail

**Scope:** `POST /recommendations` returns ranked contractors with **up to 3 slots** each; full audit row persisted. **Demo:** Call API from UI; see ranked cards with rationale; inspect audit detail. **Tests:** API schema tests; audit completeness; p95 < 500ms under baseline load. **Telemetry:** Latency, error rate, audit write success.
