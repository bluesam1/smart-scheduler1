# Story DASH.3: Activity Feed API

## Status
Approved

## Story

**As a** dispatcher,
**I want** an activity feed API,
**so that** I can see recent system events on the dashboard.

## Acceptance Criteria

1. **GET /activity:** Endpoint returns recent activities
2. **Activity Types:** Support assignment, completion, cancellation, contractor_added, job_created
3. **Filtering:** Filter by activity types
4. **Limit:** Limit results (default 20, max 100)
5. **Event Transformation:** Transform EventLog records to Activity DTOs
6. **Performance:** Efficient querying
7. **Authorization:** Dispatcher and Admin can access
8. **OpenAPI:** Endpoint documented
9. **Sorting:** Activities sorted by timestamp (newest first)
10. **Metadata:** Activities include relevant metadata

## Tasks / Subtasks

- [ ] Create Activity DTOs
  - [ ] Create `ActivityDto`
  - [ ] Include type, title, description, timestamp, metadata
- [ ] Implement activity query
  - [ ] Create `GetActivitiesQuery` with MediatR
  - [ ] Support type filtering
  - [ ] Support limit parameter
- [ ] Implement event transformation
  - [ ] Transform EventLog to Activity
  - [ ] Generate human-readable titles/descriptions
  - [ ] Extract metadata
- [ ] Implement activity service
  - [ ] Query EventLog table
  - [ ] Filter by types
  - [ ] Transform to activities
  - [ ] Sort by timestamp
- [ ] Create API endpoint
  - [ ] GET /activity endpoint
  - [ ] Support ?types= and ?limit= params
  - [ ] Return activities
- [ ] Optimize performance
  - [ ] Efficient EventLog queries
  - [ ] Index on event type and timestamp
- [ ] Add authorization
  - [ ] Apply Dispatcher/Admin policy

## Dev Notes

### Relevant Source Tree Info
- API endpoint: `src/SmartScheduler.Api/Endpoints/Activity/ActivityEndpoint.cs`
- Query: `src/SmartScheduler.Application/Activity/Queries/GetActivitiesQuery.cs`
- Transformation: `src/SmartScheduler.Application/Activity/Services/ActivityTransformer.cs`

### Architecture References
- **API Specification:** See `docs/architecture/api-specification.md` for /activity endpoint
- **Data Models:** See `docs/architecture/data-models.md` for Activity model
- **EventLog:** Activities derived from EventLog - see architecture

### Testing Standards
- **Test Location:** `src/SmartScheduler.Api.Tests/`
- **API Tests:** Test endpoint, filtering, transformation
- **Performance Tests:** Test query performance

### UI Integration Notes
- Frontend has `ActivityFeed` component (see `frontend/components/activity-feed.tsx`)
- Frontend uses mock data - needs API integration
- UI is 95% complete - needs API connection

## Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2025-01-XX | 1.0 | Initial story created | Sarah (PO) |

## Dev Agent Record

### Agent Model Used
_To be populated by dev agent_

### Debug Log References
_To be populated by dev agent_

### Completion Notes List
_To be populated by dev agent_

### File List
_To be populated by dev agent_

## QA Results
_To be populated by QA agent_

