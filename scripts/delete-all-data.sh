#!/bin/bash

# Shell script to delete all data from the database
# WARNING: This will delete ALL data! Use with caution.

# Colors for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Get connection string from argument or environment variable
CONNECTION_STRING="${1:-$DB_CONNECTION_STRING}"

# Check if connection string is provided
if [ -z "$CONNECTION_STRING" ]; then
    echo -e "${RED}Error: Connection string not provided.${NC}"
    echo -e "${YELLOW}Usage: ./delete-all-data.sh 'your_connection_string'${NC}"
    echo -e "${YELLOW}Or set the DB_CONNECTION_STRING environment variable${NC}"
    exit 1
fi

echo ""
echo -e "${RED}WARNING: This will delete ALL data from the database!${NC}"
echo -e "${YELLOW}This includes:${NC}"
echo -e "${YELLOW}  - All contractors${NC}"
echo -e "${YELLOW}  - All jobs${NC}"
echo -e "${YELLOW}  - All assignments${NC}"
echo -e "${YELLOW}  - All audit records${NC}"
echo -e "${YELLOW}  - All event logs${NC}"
echo ""
read -p "Type 'DELETE ALL' to confirm: " confirmation

if [ "$confirmation" != "DELETE ALL" ]; then
    echo -e "${GREEN}Operation cancelled.${NC}"
    exit 0
fi

echo ""
echo -e "${CYAN}Connecting to database...${NC}"

# Execute SQL commands using psql
# Parse connection string to get components
# Expected format: Host=localhost;Port=5432;Database=smartscheduler;Username=user;Password=pass

export PGPASSWORD=$(echo "$CONNECTION_STRING" | grep -o 'Password=[^;]*' | cut -d'=' -f2)
HOST=$(echo "$CONNECTION_STRING" | grep -o 'Host=[^;]*' | cut -d'=' -f2)
PORT=$(echo "$CONNECTION_STRING" | grep -o 'Port=[^;]*' | cut -d'=' -f2)
DATABASE=$(echo "$CONNECTION_STRING" | grep -o 'Database=[^;]*' | cut -d'=' -f2)
USERNAME=$(echo "$CONNECTION_STRING" | grep -o 'Username=[^;]*' | cut -d'=' -f2)

# Default values if not found
HOST=${HOST:-localhost}
PORT=${PORT:-5432}
DATABASE=${DATABASE:-smartscheduler}
USERNAME=${USERNAME:-postgres}

echo -e "${GREEN}Connected successfully!${NC}"
echo ""

# Execute DELETE commands in transaction
psql -h "$HOST" -p "$PORT" -U "$USERNAME" -d "$DATABASE" << EOF
BEGIN;

-- Delete in order to respect foreign key constraints
\echo 'Deleting from Assignments...'
DELETE FROM "Assignments";

\echo 'Deleting from AuditRecommendations...'
DELETE FROM "AuditRecommendations";

\echo 'Deleting from EventLogs...'
DELETE FROM "EventLogs";

\echo 'Deleting from Jobs...'
DELETE FROM "Jobs";

\echo 'Deleting from Contractors...'
DELETE FROM "Contractors";

COMMIT;

\echo ''
\echo 'SUCCESS: All data deleted!'
\echo ''
EOF

if [ $? -eq 0 ]; then
    echo -e "${GREEN}SUCCESS: All data has been deleted from the database!${NC}"
    echo ""
else
    echo -e "${RED}ERROR: Failed to delete data. Transaction rolled back.${NC}"
    exit 1
fi

# Clear password from environment
unset PGPASSWORD

