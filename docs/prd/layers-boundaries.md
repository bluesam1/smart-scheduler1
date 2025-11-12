# Layers & Boundaries

* **Domain:** Entities, value objects, aggregates, domain events (Contracts, Jobs, Scheduling, Recommendations).
* **Application:** Commands/queries, handlers, validators; orchestrates use cases; no IO.
* **Infrastructure:** EF Core repositories, OpenRouteService Maps HTTP client (Polly resilience), Redis cache, Outbox publisher.
* **API:** Minimal APIs/Controllers, AuthZ policies, SignalR hubs for dispatcher/contractor channels.
