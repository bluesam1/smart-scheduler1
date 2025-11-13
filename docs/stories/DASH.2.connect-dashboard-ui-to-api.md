# Story DASH.2: Connect Dashboard UI to API

## Status
Approved

## Story

**As a** dispatcher,
**I want** the dashboard to use real API data,
**so that** I can see actual system statistics.

## Acceptance Criteria

1. **Statistics Integration:** StatsOverview calls GET /dashboard/stats
2. **Replace Mock:** All mock statistics replaced with API calls
3. **Loading States:** Loading indicators during API calls
4. **Error Handling:** Proper error messages
5. **Auto-refresh:** Statistics auto-refresh periodically
6. **Real-time Updates:** Statistics update on relevant events (optional)
7. **Type Safety:** TypeScript types match API
8. **Performance:** UI remains responsive
9. **User Experience:** Smooth data loading
10. **Caching:** Frontend respects cache headers

## Tasks / Subtasks

- [ ] Update StatsOverview component
  - [ ] Replace mock data with API call
  - [ ] Call GET /dashboard/stats
  - [ ] Display real statistics
- [ ] Add loading states
  - [ ] Show loading indicator
  - [ ] Show skeleton UI during load
- [ ] Add error handling
  - [ ] Display API errors
  - [ ] Handle network failures
- [ ] Implement auto-refresh
  - [ ] Refresh statistics every 5 minutes
  - [ ] Respect cache headers
- [ ] Add real-time updates (optional)
  - [ ] Update on relevant events
  - [ ] Refresh on job/contractor changes
- [ ] Update statistics display
  - [ ] Show all metrics
  - [ ] Show change indicators
  - [ ] Format numbers appropriately

## Dev Notes

### Relevant Source Tree Info
- Dashboard component: `frontend/components/stats-overview.tsx`
- Dashboard page: `frontend/app/page.tsx`
- API client: `frontend/lib/api/generated/api-client.ts`

### Architecture References
- **API Specification:** See `docs/architecture/api-specification.md` for dashboard stats
- **UI Components:** Frontend components exist - see `frontend/components/stats-overview.tsx`

### Testing Standards
- **Test Location:** `frontend/__tests__/`
- **Component Tests:** Test API integration, loading, error handling
- **E2E Tests:** Playwright tests for dashboard

### UI Integration Notes
- **Existing UI:** Dashboard UI is 95% complete with mock data
- **Component:** `frontend/components/stats-overview.tsx` uses mock data
- **Mock Data:** Currently shows fake statistics - needs API replacement

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

### Review Date: 2025-01-27

### Reviewed By: Quinn (Test Architect)

### Code Quality Assessment

The frontend integration is well-implemented with proper React patterns, error handling, and user experience considerations. The component properly uses hooks, handles loading/error states, and implements auto-refresh. Real-time updates are marked optional and not implemented, which is acceptable for MVP.

**Strengths:**
- Clean React component with proper hooks usage
- Comprehensive error handling with user-friendly messages
- Loading states with skeleton UI
- Auto-refresh implemented (5 minutes)
- Proper TypeScript typing
- Good user experience with smooth data loading

**Areas for Improvement:**
- Missing component tests
- Real-time updates not implemented (optional AC)
- Cache header handling could be more explicit

### Refactoring Performed

No refactoring performed during this review. Code quality is good.

### Compliance Check

- Coding Standards: ✓ Follows React/TypeScript conventions
- Project Structure: ✓ Component in correct location
- Testing Strategy: ✗ No component tests added
- All ACs Met: ✓ All required ACs met (real-time updates optional)

### Improvements Checklist

- [ ] Add React Testing Library tests for StatsOverview component
- [ ] Test API integration, loading states, error handling
- [ ] Consider implementing SignalR real-time updates (future enhancement)
- [ ] Add explicit cache header handling if needed

### Security Review

✓ **PASS** - Uses authenticated fetch with automatic token refresh. No security concerns. Properly handles 401 responses and redirects to login when needed.

### Performance Considerations

✓ **PASS** - Auto-refresh every 5 minutes is reasonable and respects backend caching. UI remains responsive during data loading. No performance concerns.

### Files Modified During Review

None - no files modified during QA review.

### Gate Status

Gate: **CONCERNS** → `docs/qa/gates/DASH.2-connect-dashboard-ui-to-api.yml`

**Key Issues:**
- Missing component tests (medium)
- Real-time updates not implemented (low - optional)
- Cache header handling (low)

### Recommended Status

✓ **Ready for Review** - Implementation is functionally complete and provides good user experience. Missing tests should be added before production, but acceptable for MVP review.

