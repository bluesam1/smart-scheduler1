# GOLD: Teamfront – SmartScheduler

## Product Requirements Document (PRD)
**SmartScheduler – Intelligent Contractor Discovery & Scheduling System**

---

## 1. Introduction and Project Goal

### 1.1 Project Goal
The goal of the SmartScheduler project is to automate and optimize the assignment of contractors to flooring jobs. We aim to build an intelligent scheduling system that uses structured logic and real-time data to automatically match jobs to the best available, qualified contractor, optimizing efficiency and job success rates.

### 1.2 Business Context
Current manual scheduling processes in the flooring industry lead to significant inefficiencies: long response times, scheduling errors, and underutilized labor capacity. This project simulates the evolution of Floorzap from a job-tracking tool into a smart operational assistant, directly improving workforce utilization, on-time performance, and customer satisfaction.

### 1.3 Success Metrics (Impact)
The success of this system will be measured by its ability to drive operational efficiency:

- **Reduction in Manual Time:** ↓ 40% reduction in manual scheduling time.
- **Utilization Rate:** ↑ 25% improvement in contractor utilization rate.
- **Assignment Speed:** ↑ 20% faster average job assignment time.
- **Customer Satisfaction:** Improved customer satisfaction through faster and more accurate contractor matching.

---

## 2. Core Functional Requirements
The system must implement the following capabilities to automate contractor discovery and scheduling:

### 2.1 Domain Management
- **Contractor Management (CRUD):** Implement full CRUD (Create, Read, Update, Delete) functionality for contractors, including attributes such as: type, rating, base location, and schedule (working hours).

### 2.2 Scheduling Engine Logic
1. **Availability Engine:** Develop the core logic to accurately determine and find open time slots for a contractor based on their defined working hours and their existing assigned jobs.
2. **Distance & Proximity Check:** Integrate with a mapping API (Google Maps or OpenRouteService) to calculate real-time travel distances and estimated travel times between job sites (for proximity analysis).

### 2.3 Intelligent Scoring and Ranking
A key requirement is the **Scoring & Ranking Engine**. This must implement a weighted scoring model to rank contractors based on job fit:

```
score = (availabilityWeight × availabilityScore) 
      + (ratingWeight × ratingScore) 
      + (distanceWeight × distanceScore)
```

- **Contractor Recommendation API:** The primary API endpoint must accept job type, desired date, and location as input, and output a ranked list of available contractors complete with their open, suggested time slots.

### 2.4 Event-Driven Updates
- **Messaging:** The system must utilize a message bus (or simulate one) to publish events such as **JobAssigned**, **ScheduleUpdated**, and **ContractorRated**. This is critical for decoupled, real-time updates across the system (e.g., messaging and UI).

---

## 3. Architecture and Technical Requirements

### 3.1 Architecture Principles (Mandatory)
The application architecture is central to the assessment and must follow established enterprise design patterns:

- **Domain-Driven Design (DDD):** Core entities (**Job**, **Contractor**, **Schedule**, **Assignment**) must be modeled as robust Domain Objects.
- **CQRS:** Implement a clean separation between **Commands** (e.g., *AssignJob*, *UpdateContractor*) and **Queries** (e.g., *GetRankedContractors*).
- **Layer Separation:** Maintain clean boundaries between the **Domain**, **Application**, and **Infrastructure** layers.

### 3.2 Technical Stack and Infrastructure
- **Back-End (API):** C# with .NET 8 (**Mandatory**).
- **Front-End (UI):** TypeScript with React or Next.js.
- **Real-Time Messaging:** SignalR is required for real-time communication (e.g., pushing **JobAssigned** events to the dispatcher or contractor).
- **Database:** PostgreSQL or SQL Server.
- **Cloud Platform:** AWS (**Mandatory**).
- **Mapping:** Requires integration with a public Map API (e.g., Google Maps, OpenRouteService).

### 3.3 AI/LLM Frameworks (Augmentation)
AI integration is optional but highly encouraged for augmentation to demonstrate advanced usage:

- **OpenAI API / LangChain:** Use of LLMs for generating clear, conversational explanations for why a contractor was ranked #1 (e.g., "Contractor X was chosen due to high rating and shortest travel time to the next job").
- **Scikit-learn / TensorFlow (Optional):** Can be used to train a basic scoring model that goes beyond the explicit weighted formula, perhaps by incorporating historical job success rates.

### 3.4 Performance Benchmarks
- **Time Constraint:** Recommended project completion is 1 week.
- **Ranking Latency:** The Contractor Recommendation API must return a ranked list in a responsive manner (e.g., under **500ms**) to ensure a smooth dispatcher workflow.

---

## 4. Front-End Deliverables

### 4.1 Dispatcher UI (Front-End View)
The Front-End **MUST** provide an interactive user interface for dispatchers to:

- View open jobs and request contractor recommendations.
- See the top-ranked contractors returned by the API, including the calculated score and available time slots.
- Confirm and initiate a booking, triggering the relevant assignment event.

---

## 5. Project Deliverables and Constraints

### 5.1 Code Quality Standards
- **Architecture:** Clean, modular design adhering to DDD/CQRS/VSA principles.
- **Documentation:** Clear documentation of the weighted scoring algorithm and its implementation.
- **Testing:** Integration tests **MUST** validate the end-to-end functionality, including contractor setup, job creation, and the correct ranking output from the scoring model.

### 5.2 Submission Requirements
1. **Code Repository:** Complete, functional code repository (GitHub preferred).
2. **Brief Technical Writeup (1–2 pages):** Documenting the DDD model, CQRS command/query structure, the weighted scoring logic, and the choice of concurrency/messaging (SignalR).
3. **Demo:** A video or live presentation demonstrating the automated scoring and assignment workflow.
4. **AI Tool Documentation:** Detailed documentation of any AI tools (OpenAI, LangChain, etc.) used, including example prompts and a justification for how they enhanced the system (e.g., generating ranking rationales).
5. **Test Cases and Validation Results:** Evidence of passing integration tests.

---

## Appendix: Outline
- Product Requirements Document (PRD): SmartScheduler – Intelligent Contractor Discovery & Scheduling System
  - 1. Introduction and Project Goal
    - 1.1 Project Goal
    - 1.2 Business Context
    - 1.3 Success Metrics (Impact)
  - 2. Core Functional Requirements
    - 2.1 Domain Management
    - 2.2 Scheduling Engine Logic
    - 2.3 Intelligent Scoring and Ranking
    - 2.4 Event-Driven Updates
  - 3. Architecture and Technical Requirements
    - 3.1 Architecture Principles (Mandatory)
    - 3.2 Technical Stack and Infrastructure
    - 3.3 AI/LLM Frameworks (Augmentation)
    - 3.4 Performance Benchmarks
  - 4. Front-End Deliverables
    - 4.1 Dispatcher UI (Front-End View)
  - 5. Project Deliverables and Constraints
    - 5.1 Code Quality Standards
    - 5.2 Submission Requirements
