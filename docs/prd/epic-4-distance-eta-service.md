# Epic 4 — Distance & ETA Service

**Scope:** OpenRouteService client, ETA matrix, caching (Redis), fallback to straight‑line for coarse sort; batch + retry (Polly). **Demo:** Show ETA between base↔job and job↔job; cache hit/miss indicators. **Tests:** Contract tests against mock maps; cache TTL/eviction; resilience tests (timeouts, circuit breaker). **Telemetry:** Maps latency, cache hit %, fallback usage.
