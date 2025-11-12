# Risks & Mitigations

* **Coupling growth:** enforce dependency rules, unit tests around boundaries.
* **Hot path CPU:** precompute availability, cache ETAs, refine only top‑K with live maps.
* **Maps latency spikes:** batch + cache; fallback to straight‑line for coarse sort.
