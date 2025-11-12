# 7) Non-Functional Requirements

* **Latency:** p95 recommendation response < 500ms; rush requests target < 5s end-to-end including UI.
* **Availability:** 99.9% monthly.
* **Reliability:** Event delivery at-least-once with idempotency; monitored retries; DLQ with alerts within 5 minutes.
* **Auditability:** Persist request payload, candidate set, per-factor scores, final score, rationale, selected contractor, actor (auto/manual), and config version; retain 12 months (configurable).
* **Security:** Amazon Cognito SSO (OIDC/OAuth2); role-based access (Admin, Dispatcher, Contractor); least-privilege; contractor self-view with field filtering.
* **Privacy:** Log redaction for PII; keys rotated; optional regional data residency.
* **Observability:** OpenTelemetry traces of recommendation path; metrics on match rate, override rate, on-time arrival; structured logs.
