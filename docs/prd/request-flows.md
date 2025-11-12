# Request Flows

1. **Get Recommendations** → validate skills/certs → compute availability (buffers/hours) → ETA matrix → score & tie-break → rank + slots → audit → return + SignalR push.
2. **Confirm Booking** → re‑validate feasibility → persist assignment (transaction) → raise `JobAssigned` → Outbox publish → contractor/dispatcher notified via SignalR.
