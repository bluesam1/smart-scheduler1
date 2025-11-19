# SmartScheduler Backend

## Overview

SmartScheduler is a .NET 8 backend API built with Clean Architecture principles, using Domain-Driven Design (DDD) and CQRS patterns.

## Project Structure

```
src/
├── SmartScheduler.Api/              # ASP.NET Core Web API (entry point)
├── SmartScheduler.Application/      # Application layer (CQRS handlers, DTOs)
├── SmartScheduler.Domain/            # Domain layer (entities, value objects)
├── SmartScheduler.Infrastructure/   # Infrastructure layer (EF Core, repositories)
├── SmartScheduler.Realtime/          # SignalR hubs for real-time updates
└── SmartScheduler.Api.Tests/        # Integration tests
```

## Prerequisites

- .NET 8.0 SDK or later
- PostgreSQL 15+ with PostGIS extension
- Visual Studio 2022, VS Code, or Rider

## Setup Instructions

### 1. Database Setup

1. Install PostgreSQL 15+ with PostGIS extension
2. Create a database:
   ```sql
   CREATE DATABASE smartscheduler;
   ```
3. Enable PostGIS extension:
   ```sql
   \c smartscheduler
   CREATE EXTENSION IF NOT EXISTS postgis;
   ```

### 2. Configuration

Update the connection string in `SmartScheduler.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smartscheduler;Username=your_user;Password=your_password"
  }
}
```

### 3. Database Migrations

Apply the initial migration to create the database schema:

```bash
cd src/SmartScheduler.Infrastructure
dotnet ef database update --startup-project ../SmartScheduler.Api
```

Or use the .NET CLI:

```bash
cd src
dotnet ef database update --project SmartScheduler.Infrastructure --startup-project SmartScheduler.Api
```

### 4. Run the Application

```bash
cd src/SmartScheduler.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5004`
- HTTPS: `https://localhost:5005`
- Swagger UI: `http://localhost:5004/swagger` (Development only)

## Health Check

The API includes a health check endpoint:

```bash
curl http://localhost:5004/health
```

## Testing

Run all tests:

```bash
cd src
dotnet test
```

## Technology Stack

- **.NET 8.0**: Application framework
- **ASP.NET Core**: Web API framework
- **Entity Framework Core 8.0**: ORM with PostgreSQL provider
- **PostgreSQL 15+**: Database with PostGIS extension
- **MediatR 12.x**: CQRS pattern implementation
- **SignalR 8.0**: Real-time communication
- **NSwag**: OpenAPI/Swagger documentation
- **Serilog**: Structured logging
- **xUnit**: Testing framework

## Development

### Adding a New Migration

```bash
cd src/SmartScheduler.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../SmartScheduler.Api
```

### Generating OpenAPI Spec

The OpenAPI specification is automatically generated when the API runs. Use NSwag to generate TypeScript clients for the frontend.

## Logging

Logs are written to:
- Console (structured JSON)
- File: `logs/smartscheduler-{date}.txt`

## CORS Configuration

CORS is configured to allow requests from `http://localhost:3000` (Next.js frontend) with credentials support.

## Next Steps

- Configure Amazon Cognito for authentication (Story 0.2)
- Set up SignalR hubs (Story 0.3)
- Connect frontend to backend (Story 0.4)




