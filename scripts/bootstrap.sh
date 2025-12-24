#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "❌ Missing command: $1"
    exit 1
  }
}

echo "🔎 Checking prerequisites..."
require_cmd docker
require_cmd dotnet
require_cmd node
require_cmd pnpm
require_cmd jq
require_cmd dotnet-ef
echo "✅ Prerequisites OK"

echo "🐳 Starting infra (docker compose)..."
cd "$ROOT_DIR/infra"
docker compose -f docker-compose.yml --env-file "$ROOT_DIR/infra/local.env" up -d

echo "⏳ Waiting for postgres to accept connections..."
POSTGRES_CONTAINER_ID="$(docker ps -q --filter name=postgres | head -n 1)"
if [ -z "${POSTGRES_CONTAINER_ID}" ]; then
  echo "❌ Postgres container not found. Is docker compose up successful?"
  exit 1
fi

for i in {1..60}; do
  if docker exec "$POSTGRES_CONTAINER_ID" pg_isready -U postgres >/dev/null 2>&1; then
    echo "✅ Postgres is ready"
    break
  fi
  sleep 1
  if [ "$i" -eq 60 ]; then
    echo "❌ Postgres not ready after 60s"
    exit 1
  fi
done

echo "⏳ Waiting for Keycloak to be ready..."
for i in {1..120}; do
  if curl -sf http://localhost:8180/health/ready >/dev/null 2>&1; then
    echo "✅ Keycloak is ready"
    break
  fi
  sleep 2
  if [ "$i" -eq 120 ]; then
    echo "⚠️ Keycloak not ready after 240s (continuing anyway)"
  fi
done

cd "$ROOT_DIR"

echo "📦 Restoring .NET (if .sln exists)..."
if ls *.sln >/dev/null 2>&1; then
  dotnet restore
else
  echo "⚠️ No .sln found yet. Skipping dotnet restore at root."
fi

echo "📦 Installing web dependencies (pnpm)..."
if [ -d "apps/web" ]; then
  (cd apps/web && pnpm install)
else
  echo "⚠️ apps/web not found yet. Skipping pnpm install."
fi

if [ -d "apps/admin" ]; then
  (cd apps/admin && pnpm install)
fi

echo "🗄️ Applying EF migrations (if projects exist)..."
apply_migration() {
  local infra_proj="$1"
  local startup_proj="$2"
  if [ -d "$infra_proj" ] && [ -d "$startup_proj" ]; then
    echo "➡️  Migrating: $startup_proj"
    dotnet ef database update --project "$infra_proj" --startup-project "$startup_proj"
  else
    echo "⚠️ Skipping migrations for missing path: $infra_proj / $startup_proj"
  fi
}

apply_migration "services/catalog/src/Catalog.Infrastructure" "services/catalog/src/Catalog.Api"
apply_migration "services/planning/src/Planning.Infrastructure" "services/planning/src/Planning.Api"
apply_migration "services/notifications/src/Notifications.Infrastructure" "services/notifications/src/Notifications.Worker"
apply_migration "services/settings/src/Settings.Infrastructure" "services/settings/src/Settings.Api"
apply_migration "services/platform/src/Platform.Infrastructure" "services/platform/src/Platform.Api"

echo "✅ Bootstrap complete."
echo ""
echo "Next:"
echo "  ./scripts/dev.sh"
echo ""
echo "Useful UIs:"
echo "  Keycloak:  http://localhost:8180 (admin/admin)"
echo "  Mailhog:   http://localhost:8025"
echo "  RabbitMQ:  http://localhost:15672 (guest/guest)"
echo "  Grafana:   http://localhost:3000"
echo ""
echo "Services:"
echo "  Gateway:           http://localhost:8080"
echo "  Catalog API:       http://localhost:5101"
echo "  Planning API:      http://localhost:5201"
echo "  Settings API:      http://localhost:5301"
echo "  Platform API:      http://localhost:5401"
echo ""
echo "Test Users (Keycloak):"
echo "  Super Admin: superadmin@calendarun.local / admin123"
echo "  Demo User:   demo@calendarun.local / demo123"
