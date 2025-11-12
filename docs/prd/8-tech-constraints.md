# 8) Tech & Constraints

* **Architecture:** Option A — Modular Monolith (DDD + CQRS, Clean Architecture layers, MediatR handlers, EF Core, Redis caching, SignalR, Outbox for events).
* **Backend:** C# / .NET 8
* **Frontend:** TypeScript with React or Next.js
* **Real-time:** SignalR
* **DB:** PostgreSQL (preferred for PostGIS) or SQL Server
* **Cloud:** AWS
* **Auth:** Amazon Cognito (User Pool + Hosted UI; OIDC/OAuth2)
* **Maps:** OpenRouteService (primary) for distance/ETA calculations
* **Address Validation:** Google Places Autocomplete (New) for address validation and structured address parsing
* **Timezone Lookup:** Timezone lookup from lat/long coordinates (using timezone API or library)
* **Caching:** In-memory + Redis for hot reads and distance/feature caching.
* **Config & Weights:** Versioned JSON stored in a secure config store (e.g., SSM Parameter Store/AppConfig) with audit notes.
* **Regional Settings:** Travel-buffer multipliers and min/max by region; rush-mode override flags.
* **Observability:** OpenTelemetry + CloudWatch/X-Ray; dashboards for latency and success metrics.
* **AI (optional v2):** Offline GBM re-ranker after 3–6 months of data; rules engine remains as deterministic fallback.
