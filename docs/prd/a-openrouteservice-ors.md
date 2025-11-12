# A) OpenRouteService (ORS)

**Purpose:** Distance matrices and ETAs for routing, used by Availability/Recommendations.

**Key Endpoints**

* **Route/Matrix:** `POST /v2/matrix/driving-car` (primary)
* **Geocode (optional):** Use your chosen geocoder; ORS offers `/geocode/search` if needed.

**Environment Variables**

* `ORS_BASE_URL` (e.g., `https://api.openrouteservice.org`)
* `ORS_API_KEY` (stored in AWS Secrets Manager; read at startup)
* `ORS_TIMEOUT_MS` (e.g., 3500)
* `ORS_MAX_BATCH` (max origins×destinations per call, e.g., 25×25)

**Resilience & Caching**

* Polly: timeout + retry (2 attempts, jittered backoff), circuit breaker (30s) on `HttpRequestException` and 5xx.
* Redis cache keys: `eta:{originHash}:{destHash}:{timeBucket}` with TTL **10–20 min**; include a small time bucket (e.g., 15 min) to avoid stale rush-hour data.
* Fallback: When ORS fails or latency spikes, coarse sort by haversine distance; refine top **K=8** with live ORS.

**Quotas & Cost Guardrails**

* Start with a conservative QPS (e.g., **5 rps**); throttle in code (token bucket).
* Batch matrix requests; prefer one matrix over N single routes per recommendation.
* Telemetry to track: ORS latency p95, error rate, cache hit %, fallback usage.

**Security**

* Store `ORS_API_KEY` in **AWS Secrets Manager**; load on boot and rotate quarterly.
* Never log keys; scrub headers in logs.

---
