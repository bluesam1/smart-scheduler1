# Data Models

The core data models represent the business entities shared between frontend and backend. These TypeScript interfaces can be shared across the stack for type safety.

### Contractor

**Purpose:** Represents a contractor profile with skills, certifications, base location, working hours, and rating. Used for matching against job requirements.

**Key Attributes:**
- `id`: `string` (UUID) - Unique contractor identifier
- `name`: `string` - Contractor name
- `baseLocation`: `GeoLocation` - Base location (lat/lng) for distance calculations
- `rating`: `number` (0-100) - Composite rating (defaults to 50 for new contractors)
- `workingHours`: `WorkingHours[]` - Weekly working hours schedule
- `skills`: `string[]` - Skills/certifications tags (normalized)
- `calendar`: `ContractorCalendar` - Holidays, blackouts, exceptions
- `createdAt`: `string` (ISO8601) - Creation timestamp
- `updatedAt`: `string` (ISO8601) - Last update timestamp

**TypeScript Interface:**

```typescript
export interface Contractor {
  id: string;
  name: string;
  baseLocation: GeoLocation;
  rating: number; // 0-100, defaults to 50
  workingHours: WorkingHours[];
  skills: string[]; // Normalized skill/certification tags
  calendar?: ContractorCalendar;
  createdAt: string;
  updatedAt: string;
}

export interface GeoLocation {
  latitude: number;
  longitude: number;
  address?: string; // Optional formatted address
}

export interface WorkingHours {
  dayOfWeek: number; // 0-6 (Sunday-Saturday)
  startTime: string; // HH:mm format
  endTime: string; // HH:mm format
  timeZone: string; // IANA timezone (e.g., "America/New_York")
}

export interface ContractorCalendar {
  holidays: Date[]; // Blackout dates
  exceptions: CalendarException[]; // Custom overrides
  dailyBreakMinutes?: number; // Default 30 minutes
}

export interface CalendarException {
  date: string; // ISO8601 date
  type: "holiday" | "override";
  workingHours?: WorkingHours; // Override for specific date
}
```

**Relationships:**
- One-to-many with `Assignment` (contractor can have multiple assignments)
- Many-to-many with `Job` through `Assignment` (contractor can be assigned to multiple jobs)
- Skills must match job requirements (hard compatibility rule: job must-have ⊆ contractor skills)

### Job

**Purpose:** Represents a flooring job that needs to be assigned to a contractor. Contains job type, duration, location, required skills, and service window.

**Key Attributes:**
- `id`: `string` (UUID) - Unique job identifier
- `type`: `string` - Job type (e.g., "Flooring Installation", "Repair")
- `description`: `string?` - Detailed job description
- `duration`: `number` (minutes) - Estimated job duration
- `location`: `GeoLocation` - Job site location with structured address
- `timezone`: `string` - IANA timezone identifier (e.g., "America/New_York") derived from location coordinates
- `requiredSkills`: `string[]` - Required skills/certifications (must match contractor)
- `serviceWindow`: `TimeWindow` - Preferred service time window
- `priority`: `"Normal" | "High" | "Rush"` - Job priority level
- `status`: `"Created" | "Assigned" | "InProgress" | "Completed" | "Cancelled"` - Job status
- `assignmentStatus`: `"Unassigned" | "Partially Assigned" | "Assigned"` - Computed assignment status
- `assignedContractors`: `ContractorSummary[]?` - List of assigned contractors with time slots
- `accessNotes`: `string?` - Access/parking notes
- `tools`: `string[]?` - Required tools
- `createdAt`: `string` (ISO8601) - Creation timestamp
- `desiredDate`: `string` (ISO8601) - Desired completion date
- `updatedAt`: `string` (ISO8601) - Last update timestamp

**TypeScript Interface:**

```typescript
export interface Job {
  id: string;
  type: string;
  description?: string; // Detailed job description
  duration: number; // Minutes
  location: GeoLocation;
  timezone: string; // IANA timezone (e.g., "America/New_York") - derived from location coordinates
  requiredSkills: string[]; // Must match contractor skills
  serviceWindow: TimeWindow;
  priority: "Normal" | "High" | "Rush";
  status: JobStatus;
  assignmentStatus: "Unassigned" | "Partially Assigned" | "Assigned"; // Computed
  assignedContractors?: ContractorSummary[]; // List of assigned contractors
  accessNotes?: string;
  tools?: string[];
  createdAt: string;
  desiredDate: string; // ISO8601 date
  updatedAt: string;
}

export interface TimeWindow {
  start: string; // ISO8601 datetime
  end: string; // ISO8601 datetime
}

export type JobStatus = 
  | "Created" 
  | "Assigned" 
  | "InProgress" 
  | "Completed" 
  | "Cancelled";
```

**Relationships:**
- One-to-many with `Assignment` (job can have one active assignment)
- Many-to-many with `Contractor` through `Assignment` (job assigned to one contractor)
- Required skills must be subset of contractor skills (hard compatibility rule)

### Assignment

**Purpose:** Links a Job to a Contractor with specific start/end times. Created when dispatcher confirms a booking recommendation.

**Key Attributes:**
- `id`: `string` (UUID) - Unique assignment identifier
- `jobId`: `string` (UUID) - Reference to Job
- `contractorId`: `string` (UUID) - Reference to Contractor
- `startUtc`: `string` (ISO8601) - Assignment start time (UTC)
- `endUtc`: `string` (ISO8601) - Assignment end time (UTC)
- `source`: `"auto" | "manual"` - Whether assigned automatically or manually overridden
- `auditId`: `string` (UUID) - Reference to AuditRecommendation
- `createdAt`: `string` (ISO8601) - Creation timestamp
- `status`: `"Pending" | "Confirmed" | "InProgress" | "Completed" | "Cancelled"`

**TypeScript Interface:**

```typescript
export interface Assignment {
  id: string;
  jobId: string;
  contractorId: string;
  startUtc: string; // ISO8601 datetime
  endUtc: string; // ISO8601 datetime
  source: "auto" | "manual";
  auditId: string; // Links to AuditRecommendation
  status: AssignmentStatus;
  createdAt: string;
  updatedAt: string;
}

export type AssignmentStatus = 
  | "Pending" 
  | "Confirmed" 
  | "InProgress" 
  | "Completed" 
  | "Cancelled";
```

**Relationships:**
- Many-to-one with `Job` (assignment belongs to one job)
- Many-to-one with `Contractor` (assignment belongs to one contractor)
- One-to-one with `AuditRecommendation` (assignment links to audit record)

### Recommendation

**Purpose:** Represents a ranked contractor recommendation with up to 3 suggested time slots. Returned by the recommendation API.

**Key Attributes:**
- `contractorId`: `string` (UUID) - Contractor identifier
- `contractorName`: `string` - Contractor name (for display)
- `score`: `number` - Overall recommendation score
- `scoreBreakdown`: `ScoreBreakdown` - Per-factor scores (availability, rating, distance, rotation)
- `rationale`: `string` - Human-readable explanation (≤200 chars)
- `suggestedSlots`: `TimeSlot[]` - Up to 3 suggested time slots (earliest, lowest-travel, highest-confidence)
- `distance`: `number` (meters) - Distance from contractor base to job site
- `eta`: `number` (minutes) - Estimated travel time

**TypeScript Interface:**

```typescript
export interface Recommendation {
  contractorId: string;
  contractorName: string;
  score: number; // 0-100
  scoreBreakdown: ScoreBreakdown;
  rationale: string; // ≤200 chars, deterministic template
  suggestedSlots: TimeSlot[]; // Up to 3 slots
  distance: number; // Meters
  eta: number; // Minutes
}

export interface ScoreBreakdown {
  availability: number; // 0-100
  rating: number; // 0-100
  distance: number; // 0-100 (normalized)
  rotation?: number; // Optional soft rotation boost
}

export interface TimeSlot {
  startUtc: string; // ISO8601 datetime
  endUtc: string; // ISO8601 datetime
  type: "earliest" | "lowest-travel" | "highest-confidence";
  confidence: number; // 0-100
}
```

**Relationships:**
- Generated from `Job` and `Contractor` entities
- Not persisted (ephemeral recommendation response)
- Links to `AuditRecommendation` when persisted for audit trail

### AuditRecommendation

**Purpose:** Audit trail record for recommendation requests. Persists request payload, candidate set, scores, and selection for 12-month retention.

**Key Attributes:**
- `id`: `string` (UUID) - Unique audit record identifier
- `jobId`: `string` (UUID) - Reference to Job
- `requestPayload`: `object` - Original recommendation request payload
- `candidates`: `CandidateScore[]` - All qualified contractors with scores
- `selectedContractorId`: `string?` (UUID) - Contractor selected (if assignment created)
- `selectionActor`: `string` - User ID who made selection (auto or dispatcher)
- `configVersion`: `number` - Scoring weights config version used
- `createdAt`: `string` (ISO8601) - Request timestamp

**TypeScript Interface:**

```typescript
export interface AuditRecommendation {
  id: string;
  jobId: string;
  requestPayload: RecommendationRequest;
  candidates: CandidateScore[];
  selectedContractorId?: string;
  selectionActor: string; // User ID (system or dispatcher)
  configVersion: number;
  createdAt: string;
}

export interface RecommendationRequest {
  jobId: string;
  desiredDate: string; // ISO8601 date
  serviceWindow?: TimeWindow;
  maxResults?: number; // Default 10
}

export interface CandidateScore {
  contractorId: string;
  finalScore: number;
  perFactorScores: ScoreBreakdown;
  rationale: string;
  wasSelected: boolean;
}
```

**Relationships:**
- Many-to-one with `Job` (audit record belongs to one job)
- One-to-one with `Assignment` (if assignment created from recommendation)
- Links to `WeightsConfig` via `configVersion`

### EventLog

**Purpose:** Audit log table for domain events. Events are published in-process to SignalR and logged here for audit trail. For MVP, events are published synchronously; background worker can be added post-MVP if needed.

**Key Attributes:**
- `id`: `string` (UUID) - Unique event log record identifier
- `eventType`: `string` - Domain event type (e.g., "JobCreated", "JobAssigned", "ScheduleUpdated", "ContractorRated")
- `payload`: `object` - Event payload (JSON)
- `publishedAt`: `string` (ISO8601) - Event publication timestamp
- `publishedTo`: `string[]` - List of SignalR groups/channels event was published to
- `createdAt`: `string` (ISO8601) - Event creation timestamp

**TypeScript Interface:**

```typescript
export interface EventLog {
  id: string;
  eventType: string; // Domain event type
  payload: object; // JSON event payload
  publishedAt: string; // ISO8601 datetime
  publishedTo: string[]; // SignalR groups/channels
  createdAt: string; // ISO8601 datetime
}
```

**Relationships:**
- Independent entity (not directly related to other entities)
- Events are published synchronously to SignalR in-process
- Logged for audit trail and future replay if needed
- Background worker and Outbox pattern can be added post-MVP for external integrations

### SystemConfiguration

**Purpose:** Stores system-wide configuration values such as available job types and skills. These are managed through the Settings page UI and used for dropdowns/validation across the application.

**Key Attributes:**
- `id`: `string` (UUID) - Unique configuration record identifier
- `type`: `"JobTypes" | "Skills"` - Configuration type
- `values`: `string[]` - Array of configuration values
- `updatedAt`: `string` (ISO8601) - Last update timestamp
- `updatedBy`: `string` - User ID who last updated

**TypeScript Interface:**

```typescript
export interface SystemConfiguration {
  id: string;
  type: "JobTypes" | "Skills";
  values: string[];
  updatedAt: string;
  updatedBy: string;
}
```

**Default Values:**
- **Job Types:** "Hardwood Installation", "Tile Installation", "Carpet Installation", "Laminate Installation", "HVAC Repair", "Electrical Inspection", "Repair/Maintenance"
- **Skills:** "Hardwood Installation", "Tile", "Laminate", "Carpet", "Finishing", "HVAC", "Electrical", "Plumbing"

**Relationships:**
- Referenced by `Job.type` (job type must exist in JobTypes config)
- Referenced by `Job.requiredSkills` and `Contractor.skills` (skills must exist in Skills config)

### Activity

**Purpose:** User-friendly representation of system events for the dashboard activity feed. Transformed from `EventLog` records for display purposes.

**Key Attributes:**
- `id`: `string` (UUID) - Unique activity identifier (from EventLog)
- `type`: `"assignment" | "completion" | "cancellation" | "contractor_added" | "job_created"` - Activity type
- `title`: `string` - Short activity title (e.g., "Job Assigned")
- `description`: `string` - Human-readable description (e.g., "Hardwood Installation assigned to John Martinez")
- `timestamp`: `string` (ISO8601) - Activity timestamp
- `metadata`: `object` - Additional context (jobId, contractorId, actorId)

**TypeScript Interface:**

```typescript
export interface Activity {
  id: string;
  type: "assignment" | "completion" | "cancellation" | "contractor_added" | "job_created";
  title: string;
  description: string;
  timestamp: string;
  metadata?: {
    jobId?: string;
    contractorId?: string;
    actorId?: string;
  };
}
```

**Relationships:**
- Derived from `EventLog` table
- Not persisted separately (generated on-demand from EventLog)

### DashboardStatistics

**Purpose:** Aggregated metrics for dashboard overview. Provides key performance indicators and system health metrics.

**Key Attributes:**
- `activeContractors`: `StatMetric` - Count of contractors currently available or busy
- `pendingJobs`: `JobStatMetric` - Count of pending jobs with unassigned breakdown
- `avgAssignmentTime`: `TimeMetric` - Average time from job creation to first assignment
- `utilizationRate`: `PercentMetric` - Percentage of contractor capacity currently utilized

**TypeScript Interface:**

```typescript
export interface DashboardStatistics {
  activeContractors: StatMetric;
  pendingJobs: JobStatMetric;
  avgAssignmentTime: TimeMetric;
  utilizationRate: PercentMetric;
}

export interface StatMetric {
  value: number;
  change: string; // e.g., "+2 today", "-3 this week"
}

export interface JobStatMetric extends StatMetric {
  unassignedCount: number;
}

export interface TimeMetric {
  value: number; // Minutes
  unit: string; // "minutes", "hours"
  changePercent: number;
  changePeriod: string; // "this week", "this month"
}

export interface PercentMetric {
  value: number; // 0-100
  changePercent: number;
  changePeriod: string;
}
```

**Relationships:**
- Computed from `Contractor`, `Job`, `Assignment`, and `EventLog` tables
- Not persisted (calculated on-demand with caching)

### Enhanced Models (Computed Fields)

**The following fields are added to existing models as computed values:**

#### Contractor Additions:

```typescript
export interface Contractor {
  // ... existing fields ...
  
  // ✅ NEW COMPUTED FIELDS:
  availability: "Available" | "Busy" | "Off Duty"; // Based on current schedule
  jobsToday: number;                                // Count of assignments today
  maxJobsPerDay: number;                            // Configurable limit (default 4)
  currentUtilization: number;                       // Percentage 0-100
  timezone: string;                                 // IANA timezone (e.g., "America/New_York")
}
```

#### Job Additions:

```typescript
export interface Job {
  // ... existing fields ...
  
  // ✅ NEW COMPUTED FIELDS:
  assignmentStatus: "Unassigned" | "Partially Assigned" | "Assigned";
  assignedContractors?: ContractorSummary[]; // List of assigned contractors
  description?: string;                      // Detailed job description
}

export interface ContractorSummary {
  id: string;
  name: string;
  startUtc: string;
  endUtc: string;
}
```

#### GeoLocation Enhancements:

```typescript
export interface GeoLocation {
  latitude: number;
  longitude: number;
  
  // ✅ STRUCTURED ADDRESS (from Google Places API):
  address: string;            // Street address (from Place Details)
  city: string;               // City name
  state: string;              // State/province code (e.g., "NY", "CA")
  postalCode?: string;        // Postal/ZIP code
  country?: string;           // Country code (default "US")
  formattedAddress: string;   // Full formatted address from Google Places
  placeId?: string;           // Google Places API place_id (for caching/reference)
}
```
