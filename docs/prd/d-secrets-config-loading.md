# D) Secrets & Config Loading

* **Secrets in Secrets Manager**: `ORS_API_KEY`, DB creds.
* **Config in AppConfig/SSM**: weights, buffer policy, feature flags.
* **Bootstrap order**: load secrets → load config (with version) → warm caches.

---
