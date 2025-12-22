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

[ -d "apps/gateway" ] && start_bg "Gateway" dotnet watch --project apps/gateway run
[ -d "services/catalog/src/Catalog.Api" ] && start_bg "Catalog.Api" dotnet watch --project services/catalog/src/Catalog.Api run
[ -d "services/planning/src/Planning.Api" ] && start_bg "Planning.Api" dotnet watch --project services/planning/src/Planning.Api run
[ -d "services/notifications/src/Notifications.Worker" ] && start_bg "Notifications.Worker" dotnet watch --project services/notifications/src/Notifications.Worker run

[ -d "apps/web" ] && start_bg "Web" bash -lc "cd apps/web && pnpm dev"
[ -d "apps/admin" ] && start_bg "Admin" bash -lc "cd apps/admin && pnpm dev"

echo ""
echo "✅ Dev environment started."
echo "   Mailhog: http://localhost:8025"
echo "   RabbitMQ: http://localhost:15672 (guest/guest)"
echo "   Grafana: http://localhost:3000"
echo ""
wait

