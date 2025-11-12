# Epic 8 — Reschedule/Cancel + Calendar Integrity

**Scope:** Reschedule with feasibility checks; cancel job; emit `JobRescheduled`/`JobCancelled`; calendar consistency. **Demo:** Move a job; affected slots recompute; signals broadcast. **Tests:** No orphaned overlaps; audit trail; webhook retries. **Telemetry:** Reschedule frequency; conflict rate.
