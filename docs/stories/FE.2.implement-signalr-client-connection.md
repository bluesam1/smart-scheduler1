# Story FE.2: Implement SignalR Client Connection

## Status
Approved

## Story

**As a** dispatcher or contractor,
**I want** the frontend to connect to SignalR,
**so that** I receive real-time updates.

## Acceptance Criteria

1. **SignalR Client:** SignalR client configured in frontend
2. **Connection Management:** Connection established and managed
3. **Group Subscription:** Client subscribes to appropriate groups
4. **Event Handling:** Events received and processed
5. **Reconnection:** Automatic reconnection on disconnect
6. **Error Handling:** Graceful handling of connection errors
7. **Connection State:** Connection state displayed (optional)
8. **Performance:** Connection doesn't impact UI performance
9. **Testing:** SignalR integration tested
10. **Documentation:** SignalR client usage documented

## Tasks / Subtasks

- [ ] Install SignalR client
  - [ ] Install @microsoft/signalr package
  - [ ] Configure TypeScript types
- [ ] Create SignalR client service
  - [ ] Create `SignalRClient` class
  - [ ] Configure connection
  - [ ] Handle connection lifecycle
- [ ] Implement connection management
  - [ ] Establish connection on app load
  - [ ] Handle connection state
  - [ ] Implement reconnection logic
- [ ] Implement group subscription
  - [ ] Subscribe to dispatcher groups
  - [ ] Subscribe to contractor groups
  - [ ] Handle group membership
- [ ] Implement event handlers
  - [ ] Handle RecommendationReady event
  - [ ] Handle JobAssigned event
  - [ ] Handle JobRescheduled event
  - [ ] Handle JobCancelled event
- [ ] Add error handling
  - [ ] Handle connection errors
  - [ ] Handle reconnection failures
  - [ ] Log errors
- [ ] Integrate with React
  - [ ] Create SignalR context/hook
  - [ ] Use in components
  - [ ] Handle component lifecycle
- [ ] Add connection state display (optional)
  - [ ] Show connection status
  - [ ] Show reconnection status
- [ ] Create unit tests
  - [ ] Test connection management
  - [ ] Test event handling
  - [ ] Test reconnection

## Dev Notes

### Relevant Source Tree Info
- SignalR client: `frontend/lib/realtime/signalr-client.ts`
- SignalR context: `frontend/lib/realtime/signalr-context.tsx`
- SignalR hook: `frontend/hooks/use-signalr.ts`

### Architecture References
- **SignalR:** SignalR 8.0 - see `docs/architecture/tech-stack.md`
- **Real-time:** See `docs/prd/realtime.md` for SignalR events
- **Backend Setup:** SignalR hub setup - see Story 0.3

### Testing Standards
- **Test Location:** `frontend/__tests__/`
- **SignalR Tests:** Test connection, events, reconnection
- **Integration Tests:** Test end-to-end real-time flow

### UI Integration Notes
- SignalR client used throughout frontend
- Components subscribe to events
- Real-time updates improve UX

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

