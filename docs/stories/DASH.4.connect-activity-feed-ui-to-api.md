# Story DASH.4: Connect Activity Feed UI to API

## Status
Approved

## Story

**As a** dispatcher,
**I want** the activity feed to use real API data,
**so that** I can see actual system events.

## Acceptance Criteria

1. **Activity Integration:** ActivityFeed calls GET /activity
2. **Replace Mock:** All mock activity data replaced with API calls
3. **Loading States:** Loading indicators during API calls
4. **Error Handling:** Proper error messages
5. **Auto-refresh:** Activity feed auto-refreshes periodically
6. **Real-time Updates:** Activities update via SignalR on events
7. **Type Safety:** TypeScript types match API
8. **Performance:** UI remains responsive
9. **User Experience:** Smooth activity loading
10. **Filtering:** Activity type filtering works

## Tasks / Subtasks

- [ ] Update ActivityFeed component
  - [ ] Replace mock data with API call
  - [ ] Call GET /activity
  - [ ] Display real activities
- [ ] Add loading states
  - [ ] Show loading indicator
  - [ ] Show skeleton UI during load
- [ ] Add error handling
  - [ ] Display API errors
  - [ ] Handle network failures
- [ ] Implement auto-refresh
  - [ ] Refresh activities every 30 seconds
  - [ ] Show new activities
- [ ] Add real-time updates
  - [ ] Listen for domain events via SignalR
  - [ ] Add new activities on events
  - [ ] Update feed in real-time
- [ ] Implement filtering
  - [ ] Filter by activity types
  - [ ] Update API call with filters
- [ ] Update activity display
  - [ ] Show all activity fields
  - [ ] Format timestamps
  - [ ] Show metadata

## Dev Notes

### Relevant Source Tree Info
- Activity feed component: `frontend/components/activity-feed.tsx`
- Dashboard page: `frontend/app/page.tsx`
- API client: `frontend/lib/api/generated/api-client.ts`

### Architecture References
- **API Specification:** See `docs/architecture/api-specification.md` for activity endpoint
- **UI Components:** Frontend components exist - see `frontend/components/activity-feed.tsx`
- **SignalR:** Real-time updates via SignalR - see architecture

### Testing Standards
- **Test Location:** `frontend/__tests__/`
- **Component Tests:** Test API integration, real-time updates
- **E2E Tests:** Playwright tests for activity feed

### UI Integration Notes
- **Existing UI:** Activity feed UI is 95% complete with mock data
- **Component:** `frontend/components/activity-feed.tsx` uses mock data
- **Mock Data:** Currently shows fake activities - needs API replacement

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

