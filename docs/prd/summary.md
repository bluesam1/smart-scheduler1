# Summary

A single deployable .NET 8 application with strict internal boundaries: **Domain**, **Application**, **Infrastructure**, **API** (REST + SignalR), plus a separate **React/Next.js UI**. Uses **DDD + CQRS** with MediatR handlers, EF Core for persistence, Redis for hot caches, and an **Outbox** worker to publish domain events to AWS.
