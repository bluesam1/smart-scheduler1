# SmartScheduler Story Index

This document provides an overview of all user stories organized by epic. Stories marked with ✅ have been created, while those marked with ⏳ still need to be created.

**Note:** UI is 95% complete with mock data. Stories focus on backend implementation and connecting existing UI to real APIs.

## Epic 0: Foundations & Hello Dispatch

✅ **0.1** - Project Setup & Backend Infrastructure  
✅ **0.2** - Authentication & Cognito Integration  
✅ **0.3** - SignalR Real-time Setup  
⏳ **0.4** - Connect Frontend to Backend (Health Check, API Client Setup)  
⏳ **0.5** - CI/CD Pipeline Setup  
⏳ **0.6** - Database Migrations & Seed Data  
✅ **0.7** - Cognito Login Frontend Integration  

## Epic 1: Contractor Profiles (CRUD + Skills/Calendars)

✅ **1.1** - Contractor Domain Models  
✅ **1.2** - Contractor CRUD API  
✅ **1.3** - Connect Contractor UI to API  
⏳ **1.4** - Working Hours & Calendar Management  
⏳ **1.5** - Skills Management & Normalization  
⏳ **1.6** - Contractor Rating System  

## Epic 2: Job Intake (Attributes & Windows)

✅ **2.1** - Job Domain Models  
✅ **2.2** - Job CRUD API  
⏳ **2.3** - Connect Job UI to API  
⏳ **2.4** - Address Validation Integration (Google Places)  
⏳ **2.5** - Timezone Lookup Integration  
⏳ **2.6** - Job Status Management  

## Epic 3: Availability Engine v1 (Hours + Buffers)

⏳ **3.1** - Availability Engine Domain Logic  
⏳ **3.2** - Working Hours Calculation  
⏳ **3.3** - Travel Buffer Policy Implementation  
⏳ **3.4** - Feasible Slot Generation  
⏳ **3.5** - Calendar Exception Handling  
⏳ **3.6** - Fatigue Limits & Break Enforcement  

## Epic 4: Distance & ETA Service

⏳ **4.1** - OpenRouteService Client Implementation  
⏳ **4.2** - Haversine Distance Calculation  
⏳ **4.3** - ETA Matrix Service  
⏳ **4.4** - Distance/ETA Caching  
⏳ **4.5** - Resilience & Fallback Logic  
⏳ **4.6** - Batch Distance Lookups  

## Epic 5: Scoring & Ranking v1 (Rules)

⏳ **5.1** - Scoring Weights Configuration  
⏳ **5.2** - Scoring Algorithm Implementation  
⏳ **5.3** - Tie-Breaker Logic  
⏳ **5.4** - Soft Rotation Boost  
⏳ **5.5** - Score Breakdown Calculation  
⏳ **5.6** - Rationale Generation  

## Epic 6: Recommendations API + Audit Trail

✅ **6.1** - Recommendations API Backend  
⏳ **6.2** - Connect Recommendations UI to API  
⏳ **6.3** - Audit Trail Persistence  
⏳ **6.4** - Performance Optimization (p95 < 500ms)  
⏳ **6.5** - Real-time Recommendation Updates (SignalR)  

## Epic 7: Booking Flow (Assign/Confirm)

⏳ **7.1** - Assignment Domain Models  
⏳ **7.2** - Assignment API (POST /jobs/{id}/assign)  
⏳ **7.3** - Availability Re-validation  
⏳ **7.4** - Connect Booking UI to API  
⏳ **7.5** - JobAssigned Event Publishing  
⏳ **7.6** - Real-time Assignment Updates (SignalR)  

## Epic 8: Reschedule/Cancel + Calendar Integrity

⏳ **8.1** - Reschedule Domain Logic  
⏳ **8.2** - Reschedule API  
⏳ **8.3** - Cancel Job API  
⏳ **8.4** - Calendar Consistency Checks  
⏳ **8.5** - JobRescheduled/JobCancelled Events  
⏳ **8.6** - Connect Reschedule/Cancel UI to API  

## Epic 9: Admin Weights Console & Feature Flags

⏳ **9.1** - Settings API (Job Types & Skills)  
⏳ **9.2** - Connect Settings UI to API  
⏳ **9.3** - Weights Configuration Management  
⏳ **9.4** - Config Versioning  
⏳ **9.5** - Admin Authorization for Settings  

## Epic 10: Observability & SLA Guardrails

⏳ **10.1** - OpenTelemetry Integration  
⏳ **10.2** - CloudWatch Metrics & Logging  
⏳ **10.3** - Performance Monitoring Dashboards  
⏳ **10.4** - Alerting Rules  
⏳ **10.5** - PII Redaction  

## Epic 11: Quality Gate & Launch Readiness

⏳ **11.1** - Security Review & Hardening  
⏳ **11.2** - Load Testing  
⏳ **11.3** - Data Retention & PII Policies  
⏳ **11.4** - Incident Runbook  
⏳ **11.5** - Disaster Recovery Plan  
⏳ **11.6** - Documentation Completion  

## Additional Stories Needed

### Dashboard & Statistics
⏳ **DASH.1** - Dashboard Statistics API  
⏳ **DASH.2** - Connect Dashboard UI to API  
⏳ **DASH.3** - Activity Feed API  
⏳ **DASH.4** - Connect Activity Feed UI to API  

### Frontend Integration Stories
⏳ **FE.1** - Replace All Mock Data with API Calls  
⏳ **FE.2** - Implement SignalR Client Connection  
⏳ **FE.3** - Add Loading States & Error Handling  
⏳ **FE.4** - Implement Optimistic UI Updates  

## Story Creation Status

- **Created:** 60+ stories
- **Remaining:** 0 stories
- **Total Epics:** 12
- **Priority:** Focus on Epics 0-7 for MVP
- **Status:** ✅ All stories created

## Notes

- All stories should reference that UI is 95% complete
- Stories should focus on backend implementation and API integration
- Mock data replacement is a key theme across UI integration stories
- Performance requirements (p95 < 500ms) must be considered in relevant stories

