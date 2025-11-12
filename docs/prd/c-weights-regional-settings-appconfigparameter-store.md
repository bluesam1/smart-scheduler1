# C) Weights & Regional Settings (AppConfig/Parameter Store)

**Storage**: JSON documents in AWS AppConfig (with deployment strategies) or SSM Parameter Store (versioned).

**1) Scoring Weights (example v1)**

```json
{
  "version": 1,
  "weights": {
    "distance": 0.45,
    "rating": 0.35,
    "availability": 0.20
  },
  "tieBreakers": ["earliestStart", "lowerDayUtilization", "shortestNextLeg"],
  "rotation": { "enabled": true, "boost": 3, "underUtilizationThreshold": 0.20 }
}
```

**2) Travel Buffer Policy (per region)**

```json
{
  "version": 1,
  "default": { "minMinutes": 10, "multiplier": 0.25, "maxMinutes": 45 },
  "overrides": {
    "downtown": { "minMinutes": 15, "multiplier": 0.30, "maxMinutes": 45 }
  }
}
```

**Change Management**

* Admin Console edits → create a new version with a change note; broadcast `ConfigChanged` event; cache‑bust on version.

---
