# E. Events & Integrations

* **External consumers:** BI/warehouse, notifications (Teams/Slack), invoicing/billing, customer comms.
* **Delivery & retries:** At-least-once, idempotency keys, exponential backoff, DLQ + alerts, replay tool.
* **Webhooks:** HMAC-signed for key events; per-subscriber retry with jitter; configurable endpoints.
