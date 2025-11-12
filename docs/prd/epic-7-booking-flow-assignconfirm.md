# Epic 7 — Booking Flow (Assign/Confirm)

**Scope:** `POST /jobs/{id}/assign` with re‑validation; create Assignment; emit `JobAssigned` via Outbox; SignalR updates. **Demo:** Dispatcher confirms a recommendation; contractor and dispatcher UIs update in real time. **Tests:** Idempotent assignment; outbox → EventBridge/webhook delivery; UI optimistic update. **Telemetry:** Assignment success rate; event publish lag.
