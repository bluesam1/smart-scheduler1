#!/bin/bash

# Script to configure AWS Secrets Manager secrets for SmartScheduler

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Configuring AWS Secrets Manager secrets...${NC}"

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

# Update database connection string
echo -e "${YELLOW}Updating database connection string secret...${NC}"
CONNECTION_STRING="Host=${DB_ENDPOINT};Port=5432;Database=smartscheduler;Username=dbadmin;Password=${DB_PASSWORD}"

aws secretsmanager update-secret \
  --secret-id smartscheduler/database/connection-string \
  --secret-string "${CONNECTION_STRING}" >/dev/null

echo -e "${GREEN}Database connection string updated.${NC}"

# Prompt for API keys
echo -e "${YELLOW}API Keys Configuration${NC}"

# OpenRouteService API key
read -p "Enter OpenRouteService API key (or press Enter to skip): " ORS_API_KEY
if [ -n "${ORS_API_KEY}" ]; then
    echo -e "${YELLOW}Updating OpenRouteService API key...${NC}"
    aws secretsmanager update-secret \
      --secret-id smartscheduler/api-keys/openrouteservice \
      --secret-string "{\"ApiKey\":\"${ORS_API_KEY}\"}" >/dev/null
    echo -e "${GREEN}OpenRouteService API key updated.${NC}"
else
    echo -e "${YELLOW}Skipping OpenRouteService API key.${NC}"
fi

# Google Places API key
read -p "Enter Google Places API key (or press Enter to skip): " GOOGLE_PLACES_KEY
if [ -n "${GOOGLE_PLACES_KEY}" ]; then
    echo -e "${YELLOW}Updating Google Places API key...${NC}"
    aws secretsmanager update-secret \
      --secret-id smartscheduler/api-keys/google-places \
      --secret-string "{\"ApiKey\":\"${GOOGLE_PLACES_KEY}\"}" >/dev/null
    echo -e "${GREEN}Google Places API key updated.${NC}"
else
    echo -e "${YELLOW}Skipping Google Places API key.${NC}"
fi

echo -e "${GREEN}Secrets configuration complete!${NC}"

