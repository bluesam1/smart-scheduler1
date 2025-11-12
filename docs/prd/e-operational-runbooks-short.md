# E) Operational Runbooks (Short)

* **Outage: ORS down/high latency** → circuit breaker opens → fallback to distance → raise alert; investigate, then gradually close breaker.
* **Auth outage (Cognito)** → API returns 401/403; switch to dev pool for testing only; monitor Cognito status page.
* **Config rollback** → select prior AppConfig version; verify via health endpoint that `ConfigVersion` reverted.
