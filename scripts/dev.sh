#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

pids=()

start_bg() {
  local name="$1"
  shift
  echo "▶️  Starting: $name"
  ( "$@" ) &
  pids+=($!)
}

cleanup() {
  echo ""
  echo "🛑 Stopping processes..."
  for pid in "${pids[@]:-}"; do
    kill "$pid" >/dev/null 2>&1 || true
  done
  wait >/dev/null 2>&1 || true
  echo "✅ Stopped."
}
trap cleanup INT TERM EXIT

# Start Settings first (other services depend on it)
[ -d "services/settings/src/Settings.Api" ] && start_bg "Settings.Api" dotnet watch --project services/settings/src/Settings.Api run

# Start Platform API (for tenant/membership validation)
[ -d "services/platform/src/Platform.Api" ] && start_bg "Platform.Api" dotnet watch --project services/platform/src/Platform.Api run

# Wait a bit for Settings and Platform to start
sleep 3

[ -d "apps/gateway" ] && start_bg "Gateway" dotnet watch --project apps/gateway run
[ -d "services/catalog/src/Catalog.Api" ] && start_bg "Catalog.Api" dotnet watch --project services/catalog/src/Catalog.Api run
[ -d "services/planning/src/Planning.Api" ] && start_bg "Planning.Api" dotnet watch --project services/planning/src/Planning.Api run
[ -d "services/notifications/src/Notifications.Worker" ] && start_bg "Notifications.Worker" dotnet watch --project services/notifications/src/Notifications.Worker run

[ -d "apps/web" ] && start_bg "Web" bash -lc "cd apps/web && pnpm dev"
[ -d "apps/admin" ] && start_bg "Admin" bash -lc "cd apps/admin && pnpm dev"

echo ""
echo "✅ Dev environment started."
echo ""
echo "Authentication (Keycloak):"
echo "   Admin Console: http://localhost:8180 (admin/admin)"
echo "   Test Users:"
echo "     super@calendarun.local / super123 (super_admin role)"
echo "     demo@calendarun.local / demo123 (user role)"
echo ""
echo "Frontend:"
echo "   Web App:       http://localhost:3000"
echo ""
echo "Services:"
echo "   Gateway:       http://localhost:8080"
echo "   Platform API:  http://localhost:5401  (tenants, memberships)"
echo "   Settings API:  http://localhost:5301"
echo "   Catalog API:   http://localhost:5101"
echo "   Planning API:  http://localhost:5201"
echo ""
echo "Infrastructure:"
echo "   Mailhog:       http://localhost:8025"
echo "   RabbitMQ:      http://localhost:15672 (guest/guest)"
echo ""
wait
