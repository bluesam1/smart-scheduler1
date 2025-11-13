# SignalR MVP Status & Remaining Work

**Date:** 2025-01-XX  
**Status:** SignalR is REQUIRED for MVP (PRD line 27, line 318)

## PRD Requirements

The PRD explicitly states SignalR is required for MVP:

- **Line 27:** "Dispatcher UI to view jobs, request recommendations, confirm bookings; **real-time updates via SignalR**"
- **Line 318:** "MVP scope: Implement **two SignalR events only** — `RecommendationReady` (notify dispatcher UI to refresh ranked list) and `JobAssigned` (update job/contractor cards)."

## Current Status

### ✅ Completed

1. **Story 0.3: SignalR Real-time Setup** - COMPLETE
   - SignalR hub created (`RecommendationsHub`)
   - Backend infrastructure configured
   - Event publishing service (`IRealtimePublisher`, `SignalRRealtimePublisher`)
   - Authentication and CORS configured
   - Documentation created (`src/SIGNALR.md`)

### ⏳ Required for MVP (Not Started)

1. **Story FE.2: Implement SignalR Client Connection** - APPROVED, NOT STARTED
   - **Priority:** CRITICAL - Blocks all frontend SignalR integration
   - **Dependencies:** None (backend ready)
   - **Tasks:**
     - Install `@microsoft/signalr` package
     - Create SignalR client service
     - Implement connection management
     - Implement group subscription (dispatcher groups)
     - Create React context/hook for SignalR
     - Handle reconnection logic
   - **Blocks:** Stories 2.3, 6.5, 7.6

2. **Story 6.5: Real-time Recommendation Updates (SignalR)** - APPROVED, NOT STARTED
   - **Priority:** CRITICAL - Required for dispatcher UX
   - **Dependencies:** Story FE.2 (frontend client)
   - **Tasks:**
     - Publish `RecommendationReady` event after recommendations generated
     - Send to dispatcher groups (`/dispatch/{region}`)
     - Frontend subscribes to `RecommendationReady` events
     - Update recommendations UI on event
   - **Event Contract:** See `docs/prd/realtime.md` lines 8-38

3. **Story 7.6: Real-time Assignment Updates (SignalR)** - APPROVED, NOT STARTED
   - **Priority:** CRITICAL - Required for dispatcher UX
   - **Dependencies:** Story FE.2 (frontend client), Story 7.5 (JobAssigned event publishing)
   - **Tasks:**
     - Publish `JobAssigned` event when job assigned
     - Send to dispatcher groups (`/dispatch/{region}`)
     - Send to contractor groups (`/contractor/{contractorId}`)
     - Frontend subscribes to `JobAssigned` events
     - Update job UI on event
     - Update contractor schedule on event (optional, lower priority)
   - **Event Contract:** See `docs/prd/realtime.md` lines 41-77

4. **Story 2.3: Connect Job UI to API** - SignalR Integration Task
   - **Priority:** CRITICAL - Required for job list updates
   - **Dependencies:** Stories FE.2 and 7.6
   - **Tasks:**
     - Subscribe to `JobAssigned` events in job UI
     - Update job list when `JobAssigned` event received
     - Update job details view when `JobAssigned` event received
   - **Status:** Task added, marked as REQUIRED for MVP

### 📋 Lower Priority (Post-MVP)

1. **Story 1.3: Connect Contractor UI to API** - SignalR Integration
   - **Priority:** LOW - Contractor schedule updates can use JobAssigned events
   - **Dependencies:** Story 7.6
   - **Note:** Primary SignalR focus is dispatcher UI per PRD MVP scope

## Implementation Sequence

For MVP completion, SignalR work should follow this sequence:

1. **Story FE.2** (Frontend Client) - Must be done first
   - Enables all frontend SignalR integration
   - No dependencies on other SignalR stories

2. **Story 6.5** (RecommendationReady Events) - Can be done in parallel with 7.6
   - Depends on FE.2
   - Backend event publishing can be done independently

3. **Story 7.6** (JobAssigned Events) - Can be done in parallel with 6.5
   - Depends on FE.2
   - May depend on Story 7.5 (JobAssigned event publishing in domain layer)
   - Backend event publishing can be done independently

4. **Story 2.3** (Job UI Integration) - Must be done after 7.6
   - Depends on FE.2 and 7.6
   - Frontend integration only

## Key Files & Locations

### Backend (Already Complete)
- `src/SmartScheduler.Realtime/Hubs/RecommendationsHub.cs` - SignalR hub
- `src/SmartScheduler.Realtime/Services/IRealtimePublisher.cs` - Event publishing interface
- `src/SmartScheduler.Realtime/Services/SignalRRealtimePublisher.cs` - SignalR implementation
- `src/SmartScheduler.Api/Program.cs` - SignalR configuration
- `src/SIGNALR.md` - Comprehensive documentation

### Frontend (To Be Implemented)
- `frontend/lib/realtime/signalr-client.ts` - SignalR client service (to be created)
- `frontend/lib/realtime/signalr-context.tsx` - React context (to be created)
- `frontend/hooks/use-signalr.ts` - React hook (to be created)
- `frontend/components/jobs/` - Job UI components (need SignalR integration)
- `frontend/components/recommendations/` - Recommendations UI (need SignalR integration)

## Event Contracts

### RecommendationReady Event
- **Channel:** `/dispatch/{region}`
- **Payload:** See `docs/prd/realtime.md` lines 13-23
- **TypeScript Type:** See `docs/prd/realtime.md` lines 30-38

### JobAssigned Event
- **Channels:** `/dispatch/{region}`, `/contractor/{contractorId}`
- **Payload:** See `docs/prd/realtime.md` lines 48-59
- **TypeScript Type:** See `docs/prd/realtime.md` lines 66-77

## Testing Requirements

Each story should include:
- Unit tests for SignalR client connection management
- Integration tests for event publishing
- Integration tests for frontend event handling
- E2E tests for real-time UI updates

## Notes

- Backend SignalR infrastructure is complete and ready
- Frontend SignalR client is the critical blocker
- All MVP SignalR work depends on Story FE.2
- Only two events required for MVP: `RecommendationReady` and `JobAssigned`
- All other real-time updates can use HTTP polling per PRD


