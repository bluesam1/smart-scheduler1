# Core Modules

* **Contracts:** Contractor profiles, skills/certs, calendars.
* **Jobs:** Job definitions, windows, status.
* **Scheduling:** Availability engine (hours, buffers, feasibility checks).
* **Recommendations:** Scoring, tie-breakers, rotation, slot generation (up to 3 per contractor).
* **Events:** Outbox table + background worker → EventBridge/Webhooks.
* **Read Models:** Denormalized tables/materialized views for fast UI queries.
