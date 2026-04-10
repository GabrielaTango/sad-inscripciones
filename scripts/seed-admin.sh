#!/bin/bash
# Seed admin user via the API
# Usage: ./scripts/seed-admin.sh [API_URL]

API_URL="${1:-http://localhost:5161}"

echo "Seeding admin user at $API_URL..."

# Login attempt - if it works, admin already exists
STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}')

if [ "$STATUS" = "200" ]; then
  echo "Admin user already exists. Login OK."
else
  echo "Admin user does not exist or different password."
  echo "The admin user is auto-created when the backend starts."
  echo "Just run: dotnet run (from backend/SAD.Inscripciones.API/)"
  echo "The SeedAdminAsync() method creates user 'admin' with password 'admin123'."
fi
