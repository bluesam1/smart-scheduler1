/**
 * SignalR Event Type Definitions
 * 
 * These types match the event contracts defined in docs/prd/realtime.md
 */

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

export type JobRescheduled = {
  type: "JobRescheduled";
  jobId: string;
  previousStartUtc: string; // ISO8601
  previousEndUtc: string;   // ISO8601
  newStartUtc: string;       // ISO8601
  newEndUtc: string;         // ISO8601
  region: string;
};

export type JobCancelled = {
  type: "JobCancelled";
  jobId: string;
  reason: string;
  region: string;
};

/**
 * SignalR connection state
 */
export type SignalRConnectionState = 
  | "Disconnected"
  | "Connecting"
  | "Connected"
  | "Reconnecting"
  | "Disconnecting";

/**
 * Event handler types
 */
export type RecommendationReadyHandler = (event: RecommendationReady) => void;
export type JobAssignedHandler = (event: JobAssigned) => void;
export type JobRescheduledHandler = (event: JobRescheduled) => void;
export type JobCancelledHandler = (event: JobCancelled) => void;

