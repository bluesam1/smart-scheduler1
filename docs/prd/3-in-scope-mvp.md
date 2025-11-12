# 3) In Scope (MVP)

* Contractor CRUD (type, rating, base location, working hours)
* Availability engine (derive open slots from working hours + existing jobs)
* Distance/proximity (mapping API for travel distance/ETA)
* **Address validation (Google Places Autocomplete) with structured address storage**
* **Timezone tracking for job locations (derived from lat/long)**
* Weighted scoring & ranking (availability, rating, distance)
* Recommendation API: input (job type, desired date, location) → ranked contractors + suggested time slots
* **Dashboard statistics API (active contractors, pending jobs, utilization, assignment time)**
* **Activity feed API (recent system events for dashboard)**
* **Settings API (job types and skills management)**
* **Event publishing (JobCreated, RecommendationRequested, JobAssigned, JobRescheduled, JobCancelled, ContractorRated)**
* Dispatcher UI to view jobs, request recommendations, confirm bookings; real-time updates via SignalR
