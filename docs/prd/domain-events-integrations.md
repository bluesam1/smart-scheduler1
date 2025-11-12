# Domain Events & Integrations

* **Published events:** JobCreated, RecommendationRequested, JobAssigned, JobRescheduled, JobCancelled, ContractorRated.
* **Consumers:** BI/warehouse, notifications (Teams/Slack), invoicing/billing, customer comms.
* **Webhooks:** HMAC-signed callbacks for key events; customer-configurable endpoints; per-subscriber retry with jitter.
* **Delivery guarantees:** At-least-once with idempotency keys; exponential backoff; DLQ + alerts; replay tooling for ops.
* **MVP delivery:** Use **in-process publisher + Outbox table + EventLog** to satisfy event publication now; wire EventBridge/webhooks later with no API changes.
