# API Specification

Based on the PRD requirements, the API uses REST style with Minimal APIs pattern in .NET 8. All endpoints require JWT authentication via Amazon Cognito.

### REST API Specification

```yaml
openapi: 3.0.0
info:
  title: SmartScheduler API
  version: 1.0.0
  description: REST API for SmartScheduler contractor matching and job assignment system
servers:
  - url: https://api.smartscheduler.example.com
    description: Production API
  - url: https://api-staging.smartscheduler.example.com
    description: Staging API
  - url: http://localhost:5004
    description: Local development

security:
  - BearerAuth: []

paths:
  /contractors:
    get:
      summary: List contractors
      description: Get list of contractors with optional filtering
      tags: [Contractors]
      parameters:
        - name: skills
          in: query
          schema:
            type: array
            items:
              type: string
          description: Filter by skills/certifications
        - name: limit
          in: query
          schema:
            type: integer
            default: 50
          description: Maximum number of results
      responses:
        '200':
          description: List of contractors
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Contractor'
    post:
      summary: Create contractor
      description: Create a new contractor profile
      tags: [Contractors]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateContractorRequest'
      responses:
        '201':
          description: Contractor created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Contractor'
        '400':
          $ref: '#/components/responses/BadRequest'

  /contractors/{id}:
    get:
      summary: Get contractor by ID
      description: Get contractor details by ID
      tags: [Contractors]
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Contractor details
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Contractor'
        '404':
          $ref: '#/components/responses/NotFound'
    put:
      summary: Update contractor
      description: Update contractor profile
      tags: [Contractors]
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/UpdateContractorRequest'
      responses:
        '200':
          description: Contractor updated
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Contractor'
        '404':
          $ref: '#/components/responses/NotFound'

  /jobs:
    get:
      summary: List jobs
      description: Get list of jobs with optional filtering
      tags: [Jobs]
      parameters:
        - name: status
          in: query
          schema:
            type: string
            enum: [Created, Assigned, InProgress, Completed, Cancelled]
          description: Filter by job status
        - name: priority
          in: query
          schema:
            type: string
            enum: [Normal, High, Rush]
          description: Filter by priority
      responses:
        '200':
          description: List of jobs
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Job'
    post:
      summary: Create job
      description: Create a new job
      tags: [Jobs]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateJobRequest'
      responses:
        '201':
          description: Job created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Job'
        '400':
          $ref: '#/components/responses/BadRequest'

  /jobs/{id}:
    get:
      summary: Get job by ID
      description: Get job details by ID
      tags: [Jobs]
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Job details
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Job'
        '404':
          $ref: '#/components/responses/NotFound'

  /recommendations:
    post:
      summary: Get contractor recommendations
      description: Get ranked contractor recommendations for a job with up to 3 suggested time slots per contractor
      tags: [Recommendations]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/RecommendationRequest'
      responses:
        '200':
          description: Ranked contractor recommendations
          content:
            application/json:
              schema:
                type: object
                properties:
                  requestId:
                    type: string
                    format: uuid
                  jobId:
                    type: string
                    format: uuid
                  recommendations:
                    type: array
                    items:
                      $ref: '#/components/schemas/Recommendation'
                  configVersion:
                    type: integer
                  generatedAt:
                    type: string
                    format: date-time
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/NotFound'
      x-performance-target:
        p95: 500ms

  /jobs/{id}/assign:
    post:
      summary: Assign job to contractor
      description: Assign a job to a contractor with re-validation of availability
      tags: [Jobs]
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [contractorId, startUtc, endUtc]
              properties:
                contractorId:
                  type: string
                  format: uuid
                startUtc:
                  type: string
                  format: date-time
                endUtc:
                  type: string
                  format: date-time
      responses:
        '201':
          description: Job assigned successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Assignment'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/NotFound'
        '409':
          description: Conflict - job already assigned or contractor unavailable
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

  /health:
    get:
      summary: Health check
      description: API health check endpoint
      tags: [System]
      responses:
        '200':
          description: API is healthy
          content:
            application/json:
              schema:
                type: object
                properties:
                  status:
                    type: string
                    example: healthy
                  timestamp:
                    type: string
                    format: date-time

  /settings/job-types:
    get:
      summary: List available job types
      description: Get list of job types available for job creation
      tags: [Settings]
      responses:
        '200':
          description: List of job types
          content:
            application/json:
              schema:
                type: object
                properties:
                  jobTypes:
                    type: array
                    items:
                      type: string
                    example: ["Hardwood Installation", "Tile Installation", "Carpet Installation"]
    post:
      summary: Add new job type
      description: Add a new job type to the system
      tags: [Settings]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [jobType]
              properties:
                jobType:
                  type: string
                  example: "Vinyl Installation"
      responses:
        '201':
          description: Job type added successfully
          content:
            application/json:
              schema:
                type: object
                properties:
                  jobType:
                    type: string
        '400':
          $ref: '#/components/responses/BadRequest'
        '409':
          description: Job type already exists
    put:
      summary: Update job type
      description: Rename an existing job type
      tags: [Settings]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [oldValue, newValue]
              properties:
                oldValue:
                  type: string
                newValue:
                  type: string
      responses:
        '200':
          description: Job type updated
        '404':
          $ref: '#/components/responses/NotFound'
    delete:
      summary: Remove job type
      description: Remove a job type from the system
      tags: [Settings]
      parameters:
        - name: jobType
          in: query
          required: true
          schema:
            type: string
      responses:
        '204':
          description: Job type removed
        '404':
          $ref: '#/components/responses/NotFound'
        '409':
          description: Job type is in use and cannot be deleted

  /settings/skills:
    get:
      summary: List available skills
      description: Get list of skills available for contractors and job requirements
      tags: [Settings]
      responses:
        '200':
          description: List of skills
          content:
            application/json:
              schema:
                type: object
                properties:
                  skills:
                    type: array
                    items:
                      type: string
                    example: ["Hardwood Installation", "Tile", "Carpet", "Finishing"]
    post:
      summary: Add new skill
      description: Add a new skill to the system
      tags: [Settings]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [skill]
              properties:
                skill:
                  type: string
                  example: "Stone Installation"
      responses:
        '201':
          description: Skill added successfully
        '409':
          description: Skill already exists
    put:
      summary: Update skill
      description: Rename an existing skill
      tags: [Settings]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [oldValue, newValue]
              properties:
                oldValue:
                  type: string
                newValue:
                  type: string
      responses:
        '200':
          description: Skill updated
        '404':
          $ref: '#/components/responses/NotFound'
    delete:
      summary: Remove skill
      description: Remove a skill from the system
      tags: [Settings]
      parameters:
        - name: skill
          in: query
          required: true
          schema:
            type: string
      responses:
        '204':
          description: Skill removed
        '409':
          description: Skill is in use and cannot be deleted

  /activity:
    get:
      summary: Get recent activity feed
      description: Returns recent system events for dashboard display (assignments, completions, etc.)
      tags: [Dashboard]
      parameters:
        - name: limit
          in: query
          schema:
            type: integer
            default: 20
            maximum: 100
          description: Maximum number of activities to return
        - name: types
          in: query
          schema:
            type: array
            items:
              type: string
              enum: [assignment, completion, cancellation, contractor_added, job_created]
          description: Filter by activity types
      responses:
        '200':
          description: List of recent activities
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Activity'

  /dashboard/stats:
    get:
      summary: Get dashboard statistics
      description: Returns key metrics for dashboard overview (active contractors, pending jobs, utilization, etc.)
      tags: [Dashboard]
      responses:
        '200':
          description: Dashboard statistics
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DashboardStatistics'

components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: JWT token from Amazon Cognito

  schemas:
    Contractor:
      type: object
      required: [id, name, baseLocation, rating, workingHours, skills, availability, jobsToday, maxJobsPerDay, currentUtilization, timezone]
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string
        baseLocation:
          $ref: '#/components/schemas/GeoLocation'
        rating:
          type: number
          minimum: 0
          maximum: 100
        workingHours:
          type: array
          items:
            $ref: '#/components/schemas/WorkingHours'
        skills:
          type: array
          items:
            type: string
        calendar:
          $ref: '#/components/schemas/ContractorCalendar'
        availability:
          type: string
          enum: [Available, Busy, "Off Duty"]
          description: Computed based on current schedule and working hours
        jobsToday:
          type: integer
          description: Count of assignments for today
        maxJobsPerDay:
          type: integer
          description: Maximum jobs per day (default 4)
          default: 4
        currentUtilization:
          type: number
          minimum: 0
          maximum: 100
          description: Percentage of available hours utilized today
        timezone:
          type: string
          description: IANA timezone (e.g., "America/New_York")
          example: "America/New_York"
        createdAt:
          type: string
          format: date-time
        updatedAt:
          type: string
          format: date-time

    CreateContractorRequest:
      type: object
      required: [name, baseLocation, workingHours, skills]
      properties:
        name:
          type: string
        baseLocation:
          $ref: '#/components/schemas/GeoLocation'
        workingHours:
          type: array
          items:
            $ref: '#/components/schemas/WorkingHours'
        skills:
          type: array
          items:
            type: string
        calendar:
          $ref: '#/components/schemas/ContractorCalendar'

    UpdateContractorRequest:
      type: object
      properties:
        name:
          type: string
        baseLocation:
          $ref: '#/components/schemas/GeoLocation'
        workingHours:
          type: array
          items:
            $ref: '#/components/schemas/WorkingHours'
        skills:
          type: array
          items:
            type: string
        calendar:
          $ref: '#/components/schemas/ContractorCalendar'

    Job:
      type: object
      required: [id, type, duration, location, timezone, requiredSkills, serviceWindow, priority, status, assignmentStatus]
      properties:
        id:
          type: string
          format: uuid
        type:
          type: string
        description:
          type: string
          description: Detailed job description
        duration:
          type: integer
          description: Duration in minutes
        location:
          $ref: '#/components/schemas/GeoLocation'
        timezone:
          type: string
          description: IANA timezone identifier derived from location coordinates (e.g., "America/New_York")
          example: "America/New_York"
        requiredSkills:
          type: array
          items:
            type: string
        serviceWindow:
          $ref: '#/components/schemas/TimeWindow'
        priority:
          type: string
          enum: [Normal, High, Rush]
        status:
          type: string
          enum: [Created, Assigned, InProgress, Completed, Cancelled]
        assignmentStatus:
          type: string
          enum: [Unassigned, "Partially Assigned", Assigned]
          description: Computed based on Assignment records
        assignedContractors:
          type: array
          items:
            $ref: '#/components/schemas/ContractorSummary'
          description: List of contractors assigned to this job
        accessNotes:
          type: string
        tools:
          type: array
          items:
            type: string
        createdAt:
          type: string
          format: date-time
        desiredDate:
          type: string
          format: date
        updatedAt:
          type: string
          format: date-time

    CreateJobRequest:
      type: object
      required: [type, duration, location, requiredSkills, serviceWindow, priority, desiredDate]
      properties:
        type:
          type: string
        duration:
          type: integer
        location:
          $ref: '#/components/schemas/GeoLocation'
        requiredSkills:
          type: array
          items:
            type: string
        serviceWindow:
          $ref: '#/components/schemas/TimeWindow'
        priority:
          type: string
          enum: [Normal, High, Rush]
        accessNotes:
          type: string
        tools:
          type: array
          items:
            type: string
        desiredDate:
          type: string
          format: date

    RecommendationRequest:
      type: object
      required: [jobId, desiredDate]
      properties:
        jobId:
          type: string
          format: uuid
        desiredDate:
          type: string
          format: date
        serviceWindow:
          $ref: '#/components/schemas/TimeWindow'
        maxResults:
          type: integer
          default: 10
          maximum: 50

    Recommendation:
      type: object
      required: [contractorId, contractorName, score, scoreBreakdown, rationale, suggestedSlots, distance, eta]
      properties:
        contractorId:
          type: string
          format: uuid
        contractorName:
          type: string
        score:
          type: number
          minimum: 0
          maximum: 100
        scoreBreakdown:
          $ref: '#/components/schemas/ScoreBreakdown'
        rationale:
          type: string
          maxLength: 200
        suggestedSlots:
          type: array
          maxItems: 3
          items:
            $ref: '#/components/schemas/TimeSlot'
        distance:
          type: number
          description: Distance in meters
        eta:
          type: number
          description: Estimated travel time in minutes

    Assignment:
      type: object
      required: [id, jobId, contractorId, startUtc, endUtc, source, status]
      properties:
        id:
          type: string
          format: uuid
        jobId:
          type: string
          format: uuid
        contractorId:
          type: string
          format: uuid
        startUtc:
          type: string
          format: date-time
        endUtc:
          type: string
          format: date-time
        source:
          type: string
          enum: [auto, manual]
        auditId:
          type: string
          format: uuid
        status:
          type: string
          enum: [Pending, Confirmed, InProgress, Completed, Cancelled]
        createdAt:
          type: string
          format: date-time
        updatedAt:
          type: string
          format: date-time

    GeoLocation:
      type: object
      required: [latitude, longitude, address, city, state, formattedAddress]
      properties:
        latitude:
          type: number
          format: double
        longitude:
          type: number
          format: double
        address:
          type: string
          description: Street address (from Google Places API)
        city:
          type: string
          description: City name
        state:
          type: string
          description: State/province code (e.g., "NY", "CA")
        postalCode:
          type: string
          description: Postal/ZIP code
        country:
          type: string
          description: Country code (default "US")
          default: "US"
        formattedAddress:
          type: string
          description: Full formatted address from Google Places API
        placeId:
          type: string
          description: Google Places API place_id (for caching/reference)

    WorkingHours:
      type: object
      required: [dayOfWeek, startTime, endTime, timeZone]
      properties:
        dayOfWeek:
          type: integer
          minimum: 0
          maximum: 6
          description: 0=Sunday, 6=Saturday
        startTime:
          type: string
          pattern: '^([0-1][0-9]|2[0-3]):[0-5][0-9]$'
        endTime:
          type: string
          pattern: '^([0-1][0-9]|2[0-3]):[0-5][0-9]$'
        timeZone:
          type: string
          description: IANA timezone (e.g., "America/New_York")

    ContractorCalendar:
      type: object
      properties:
        holidays:
          type: array
          items:
            type: string
            format: date
        exceptions:
          type: array
          items:
            $ref: '#/components/schemas/CalendarException'
        dailyBreakMinutes:
          type: integer
          default: 30

    CalendarException:
      type: object
      required: [date, type]
      properties:
        date:
          type: string
          format: date
        type:
          type: string
          enum: [holiday, override]
        workingHours:
          $ref: '#/components/schemas/WorkingHours'

    TimeWindow:
      type: object
      required: [start, end]
      properties:
        start:
          type: string
          format: date-time
        end:
          type: string
          format: date-time

    TimeSlot:
      type: object
      required: [startUtc, endUtc, type, confidence]
      properties:
        startUtc:
          type: string
          format: date-time
        endUtc:
          type: string
          format: date-time
        type:
          type: string
          enum: [earliest, lowest-travel, highest-confidence]
        confidence:
          type: number
          minimum: 0
          maximum: 100

    ScoreBreakdown:
      type: object
      required: [availability, rating, distance]
      properties:
        availability:
          type: number
          minimum: 0
          maximum: 100
        rating:
          type: number
          minimum: 0
          maximum: 100
        distance:
          type: number
          minimum: 0
          maximum: 100
        rotation:
          type: number
          description: Optional soft rotation boost

    Activity:
      type: object
      required: [id, type, title, description, timestamp]
      properties:
        id:
          type: string
          format: uuid
        type:
          type: string
          enum: [assignment, completion, cancellation, contractor_added, job_created]
        title:
          type: string
          example: "Job Assigned"
        description:
          type: string
          example: "Hardwood Installation assigned to John Martinez"
        timestamp:
          type: string
          format: date-time
        metadata:
          type: object
          properties:
            jobId:
              type: string
              format: uuid
            contractorId:
              type: string
              format: uuid
            actorId:
              type: string

    DashboardStatistics:
      type: object
      required: [activeContractors, pendingJobs, avgAssignmentTime, utilizationRate]
      properties:
        activeContractors:
          type: object
          properties:
            value:
              type: integer
              example: 24
            change:
              type: string
              example: "+2 today"
        pendingJobs:
          type: object
          properties:
            value:
              type: integer
              example: 8
            unassignedCount:
              type: integer
              example: 3
        avgAssignmentTime:
          type: object
          properties:
            value:
              type: number
              description: Time in minutes
              example: 4.2
            unit:
              type: string
              example: "minutes"
            changePercent:
              type: number
              example: -20
            changePeriod:
              type: string
              example: "this week"
        utilizationRate:
          type: object
          properties:
            value:
              type: number
              description: Percentage 0-100
              example: 76
            changePercent:
              type: number
              example: 5
            changePeriod:
              type: string
              example: "this week"

    ContractorSummary:
      type: object
      required: [id, name, startUtc, endUtc]
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string
        startUtc:
          type: string
          format: date-time
        endUtc:
          type: string
          format: date-time

    Error:
      type: object
      required: [error]
      properties:
        error:
          type: object
          required: [code, message]
          properties:
            code:
              type: string
            message:
              type: string
            details:
              type: object
            timestamp:
              type: string
              format: date-time
            requestId:
              type: string
              format: uuid

  responses:
    BadRequest:
      description: Bad request
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Error'
    NotFound:
      description: Resource not found
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Error'
```

**Authentication:**
- All endpoints require JWT Bearer token from Amazon Cognito
- Token must include `groups` claim with role (Admin, Dispatcher, Contractor)
- Role-based authorization enforced via policy-based authorization in .NET

**Performance Requirements:**
- `/recommendations` endpoint must achieve p95 < 500ms (per PRD)
- `/dashboard/stats` endpoint target p95 < 500ms (with 5-minute caching)
- All other endpoints target p95 < 200ms

**Error Handling:**
- Standard error response format with code, message, details, timestamp, and requestId
- 400 for validation errors
- 401 for authentication failures
- 403 for authorization failures
- 404 for not found
- 409 for conflicts (e.g., concurrent assignment attempts)
- 500 for server errors

**Address Validation Integration:**

The system integrates with Google Places Autocomplete (New) for address validation:

1. **Frontend Integration:**
   - Frontend uses Google Places Autocomplete widget for address input
   - User selects address from autocomplete suggestions
   - On selection, frontend calls Place Details API to get structured address components

2. **Backend Processing:**
   - Backend receives structured address from frontend (or Place Details API response)
   - Validates address components are complete
   - Looks up timezone from lat/long coordinates using timezone API/library
   - Stores structured address + coordinates + timezone in database

3. **Caching Strategy:**
   - Cache Place Details results by `place_id` to minimize API calls
   - Cache timezone lookups by lat/long (timezone doesn't change)
   - Use in-memory cache (IMemoryCache) for MVP

4. **Error Handling:**
   - If Google Places API unavailable, allow manual address entry
   - If timezone lookup fails, use default timezone or contractor's timezone
   - Log errors for monitoring but don't block job creation

5. **Cost Optimization:**
   - Autocomplete sessions are no-charge (only user interaction)
   - Only Place Details/Address Validation calls are charged
   - Cache aggressively to reduce API calls
   - Consider batch processing for bulk imports

**Timezone Lookup:**

- Use timezone API or library (e.g., TimeZoneMapper, Google Time Zone API) to derive IANA timezone from lat/long
- Store timezone on Job entity for accurate time calculations
- Use timezone for service window calculations and availability matching
- Default to contractor's timezone if job timezone lookup fails

