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

# Wait a bit for Settings to start
sleep 2

[ -d "apps/gateway" ] && start_bg "Gateway" dotnet watch --project apps/gateway run
[ -d "services/catalog/src/Catalog.Api" ] && start_bg "Catalog.Api" dotnet watch --project services/catalog/src/Catalog.Api run
[ -d "services/planning/src/Planning.Api" ] && start_bg "Planning.Api" dotnet watch --project services/planning/src/Planning.Api run
[ -d "services/notifications/src/Notifications.Worker" ] && start_bg "Notifications.Worker" dotnet watch --project services/notifications/src/Notifications.Worker run

[ -d "apps/web" ] && start_bg "Web" bash -lc "cd apps/web && pnpm dev"
[ -d "apps/admin" ] && start_bg "Admin" bash -lc "cd apps/admin && pnpm dev"

echo ""
echo "✅ Dev environment started."
echo ""
echo "Services:"
echo "   Settings API:  http://localhost:5301  (admin settings)"
echo "   Gateway:       http://localhost:8080"
echo "   Catalog API:   http://localhost:5101"
echo "   Planning API:  http://localhost:5201"
echo ""
echo "Infrastructure:"
echo "   Mailhog:       http://localhost:8025"
echo "   RabbitMQ:      http://localhost:15672 (guest/guest)"
echo "   Grafana:       http://localhost:3000"
echo ""
wait
