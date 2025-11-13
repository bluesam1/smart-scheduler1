#!/bin/bash
# Generate TypeScript API client
# This script builds the API, starts it, fetches the OpenAPI spec, generates the client, then stops the API

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "Step 1: Building API..."
dotnet build src/SmartScheduler.Api/SmartScheduler.Api.csproj

echo ""
echo "Step 2: Starting API server in background..."
cd src/SmartScheduler.Api
dotnet run --no-build > /tmp/smartscheduler-api.log 2>&1 &
API_PID=$!
cd "$PROJECT_ROOT"

# Function to cleanup on exit
cleanup() {
    if [ ! -z "$API_PID" ]; then
        echo ""
        echo "Stopping API server (PID: $API_PID)..."
        kill $API_PID 2>/dev/null || true
        wait $API_PID 2>/dev/null || true
    fi
}

# Set trap to cleanup on script exit
trap cleanup EXIT

# Wait for API to be ready
echo "Waiting for API to start..."
MAX_ATTEMPTS=30
ATTEMPT=0
API_READY=false

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    if curl -s -f http://localhost:5004/health > /dev/null 2>&1; then
        echo "API is ready!"
        API_READY=true
        break
    fi
    sleep 1
    ATTEMPT=$((ATTEMPT + 1))
done

if [ "$API_READY" = false ]; then
    echo "API failed to start after $MAX_ATTEMPTS attempts"
    echo "Check logs at /tmp/smartscheduler-api.log"
    exit 1
fi

echo ""
echo "Step 3: Fetching OpenAPI spec..."
if curl -s -f http://localhost:5004/swagger/v1/swagger.json -o openapi.json; then
    echo "OpenAPI spec saved to openapi.json"
else
    echo "Failed to fetch OpenAPI spec"
    exit 1
fi

echo ""
echo "Step 4: Generating TypeScript client from OpenAPI spec..."
nswag openapi2tsclient \
  /input:openapi.json \
  /output:frontend/lib/api/generated/api-client.ts \
  /template:Fetch \
  /typeStyle:Interface \
  /generateClientClasses:true \
  /generateOptionalParameters:true \
  /dateTimeType:string \
  /nullValue:Undefined \
  /withCredentials:true \
  /operationGenerationMode:MultipleClientsFromOperationId \
  /markOptionalProperties:true \
  /generateDefaultValues:true \
  /generateDtoTypes:true \
  /exportTypes:true

if [ $? -ne 0 ]; then
    echo "Failed to generate TypeScript client"
    exit 1
fi

echo ""
echo "Done! API client generated to frontend/lib/api/generated/api-client.ts"
