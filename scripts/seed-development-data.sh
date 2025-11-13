#!/bin/bash

# Script to seed the database with development data
# Usage: ./scripts/seed-development-data.sh

set -e

echo "Seeding database with development data..."

cd "$(dirname "$0")/../src/SmartScheduler.Api"

# Run the seeder (this will be implemented as a CLI command or endpoint)
# For now, this is a placeholder that will be implemented once domain entities exist
dotnet run -- seed-development

echo "Development data seeded successfully!"


