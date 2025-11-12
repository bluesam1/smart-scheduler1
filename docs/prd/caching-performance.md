# Caching & Performance

* Redis for contractor snapshots, skills maps, and distance matrix entries (short TTL).
* **Coarse→refine strategy:** Always perform initial coarse sort by **haversine distance**, then refine the **top 5–8 candidates** with **OpenRouteService matrix/ETA**.
* Precompute daily availability windows; cache config by version; batch distance lookups; fall back to coarse-only sort if ORS is degraded.
