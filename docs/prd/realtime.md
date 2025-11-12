# Real‑time

* **Groups:** `/dispatch/{region}` (recommendations, job status) and `/contractor/{id}` (assignments/updates).
* **MVP scope:** Implement **two SignalR events only** — `RecommendationReady` (notify dispatcher UI to refresh ranked list) and `JobAssigned` (update job/contractor cards). All other data fetches may use HTTP polling.

### SignalR Message Contracts (MVP)

#### 1) `RecommendationReady`

**Channel:** `/dispatch/{region}`
**Semantics:** Server notifies that recommendations for a job have been computed/refreshed; client fetches via HTTP.

**Payload (JSON):**

```json
{
  "type": "RecommendationReady",
  "jobId": "uuid",
  "requestId": "uuid",
  "region": "string",
  "configVersion": 1,
  "generatedAt": "2025-11-08T12:34:56Z"
}
```

**Notes:** `requestId` correlates to the HTTP POST /recommendations call; `configVersion` helps cache-busting.

**TypeScript**

```ts
export type RecommendationReady = {
  type: "RecommendationReady";
  jobId: string;
  requestId: string;
  region: string;
  configVersion: number;
  generatedAt: string; // ISO8601
};
```

#### 2) `JobAssigned`

**Channel(s):** `/dispatch/{region}`, `/contractor/{contractorId}`
**Semantics:** A job was assigned; UIs update status and calendars immediately.

**Payload (JSON):**

```json
{
  "type": "JobAssigned",
  "jobId": "uuid",
  "contractorId": "uuid",
  "assignmentId": "uuid",
  "startUtc": "2025-11-08T15:00:00Z",
  "endUtc": "2025-11-08T17:00:00Z",
  "region": "string",
  "source": "auto|manual",
  "auditId": "uuid"
}
```

**Notes:** `auditId` links to the AuditRecommendation record for drill‑down.

**TypeScript**

```ts
export type JobAssigned = {
  type: "JobAssigned";
  jobId: string;
  contractorId: string;
  assignmentId: string;
  startUtc: string; // ISO8601
  endUtc: string;   // ISO8601
  region: string;
  source: "auto" | "manual";
  auditId: string;
};
```

**General Guidelines**

* Include a `type` discriminator for forward compatibility.
* All timestamps UTC ISO8601.
* Keep payloads < 2KB; heavy data is fetched via HTTP after the signal.
* Version via hub route (e.g., `/hub/v1`) or add `schemaVersion` if needed later.
