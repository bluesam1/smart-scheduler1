# Migration Path (if needed later)

Peel out **Recommendations** first as a separate service (stateless, compute-heavy), then **Scheduling** as traffic grows; preserve contracts via the Outbox/event model.

---
