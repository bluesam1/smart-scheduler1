# Introduction

This document outlines the complete fullstack architecture for SmartScheduler, including backend systems, frontend implementation, and their integration. It serves as the single source of truth for AI-driven development, ensuring consistency across the entire technology stack.

This unified approach combines what would traditionally be separate backend and frontend architecture documents, streamlining the development process for modern fullstack applications where these concerns are increasingly intertwined.

### Starter Template or Existing Project

**N/A - Greenfield project**

This is a new project starting from scratch. No existing codebase or starter templates are being used. The architecture will be built following the PRD specifications for a Modular Monolith with DDD + CQRS patterns.

### Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2025-01-XX | 1.0 | Initial architecture document created | Winston (Architect) |
| 2025-01-XX | 1.1 | Simplified MVP: Replaced Redis with in-memory caching, simplified Outbox to EventLog with in-process publishing | Winston (Architect) |
| 2025-11-12 | 1.2 | Added NSwag for TypeScript API client generation with interface-based models | Winston (Architect) |
| 2025-11-12 | 1.3 | Added missing APIs for v0 frontend: Settings, Activity Feed, Dashboard Stats, and enhanced Contractor/Job models | Winston (Architect) |
| 2025-11-12 | 1.4 | Added Google Places Autocomplete for address validation, timezone tracking for job locations, and dashboard statistics implementation | Winston (Architect) |
