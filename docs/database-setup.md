# Database Setup Guide

This guide covers setting up PostgreSQL with PostGIS, running migrations, and seeding data for the SmartScheduler application.

## Prerequisites

- .NET 8.0 SDK or later
- PostgreSQL 15+ with PostGIS extension
- Visual Studio 2022, VS Code, or Rider (optional)

## PostgreSQL Setup

### 1. Install PostgreSQL

Install PostgreSQL 15 or later from [postgresql.org](https://www.postgresql.org/download/).

**Windows:**
- Download and run the installer from the official website
- During installation, make sure to include the PostGIS extension

**macOS:**
```bash
brew install postgresql@15
brew install postgis
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install postgresql-15 postgresql-15-postgis-3
```

### 2. Create Database

Connect to PostgreSQL and create the database:

```sql
CREATE DATABASE smartscheduler;
```

### 3. Enable PostGIS Extension

Connect to the `smartscheduler` database and enable PostGIS:

```sql
\c smartscheduler
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;
```

Verify PostGIS is installed:

```sql
SELECT PostGIS_Version();
```

## PostGIS Installation

PostGIS is a spatial database extension for PostgreSQL that enables geospatial queries and operations.

### Installation Methods

**Windows:**
- PostGIS is typically included with PostgreSQL installation
- If not, download from [postgis.net](https://postgis.net/install/)

**macOS:**
```bash
brew install postgis
```

**Linux:**
```bash
# Ubuntu/Debian
sudo apt install postgresql-15-postgis-3

# CentOS/RHEL
sudo yum install postgis33_15
```

### Verify Installation

After enabling the extension, verify it's working:

```sql
SELECT PostGIS_Version();
SELECT ST_AsText(ST_MakePoint(-74.006, 40.7128)); -- Should return 'POINT(-74.006 40.7128)'
```

## Configuration

### Connection String

Update the connection string in `src/SmartScheduler.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smartscheduler;Username=your_user;Password=your_password"
  }
}
```

### Environment Variables

You can also set the connection string via environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=smartscheduler;Username=postgres;Password=postgres"
```

## Migration Process

### Creating Migrations

When you add or modify domain entities, create a new migration:

```bash
cd src
dotnet ef migrations add MigrationName --project SmartScheduler.Infrastructure --startup-project SmartScheduler.Api
```

**Migration Naming Convention:**
- Use descriptive names: `AddContractorTable`, `AddJobAssignments`, etc.
- EF Core automatically prefixes with timestamp: `20250112123456_AddContractorTable`

### Automatic Migration on Startup

The API can automatically apply pending migrations on startup. This is controlled by the `Database:AutoMigrate` configuration setting:

**Development Environment:**
- Auto-migration is **enabled by default** (`appsettings.Development.json`)
- Migrations are applied automatically when the API starts
- If migration fails, the API continues running (with a warning)

**Production Environment:**
- Auto-migration is **disabled by default** (`appsettings.json`)
- Migrations must be applied manually or via CI/CD pipeline
- If enabled in production, migration failures will cause the API to fail fast

**Configuration:**

To enable/disable auto-migration, set in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Database": {
    "AutoMigrate": true  // or false
  }
}
```

**Note:** In production, it's recommended to run migrations manually or via CI/CD for better control and rollback capabilities.

### Applying Migrations Manually

**Development Environment:**

Using script (recommended):
```bash
./scripts/migrate-database-dev.sh
# or on Windows:
.\scripts\migrate-database-dev.ps1
```

Manual command:
```bash
cd src
dotnet ef database update --project SmartScheduler.Infrastructure --startup-project SmartScheduler.Api
```

**Production Environment:**

Using script (recommended):
```bash
./scripts/migrate-database-prod.sh "Host=prod-db;Port=5432;Database=smartscheduler;Username=user;Password=pass"
# or on Windows:
.\scripts\migrate-database-prod.ps1 "Host=prod-db;Port=5432;Database=smartscheduler;Username=user;Password=pass"
```

Or using environment variable:
```bash
export DATABASE_CONNECTION_STRING="Host=prod-db;Port=5432;Database=smartscheduler;Username=user;Password=pass"
./scripts/migrate-database-prod.sh
```

Manual command:
```bash
cd src
dotnet ef database update --project SmartScheduler.Infrastructure --startup-project SmartScheduler.Api --connection "YOUR_CONNECTION_STRING"
```

### Rolling Back Migrations

To rollback the last migration:

```bash
./scripts/rollback-migration.sh
# or on Windows:
.\scripts\rollback-migration.ps1
```

To rollback to a specific migration:

```bash
./scripts/rollback-migration.sh MigrationName
# or on Windows:
.\scripts\rollback-migration.ps1 MigrationName
```

**Note:** Rolling back migrations that have been applied to production should be done with extreme caution. Always backup the database first.

### Listing Migrations

View all migrations and their status:

```bash
cd src
dotnet ef migrations list --project SmartScheduler.Infrastructure --startup-project SmartScheduler.Api
```

## Seed Data Process

### Development Seed Data

Development seed data includes:
- Sample contractors with various skills and locations
- Sample jobs with different types and requirements
- System configuration (job types, skills catalog)

**Apply development seed data:**

```bash
./scripts/seed-development-data.sh
# or on Windows:
.\scripts\seed-development-data.ps1
```

**Clear development seed data:**

The seed data infrastructure supports clearing seeded data while preserving system configuration. This will be implemented once domain entities are created.

### Test Seed Data

Test seed data is minimal and designed for integration tests:
- Minimal contractors and jobs
- Isolated test data
- Fast test execution

Test data seeding is typically handled automatically by test infrastructure.

### Seed Data Characteristics

- **Idempotent:** Running seed scripts multiple times is safe
- **Isolated:** Development and test data are separate
- **Configurable:** Can be applied or cleared independently

## Database Schema

The initial migration includes:
- PostGIS extension enabled
- Database structure ready for domain entities

Once domain entities are created (Contractor, Job, Assignment, etc.), additional migrations will create the full schema.

## Troubleshooting

### Migration Errors

**Error: "No design-time services were found"**
- Ensure `Microsoft.EntityFrameworkCore.Design` is installed in the API project
- Verify the design-time factory exists: `src/SmartScheduler.Infrastructure/Data/DesignTimeDbContextFactory.cs`

**Error: "Connection string is null"**
- Check `appsettings.json` has the correct connection string
- Verify environment variables if using them
- Ensure the design-time factory can find the configuration

### PostGIS Errors

**Error: "extension postgis does not exist"**
- Install PostGIS extension for your PostgreSQL version
- Enable the extension: `CREATE EXTENSION IF NOT EXISTS postgis;`

**Error: "function st_makepoint does not exist"**
- Verify PostGIS is properly installed and enabled
- Check PostgreSQL version compatibility

### Connection Issues

**Error: "Connection refused"**
- Verify PostgreSQL is running: `pg_isready` or check service status
- Check firewall settings
- Verify connection string host and port

**Error: "Authentication failed"**
- Verify username and password in connection string
- Check PostgreSQL `pg_hba.conf` configuration

## Best Practices

1. **Always backup before migrations in production**
2. **Test migrations in development/staging first**
3. **Use transactions for production migrations when possible**
4. **Keep migration files in version control**
5. **Never modify existing migrations that have been applied**
6. **Use descriptive migration names**
7. **Review generated SQL before applying migrations**
8. **Document any manual database changes**

## Additional Resources

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostGIS Documentation](https://postgis.net/documentation/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

