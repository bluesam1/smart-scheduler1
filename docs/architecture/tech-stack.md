# Tech Stack

This is the DEFINITIVE technology selection for the entire project. All development must use these exact versions.

### Technology Stack Table

| Category | Technology | Version | Purpose | Rationale |
|----------|-----------|---------|---------|------------|
| Frontend Language | TypeScript | 5.x | Type-safe frontend development | Provides type safety for React/Next.js, catches errors at compile time |
| Frontend Framework | Next.js | 14+ | React framework with App Router | PRD specifies Next.js 14+ with App Router. Provides SSR, routing, and optimized builds |
| UI Component Library | React | 18+ | UI component library | Core dependency of Next.js. Component-based architecture for reusable UI |
| State Management | React Hooks + Context | Built-in | Client-side state management | Sufficient for MVP. Can add Zustand/Redux later if needed |
| Backend Language | C# | 12.0 | Backend application development | PRD specifies C# / .NET 8. Modern language with strong typing |
| Backend Framework | .NET | 8.0 | Application framework | PRD specifies .NET 8. Provides ASP.NET Core, EF Core, SignalR |
| API Style | REST | - | HTTP API endpoints | RESTful APIs with Minimal APIs pattern. Simple, well-understood |
| Database | PostgreSQL | 15+ | Primary data store | PRD specifies PostgreSQL with PostGIS extension for geospatial queries |
| ORM | Entity Framework Core | 8.0 | Data access abstraction | PRD specifies EF Core. Provides migrations, LINQ, and repository pattern support |
| Cache | IMemoryCache (.NET) | Built-in | In-memory caching | In-memory caching for MVP to reduce deployment complexity. Enables sub-500ms responses for single-instance deployments. Redis can be added post-MVP for multi-instance support |
| Authentication | Amazon Cognito | - | User authentication | PRD specifies Cognito (User Pool + Hosted UI). Managed OIDC/OAuth2 |
| Maps API | OpenRouteService | v2 | Distance/ETA calculations | PRD specifies OpenRouteService as primary mapping API. Required for MVP |
| Address Validation | Google Places Autocomplete (New) | Latest | Address validation and structured parsing | PRD specifies Google Places Autocomplete. Autocomplete sessions no-charge; only Place Details/Address Validation charged |
| Timezone Lookup | Timezone API or Library | Latest | Timezone from lat/long | Derive IANA timezone from coordinates for job locations |
| Real-time | SignalR | 8.0 | WebSocket-based real-time | PRD specifies SignalR for real-time updates to dispatchers and contractors |
| CQRS Framework | MediatR | 12.x | Command/Query separation | PRD specifies MediatR handlers for CQRS pattern. Decouples commands/queries |
| Frontend Testing | Jest + React Testing Library | Latest | Component and unit testing | Industry standard for React/Next.js testing. Supports component testing |
| Backend Testing | xUnit | 2.x | Unit and integration testing | Standard .NET testing framework. Supports integration tests with TestContainers |
| E2E Testing | Playwright | Latest | End-to-end browser testing | Modern E2E framework. Supports multiple browsers and headless mode |
| Build Tool | .NET CLI | 8.0 | Backend build tool | Built into .NET SDK. Handles compilation, testing, publishing |
| Package Manager | npm | Latest | Frontend dependency management | Standard for Node.js projects |
| Bundler | Next.js (Turbopack) | 14+ | Frontend bundling | Next.js 14+ includes Turbopack. Fast bundling and HMR |
| IaC Tool | AWS CDK / Terraform | Latest | Infrastructure as Code | AWS CDK preferred for AWS-native resources. Terraform as alternative |
| CI/CD | GitHub Actions | - | Continuous integration/deployment | Common choice for GitHub repos. Supports .NET and Node.js workflows |
| Monitoring | OpenTelemetry + CloudWatch | Latest | Application observability | PRD specifies OpenTelemetry for traces, CloudWatch for metrics/logs |
| Logging | Serilog + CloudWatch Logs | Latest | Structured logging | Serilog for .NET structured logging. CloudWatch Logs for aggregation |
| CSS Framework | Tailwind CSS | 3.x | Utility-first CSS | Modern, fast CSS framework. Works well with Next.js and component libraries |
| API Client Generator | NSwag | Latest | TypeScript client generation | Generates type-safe TypeScript API client from OpenAPI spec using interface-based models |
