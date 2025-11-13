# SignalR Real-time Communication

This document describes the SignalR real-time communication setup for SmartScheduler.

## Overview

SignalR provides real-time bidirectional communication between the server and clients. The SmartScheduler API uses SignalR to notify dispatchers and contractors of important events without requiring polling.

## Hub Endpoint

**Endpoint:** `/hub/recommendations`  
**Authentication:** Required (JWT Bearer token)  
**Protocol:** WebSocket (with fallback to Server-Sent Events or Long Polling)

## Connection Flow

1. **Client connects** to `/hub/recommendations` with a valid JWT token in the `Authorization` header
2. **Client joins groups** based on their role:
   - Dispatchers: Call `JoinDispatchGroup(region)` to join `/dispatch/{region}`
   - Contractors: Call `JoinContractorGroup(contractorId)` to join `/contractor/{contractorId}`
3. **Client receives events** via SignalR messages
4. **Client handles disconnection** and reconnects automatically

## Groups

### Dispatcher Groups
- **Format:** `dispatch/{region}`
- **Purpose:** Notify dispatchers in a specific region about recommendations and job assignments
- **Example:** `dispatch/north-america`

### Contractor Groups
- **Format:** `contractor/{contractorId}`
- **Purpose:** Notify specific contractors about their job assignments
- **Example:** `contractor/123e4567-e89b-12d3-a456-426614174000`

## Events

### 1. RecommendationReady

**Event Name:** `RecommendationReady`  
**Target Groups:** `/dispatch/{region}`  
**Purpose:** Notifies dispatchers that recommendations for a job have been computed/refreshed

**Payload:**
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

**Fields:**
- `type`: Always `"RecommendationReady"` (discriminator for forward compatibility)
- `jobId`: The job identifier (UUID)
- `requestId`: The request identifier that correlates to the HTTP POST /recommendations call (UUID)
- `region`: The region identifier (string)
- `configVersion`: The configuration version used for generating recommendations (integer)
- `generatedAt`: ISO8601 timestamp when recommendations were generated

**Client Action:** After receiving this event, the client should fetch the full recommendations via HTTP GET `/api/recommendations/{jobId}`.

**Example:**
```typescript
connection.on("RecommendationReady", (payload: RecommendationReady) => {
  console.log(`Recommendations ready for job ${payload.jobId}`);
  // Fetch full recommendations via HTTP
  fetch(`/api/recommendations/${payload.jobId}`);
});
```

### 2. JobAssigned

**Event Name:** `JobAssigned`  
**Target Groups:** `/dispatch/{region}` and `/contractor/{contractorId}`  
**Purpose:** Notifies dispatchers and contractors that a job has been assigned

**Payload:**
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

**Fields:**
- `type`: Always `"JobAssigned"` (discriminator for forward compatibility)
- `jobId`: The job identifier (UUID)
- `contractorId`: The contractor identifier (UUID)
- `assignmentId`: The assignment identifier (UUID)
- `startUtc`: Assignment start time in UTC (ISO8601)
- `endUtc`: Assignment end time in UTC (ISO8601)
- `region`: The region identifier (string)
- `source`: Assignment source - either `"auto"` (automatic) or `"manual"` (manual assignment)
- `auditId`: The audit recommendation identifier for drill-down (UUID)

**Client Action:** Update job status and contractor calendars immediately.

**Example:**
```typescript
connection.on("JobAssigned", (payload: JobAssigned) => {
  console.log(`Job ${payload.jobId} assigned to contractor ${payload.contractorId}`);
  // Update UI immediately
  updateJobStatus(payload.jobId, "assigned");
  updateContractorCalendar(payload.contractorId, payload);
});
```

## Hub Methods

### JoinDispatchGroup(region: string)

Adds a dispatcher to a region group.

**Parameters:**
- `region`: The region identifier (string)

**Example:**
```typescript
await connection.invoke("JoinDispatchGroup", "north-america");
```

### LeaveDispatchGroup(region: string)

Removes a dispatcher from a region group.

**Parameters:**
- `region`: The region identifier (string)

**Example:**
```typescript
await connection.invoke("LeaveDispatchGroup", "north-america");
```

### JoinContractorGroup(contractorId: string)

Adds a contractor to their personal group.

**Parameters:**
- `contractorId`: The contractor identifier (string/UUID)

**Example:**
```typescript
await connection.invoke("JoinContractorGroup", "123e4567-e89b-12d3-a456-426614174000");
```

### LeaveContractorGroup(contractorId: string)

Removes a contractor from their personal group.

**Parameters:**
- `contractorId`: The contractor identifier (string/UUID)

**Example:**
```typescript
await connection.invoke("LeaveContractorGroup", "123e4567-e89b-12d3-a456-426614174000");
```

## TypeScript Type Definitions

```typescript
export type RecommendationReady = {
  type: "RecommendationReady";
  jobId: string;
  requestId: string;
  region: string;
  configVersion: number;
  generatedAt: string; // ISO8601
};

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

## Frontend Integration

### Connection Setup

```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5004/hub/recommendations", {
    accessTokenFactory: () => {
      // Return JWT access token from Cognito
      return getAccessToken();
    }
  })
  .withAutomaticReconnect()
  .build();

// Start connection
await connection.start();

// Join groups based on user role
if (userRole === "Dispatcher") {
  await connection.invoke("JoinDispatchGroup", userRegion);
} else if (userRole === "Contractor") {
  await connection.invoke("JoinContractorGroup", userId);
}

// Subscribe to events
connection.on("RecommendationReady", handleRecommendationReady);
connection.on("JobAssigned", handleJobAssigned);
```

### Reconnection Handling

SignalR automatically handles reconnection. The `withAutomaticReconnect()` method will:
1. Attempt to reconnect after a delay
2. Retry with exponential backoff
3. Rejoin groups after successful reconnection

### Error Handling

```typescript
connection.onclose((error) => {
  if (error) {
    console.error("SignalR connection closed with error:", error);
  } else {
    console.log("SignalR connection closed");
  }
});

connection.onreconnecting((error) => {
  console.log("SignalR reconnecting...", error);
});

connection.onreconnected((connectionId) => {
  console.log("SignalR reconnected:", connectionId);
  // Rejoin groups after reconnection
  if (userRole === "Dispatcher") {
    connection.invoke("JoinDispatchGroup", userRegion);
  } else if (userRole === "Contractor") {
    connection.invoke("JoinContractorGroup", userId);
  }
});
```

## CORS Configuration

SignalR CORS is configured in `Program.cs` to allow connections from:
- `http://localhost:3000` (Next.js development server)

CORS is configured with credentials support for authenticated requests.

## Authentication

SignalR connections require JWT authentication. The token must be provided in the `Authorization` header when establishing the connection.

The token is validated using the same Cognito JWT validation configured in Story 0.2 (Authentication & Cognito Integration).

## Performance Considerations

- Event publishing is asynchronous and non-blocking
- Events are published to groups, not individual connections
- Payloads are kept small (< 2KB) - heavy data is fetched via HTTP after the signal
- Connection state is managed automatically by SignalR

## Testing

See `src/SmartScheduler.Realtime.Tests/` for unit and integration tests covering:
- Hub connection and disconnection
- Group management
- Event publishing
- Error handling

