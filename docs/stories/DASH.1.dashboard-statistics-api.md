# Story DASH.1: Dashboard Statistics API

## Status
Approved

## Story

**As a** dispatcher,
**I want** dashboard statistics via API,
**so that** I can see key metrics on the dashboard.

## Acceptance Criteria

1. **GET /dashboard/stats:** Endpoint returns dashboard statistics
2. **Active Contractors:** Count of active contractors with change indicator
3. **Pending Jobs:** Count of pending jobs with unassigned breakdown
4. **Average Assignment Time:** Average time from job creation to assignment
5. **Utilization Rate:** Contractor utilization percentage
6. **Caching:** Statistics cached for 5 minutes
7. **Performance:** p95 < 500ms target
8. **Change Indicators:** Change indicators (e.g., "+2 today", "-3 this week")
9. **Authorization:** Dispatcher and Admin can access
10. **OpenAPI:** Endpoint documented

## Tasks / Subtasks

- [ ] Create statistics DTOs
  - [ ] Create `DashboardStatisticsDto`
  - [ ] Create `StatMetric`, `JobStatMetric`, `TimeMetric`, `PercentMetric` DTOs
- [ ] Implement statistics query
  - [ ] Create `GetDashboardStatisticsQuery` with MediatR
  - [ ] Create query handler
- [ ] Implement statistics calculation
  - [ ] Calculate active contractors count
  - [ ] Calculate pending jobs count
  - [ ] Calculate average assignment time
  - [ ] Calculate utilization rate
- [ ] Implement change indicators
  - [ ] Calculate changes (today, this week)
  - [ ] Format change strings
  - [ ] Include in response
- [ ] Add caching
  - [ ] Cache statistics for 5 minutes
  - [ ] Use in-memory cache
  - [ ] Invalidate on relevant changes
- [ ] Create API endpoint
  - [ ] GET /dashboard/stats endpoint
  - [ ] Return statistics
- [ ] Optimize performance
  - [ ] Optimize database queries
  - [ ] Use efficient aggregations
  - [ ] Verify p95 < 500ms
- [ ] Add authorization
  - [ ] Apply Dispatcher/Admin policy

## Dev Notes

### Relevant Source Tree Info
- API endpoint: `src/SmartScheduler.Api/Endpoints/Dashboard/StatsEndpoint.cs`
- Query: `src/SmartScheduler.Application/Dashboard/Queries/GetDashboardStatisticsQuery.cs`
- DTOs: `src/SmartScheduler.Application/DTOs/DashboardStatistics.cs`

### Architecture References
- **API Specification:** See `docs/architecture/api-specification.md` for /dashboard/stats endpoint
- **Data Models:** See `docs/architecture/data-models.md` for DashboardStatistics model
- **Performance:** p95 < 500ms with caching - see architecture

### Testing Standards
- **Test Location:** `src/SmartScheduler.Api.Tests/`
- **API Tests:** Test endpoint, statistics calculation, caching
- **Performance Tests:** Verify p95 < 500ms

### UI Integration Notes
- Frontend has `StatsOverview` component (see `frontend/components/stats-overview.tsx`)
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

