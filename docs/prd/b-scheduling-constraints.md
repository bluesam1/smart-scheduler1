# B. Scheduling & Constraints

* **Travel buffers:** max(10m, min(45m, ETA × 0.25)), region-configurable; applied to all legs.
* **Daily hours / consecutive jobs:** Target 8h; soft cap 10h; hard stop 12h; ≤4 in a row without a 15m break.
* **Rush jobs:** Distance weight +15%, availability +10%, rating unchanged; bypass soft caps, never hard ones; optional 10% reserved capacity if frequent.
* **Long sequential hops:** Apply score penalty when ETA(A→B) > 35m; do not hard-block.
