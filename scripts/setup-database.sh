#!/bin/bash

# Script to set up the database (PostGIS extension and migrations)

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

echo -e "${GREEN}Setting up database...${NC}"

# Get database endpoint
echo -e "${YELLOW}Getting database endpoint...${NC}"
DB_ENDPOINT=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Database \
  --query "Stacks[0].Outputs[?OutputKey=='DatabaseEndpoint'].OutputValue" \
  --output text 2>/dev/null || echo "")

if [ -z "${DB_ENDPOINT}" ]; then
    echo -e "${RED}Database stack not found. Deploy database stack first.${NC}"
    exit 1
fi

echo -e "${GREEN}Database endpoint: ${DB_ENDPOINT}${NC}"

# Get database password
echo -e "${YELLOW}Getting database password...${NC}"
DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/master-credentials \
  --query SecretString --output text | jq -r .password 2>/dev/null || echo "")

if [ -z "${DB_PASSWORD}" ]; then
    echo -e "${RED}Could not retrieve database password. Check Secrets Manager.${NC}"
    exit 1
fi

# Check if psql is available
if ! command -v psql >/dev/null 2>&1; then
    echo -e "${YELLOW}psql not found. Skipping PostGIS extension setup.${NC}"
    echo -e "${YELLOW}You can enable PostGIS manually by connecting to the database and running:${NC}"
    echo -e "${YELLOW}  CREATE EXTENSION IF NOT EXISTS postgis;${NC}"
    echo -e "${YELLOW}  CREATE EXTENSION IF NOT EXISTS postgis_topology;${NC}"
else
    # Enable PostGIS extension
    echo -e "${YELLOW}Enabling PostGIS extension...${NC}"
    PGPASSWORD=${DB_PASSWORD} psql -h ${DB_ENDPOINT} -U dbadmin -d smartscheduler -c "CREATE EXTENSION IF NOT EXISTS postgis;" 2>/dev/null || {
        echo -e "${RED}Failed to enable PostGIS extension. Check database connection.${NC}"
        exit 1
    }
    
    PGPASSWORD=${DB_PASSWORD} psql -h ${DB_ENDPOINT} -U dbadmin -d smartscheduler -c "CREATE EXTENSION IF NOT EXISTS postgis_topology;" 2>/dev/null || {
        echo -e "${YELLOW}Failed to enable postgis_topology extension (optional).${NC}"
    }
    
    echo -e "${GREEN}PostGIS extension enabled.${NC}"
fi

# Get connection string
echo -e "${YELLOW}Getting connection string...${NC}"
CONNECTION_STRING=$(aws secretsmanager get-secret-value \
  --secret-id smartscheduler/database/connection-string \
  --query SecretString --output text 2>/dev/null || echo "")

if [ -z "${CONNECTION_STRING}" ]; then
    echo -e "${RED}Could not retrieve connection string. Run configure-secrets.sh first.${NC}"
    exit 1
fi

# Run database migrations
echo -e "${YELLOW}Running database migrations...${NC}"
cd "${PROJECT_ROOT}/src"

dotnet ef database update \
  --project SmartScheduler.Infrastructure \
  --startup-project SmartScheduler.Api \
  --connection "${CONNECTION_STRING}"

echo -e "${GREEN}Database setup complete!${NC}"

