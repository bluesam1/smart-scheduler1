# Story FE.1: Replace All Mock Data with API Calls

## Status
Approved

## Story

**As a** developer,
**I want** all mock data replaced with real API calls,
**so that** the frontend uses real backend data throughout.

## Acceptance Criteria

1. **Mock Data Audit:** All mock data identified
2. **API Integration:** All mock data replaced with API calls
3. **Type Safety:** All API calls use generated TypeScript types
4. **Error Handling:** Consistent error handling across all API calls
5. **Loading States:** Loading states added where missing
6. **No Mock Remnants:** No mock data remaining in codebase
7. **Consistent Patterns:** Consistent API integration patterns
8. **Testing:** All API integrations tested
9. **Documentation:** API integration patterns documented
10. **Verification:** All mock data replacement verified

## Tasks / Subtasks

- [ ] Audit mock data
  - [ ] Search codebase for mock data
  - [ ] List all mock data locations
  - [ ] Prioritize replacements
- [ ] Replace contractor mock data
  - [ ] Replace in ContractorList
  - [ ] Replace in ContractorCard
  - [ ] Replace in contractor dialogs
- [ ] Replace job mock data
  - [ ] Replace in JobsTable
  - [ ] Replace in JobQueue
  - [ ] Replace in job dialogs
- [ ] Replace recommendation mock data
  - [ ] Replace in RecommendationsSheet
  - [ ] Replace in RecommendationCard
- [ ] Replace dashboard mock data
  - [ ] Replace in StatsOverview
  - [ ] Replace in ActivityFeed
- [ ] Add consistent error handling
  - [ ] Create error handling utilities
  - [ ] Apply to all API calls
- [ ] Add loading states
  - [ ] Add loading indicators
  - [ ] Add skeleton UI where needed
- [ ] Verify no mock remnants
  - [ ] Search for mock data patterns
  - [ ] Remove all mock data
- [ ] Create integration tests
  - [ ] Test all API integrations
  - [ ] Verify error handling

## Dev Notes

### Relevant Source Tree Info
- All frontend components: `frontend/components/`
- API client: `frontend/lib/api/generated/api-client.ts`
- Mock data: Throughout component files

### Architecture References
- **API Client:** Generated TypeScript client - see `docs/architecture/api-client-generation.md`
- **UI Components:** All frontend components - see `frontend/components/`
- **API Integration:** See individual UI connection stories

### Testing Standards
- **Test Location:** `frontend/__tests__/`
- **Integration Tests:** Test all API integrations
- **E2E Tests:** Playwright tests for complete flows

### UI Integration Notes
- **Existing UI:** UI is 95% complete with mock data
- **Mock Locations:** Mock data in component files (e.g., `mockContractors`, `generateRecommendations`)
- **Replacement:** This story ensures all mock data is replaced

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

