#!/usr/bin/env bash
#
# 🏃 CalendaRun - Tüm Sistemi Tek Komutla Başlat
#
# Kullanım:
#   ./scripts/start-all.sh          # Tüm sistemi başlat
#   ./scripts/start-all.sh --fresh  # Sıfırdan başlat (volume'ları sil)
#   ./scripts/start-all.sh --stop   # Tüm servisleri durdur
#
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

COMPOSE_FILE="$ROOT_DIR/infra/docker-compose.yml"
ENV_FILE="$ROOT_DIR/infra/local.env"
ENV_SAMPLE="$ROOT_DIR/infra/local.env.sample"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Flags
FRESH=false
STOP_ONLY=false

# Parse arguments
for arg in "$@"; do
  case $arg in
    --fresh)
      FRESH=true
      shift
      ;;
    --stop)
      STOP_ONLY=true
      shift
      ;;
  esac
done

print_header() {
  echo ""
  echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
  echo -e "${CYAN}  🏃 CalendaRun - $1${NC}"
  echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
  echo ""
}

print_step() {
  echo -e "${BLUE}▶ $1${NC}"
}

print_success() {
  echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
  echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
  echo -e "${RED}❌ $1${NC}"
}

compose() {
  docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" "$@"
}

load_env() {
  if [ ! -f "$ENV_FILE" ]; then
    if [ -f "$ENV_SAMPLE" ]; then
      print_warning "infra/local.env bulunamadı, örnek dosya kopyalanıyor..."
      cp "$ENV_SAMPLE" "$ENV_FILE"
    else
      print_error "infra/local.env bulunamadı ve örnek dosya yok."
      exit 1
    fi
  fi

  set -a
  # shellcheck source=/dev/null
  source "$ENV_FILE"
  set +a

  POSTGRES_PORT="${POSTGRES_PORT:-55432}"
  REDIS_PORT="${REDIS_PORT:-6379}"
  RABBITMQ_PORT="${RABBITMQ_PORT:-5672}"
  RABBITMQ_MGMT_PORT="${RABBITMQ_MGMT_PORT:-15672}"
  RABBITMQ_USER="${RABBITMQ_USER:-guest}"
  RABBITMQ_PASS="${RABBITMQ_PASS:-guest}"
  MAILHOG_SMTP_PORT="${MAILHOG_SMTP_PORT:-1025}"
  MAILHOG_UI_PORT="${MAILHOG_UI_PORT:-8025}"
  OTEL_GRPC_PORT="${OTEL_GRPC_PORT:-4317}"
  OTEL_HTTP_PORT="${OTEL_HTTP_PORT:-4318}"
  PROMETHEUS_PORT="${PROMETHEUS_PORT:-9090}"
  GRAFANA_PORT="${GRAFANA_PORT:-3001}"
  KEYCLOAK_PORT="${KEYCLOAK_PORT:-8180}"
  KEYCLOAK_ADMIN="${KEYCLOAK_ADMIN:-admin}"
  KEYCLOAK_ADMIN_PASSWORD="${KEYCLOAK_ADMIN_PASSWORD:-admin}"
  KEYCLOAK_REALM="${KEYCLOAK_REALM:-calendarun}"
  GATEWAY_PORT="${GATEWAY_PORT:-8080}"
  CATALOG_PORT="${CATALOG_PORT:-5101}"
  PLANNING_PORT="${PLANNING_PORT:-5201}"
  SETTINGS_PORT="${SETTINGS_PORT:-5301}"
  PLATFORM_PORT="${PLATFORM_PORT:-5401}"
  FRONTEND_PORT="${FRONTEND_PORT:-3000}"

  if [ "$GRAFANA_PORT" = "$FRONTEND_PORT" ]; then
    print_error "GRAFANA_PORT ($GRAFANA_PORT) frontend portuyla çakışıyor. infra/local.env içinde GRAFANA_PORT'u 3001 gibi boş bir porta al."
    exit 1
  fi
}

ensure_docker_running() {
  print_step "Docker daemon kontrol ediliyor..."

  if docker info >/dev/null 2>&1; then
    print_success "Docker hazır"
    return
  fi

  if command -v open >/dev/null 2>&1 && [[ "$OSTYPE" == "darwin"* ]]; then
    print_warning "Docker çalışmıyor, Docker Desktop açılıyor..."
    open -a Docker >/dev/null 2>&1 || true
  fi

  local max_wait=60
  local waited=0

  while ! docker info >/dev/null 2>&1; do
    if [ $waited -ge $max_wait ]; then
      print_error "Docker daemon başlatılamadı. Docker Desktop açık mı?"
      exit 1
    fi
    sleep 2
    waited=$((waited + 2))
    echo -ne "\r  Bekleniyor... ${waited}s / ${max_wait}s"
  done
  echo ""
  print_success "Docker hazır"
}

# Check requirements
check_requirements() {
  print_step "Gereksinimleri kontrol ediliyor..."
  
  local missing=()
  
  command -v docker >/dev/null 2>&1 || missing+=("docker")
  docker compose version >/dev/null 2>&1 || missing+=("docker compose (Docker Compose 2+)")
  command -v dotnet >/dev/null 2>&1 || missing+=("dotnet")
  command -v node >/dev/null 2>&1 || missing+=("node")
  command -v pnpm >/dev/null 2>&1 || missing+=("pnpm (npm install -g pnpm)")
  command -v curl >/dev/null 2>&1 || missing+=("curl")
  command -v lsof >/dev/null 2>&1 || missing+=("lsof")
  
  if [ ${#missing[@]} -ne 0 ]; then
    print_error "Eksik araçlar: ${missing[*]}"
    exit 1
  fi
  
  print_success "Tüm gereksinimler mevcut"
}

# Check if port is in use
check_port() {
  local port=$1
  if lsof -i :$port >/dev/null 2>&1; then
    return 0  # Port in use
  else
    return 1  # Port free
  fi
}

check_infra_ports() {
  print_step "Port çakışmaları kontrol ediliyor..."

  local conflicts=()
  local entries=(
    "PostgreSQL:$POSTGRES_PORT"
    "Keycloak:$KEYCLOAK_PORT"
    "Redis:$REDIS_PORT"
    "RabbitMQ:$RABBITMQ_PORT"
    "RabbitMQ Mgmt:$RABBITMQ_MGMT_PORT"
    "Mailhog SMTP:$MAILHOG_SMTP_PORT"
    "Mailhog UI:$MAILHOG_UI_PORT"
    "OTel gRPC:$OTEL_GRPC_PORT"
    "OTel HTTP:$OTEL_HTTP_PORT"
    "Prometheus:$PROMETHEUS_PORT"
    "Grafana:$GRAFANA_PORT"
  )

  for entry in "${entries[@]}"; do
    local name="${entry%%:*}"
    local port="${entry##*:}"
    if check_port "$port"; then
      conflicts+=("$name:$port")
    fi
  done

  if [ ${#conflicts[@]} -ne 0 ]; then
    print_error "Port çakışması var: ${conflicts[*]}"
    exit 1
  fi

  print_success "Portlar uygun"
}

# Kill process on port
kill_port() {
  local port=$1
  local pids=$(lsof -ti :$port 2>/dev/null || true)
  if [ -n "$pids" ]; then
    echo "$pids" | xargs kill -9 2>/dev/null || true
    sleep 1
  fi
}

# Stop all processes
stop_all() {
  print_header "Servisleri Durdurma"
  
  print_step "Backend process'leri durduruluyor..."
  pkill -f "dotnet.*Platform.Api" 2>/dev/null || true
  pkill -f "dotnet.*Catalog.Api" 2>/dev/null || true
  pkill -f "dotnet.*Planning.Api" 2>/dev/null || true
  pkill -f "dotnet.*Gateway" 2>/dev/null || true
  pkill -f "dotnet.*Settings.Api" 2>/dev/null || true
  
  print_step "Frontend process'i durduruluyor..."
  pkill -f "next-server" 2>/dev/null || true
  pkill -f "next dev" 2>/dev/null || true
  
  # Kill specific ports if still in use
  local ports=("$FRONTEND_PORT" "$GATEWAY_PORT" "$CATALOG_PORT" "$PLANNING_PORT" "$SETTINGS_PORT" "$PLATFORM_PORT")
  for port in "${ports[@]}"; do
    kill_port "$port"
  done
  
  print_step "Docker container'ları durduruluyor..."
  compose down 2>/dev/null || true
  
  print_success "Tüm servisler durduruldu"
}

wait_for_keycloak() {
  print_step "Keycloak bağlantısı bekleniyor (bu biraz sürebilir)..."

  local max_wait=180
  local waited=0

  while true; do
    local container_id=""
    local status=""

    container_id=$(compose ps -q keycloak 2>/dev/null || true)
    if [ -n "$container_id" ]; then
      status=$(docker inspect -f '{{.State.Status}}' "$container_id" 2>/dev/null || true)
      if [ "$status" = "exited" ] || [ "$status" = "dead" ]; then
        print_error "Keycloak container'ı beklenmedik şekilde durdu."
        compose logs --tail 200 keycloak || true
        exit 1
      fi
    fi

    if curl -sf "http://localhost:${KEYCLOAK_PORT}/health/ready" >/dev/null 2>&1; then
      if curl -sf "http://localhost:${KEYCLOAK_PORT}/realms/${KEYCLOAK_REALM}/.well-known/openid-configuration" >/dev/null 2>&1; then
        print_success "Keycloak hazır"
        return 0
      fi
      print_error "Keycloak hazır ama realm (${KEYCLOAK_REALM}) bulunamadı."
      compose logs --tail 200 keycloak || true
      exit 1
    fi

    if [ $waited -ge $max_wait ]; then
      print_error "Keycloak hazır olamadı!"
      compose logs --tail 200 keycloak || true
      exit 1
    fi

    sleep 3
    waited=$((waited + 3))
    echo -ne "\r  Bekleniyor... ${waited}s / ${max_wait}s"
  done
}

# Start Docker infrastructure
start_docker() {
  print_step "Docker altyapısı başlatılıyor..."
  
  if $FRESH; then
    print_warning "Fresh mode: Volume'lar siliniyor..."
    compose down -v 2>/dev/null || true
  fi
  
  print_step "PostgreSQL başlatılıyor..."
  compose up -d postgres
  
  # Wait for PostgreSQL
  print_step "PostgreSQL bağlantısı bekleniyor..."
  local max_wait=60
  local waited=0
  
  while ! compose exec -T postgres pg_isready -U postgres >/dev/null 2>&1; do
    if [ $waited -ge $max_wait ]; then
      print_error "PostgreSQL başlatılamadı!"
      exit 1
    fi
    sleep 2
    waited=$((waited + 2))
    echo -ne "\r  Bekleniyor... ${waited}s / ${max_wait}s"
  done
  echo ""
  print_success "PostgreSQL hazır"
  
  # Create databases if they don't exist
  print_step "Database'ler kontrol ediliyor..."
  local dbs=("catalogdb" "planningdb" "platformdb" "settingsdb" "notificationsdb" "keycloakdb")
  for db in "${dbs[@]}"; do
    compose exec -T postgres \
      psql -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = '$db'" | grep -q 1 || \
      compose exec -T postgres \
      psql -U postgres -c "CREATE DATABASE $db;" 2>/dev/null || true
  done
  print_success "Database'ler hazır"
  
  print_step "Diğer altyapı servisleri başlatılıyor..."
  compose up -d redis rabbitmq mailhog otel-collector prometheus grafana
  
  # Wait for Redis
  print_step "Redis bağlantısı bekleniyor..."
  waited=0
  while ! compose exec -T redis redis-cli ping >/dev/null 2>&1; do
    if [ $waited -ge 30 ]; then
      print_warning "Redis hala hazır değil, devam ediliyor..."
      break
    fi
    sleep 1
    waited=$((waited + 1))
  done
  print_success "Redis hazır"
  
  print_step "Keycloak başlatılıyor..."
  compose up -d keycloak
  wait_for_keycloak
  
  print_success "Docker altyapısı hazır"
}

# Build all services
build_services() {
  print_step "Backend servisleri derleniyor..."
  
  if dotnet build CalendaRun.sln --configuration Release --verbosity quiet 2>/dev/null; then
    print_success "Build tamamlandı"
  else
    print_warning "Release build başarısız, Debug deneniyor..."
    dotnet build CalendaRun.sln --verbosity quiet || {
      print_error "Build başarısız!"
      exit 1
    }
    print_success "Build tamamlandı (Debug)"
  fi
}

# Run migrations
run_migrations() {
  print_step "Database migration'ları uygulanıyor..."
  
  # Platform
  if [ -d "services/platform/src/Platform.Infrastructure" ]; then
    echo "  → Platform DB..."
    dotnet ef database update \
      --project services/platform/src/Platform.Infrastructure \
      --startup-project services/platform/src/Platform.Api \
      --no-build 2>&1 | grep -E "(Applying|Done|error)" || true
  fi
  
  # Catalog
  if [ -d "services/catalog/src/Catalog.Infrastructure" ]; then
    echo "  → Catalog DB..."
    dotnet ef database update \
      --project services/catalog/src/Catalog.Infrastructure \
      --startup-project services/catalog/src/Catalog.Api \
      --no-build 2>&1 | grep -E "(Applying|Done|error)" || true
  fi
  
  # Planning
  if [ -d "services/planning/src/Planning.Infrastructure" ]; then
    echo "  → Planning DB..."
    dotnet ef database update \
      --project services/planning/src/Planning.Infrastructure \
      --startup-project services/planning/src/Planning.Api \
      --no-build 2>&1 | grep -E "(Applying|Done|error)" || true
  fi

  # Settings
  if [ -d "services/settings/src/Settings.Infrastructure" ]; then
    echo "  → Settings DB..."
    dotnet ef database update \
      --project services/settings/src/Settings.Infrastructure \
      --startup-project services/settings/src/Settings.Api \
      --no-build 2>&1 | grep -E "(Applying|Done|error)" || true
  fi
  
  print_success "Migration'lar tamamlandı"
}

# Install frontend dependencies
install_frontend() {
  print_step "Frontend bağımlılıkları kontrol ediliyor..."
  
  if [ -d "apps/web" ]; then
    if [ ! -d "apps/web/node_modules" ]; then
      print_step "Frontend bağımlılıkları yükleniyor..."
      (cd apps/web && pnpm install --frozen-lockfile 2>/dev/null || pnpm install)
    fi
    print_success "Frontend hazır"
  fi
}

# Start all services
start_services() {
  print_step "Backend servisleri başlatılıyor..."
  
  # Create log directory
  mkdir -p .logs
  
  # Set environment
  export ASPNETCORE_ENVIRONMENT=Development
  
  # Start Settings API
  if [ -d "services/settings/src/Settings.Api" ]; then
    echo "  → Settings API (:$SETTINGS_PORT)..."
    kill_port "$SETTINGS_PORT"
    nohup dotnet run --project services/settings/src/Settings.Api --no-build --no-launch-profile > .logs/settings.log 2>&1 &
    sleep 2
  fi
  
  # Start Platform API first (needed for membership validation)
  if [ -d "services/platform/src/Platform.Api" ]; then
    echo "  → Platform API (:$PLATFORM_PORT)..."
    kill_port "$PLATFORM_PORT"
    nohup dotnet run --project services/platform/src/Platform.Api --no-build --no-launch-profile > .logs/platform.log 2>&1 &
    sleep 3
  fi
  
  # Start Catalog API
  if [ -d "services/catalog/src/Catalog.Api" ]; then
    echo "  → Catalog API (:$CATALOG_PORT)..."
    kill_port "$CATALOG_PORT"
    nohup dotnet run --project services/catalog/src/Catalog.Api --no-build --no-launch-profile > .logs/catalog.log 2>&1 &
  fi
  
  # Start Planning API
  if [ -d "services/planning/src/Planning.Api" ]; then
    echo "  → Planning API (:$PLANNING_PORT)..."
    kill_port "$PLANNING_PORT"
    nohup dotnet run --project services/planning/src/Planning.Api --no-build --no-launch-profile > .logs/planning.log 2>&1 &
  fi
  
  # Wait for APIs to start
  sleep 5
  
  # Start Gateway
  if [ -d "apps/gateway" ]; then
    echo "  → Gateway (:$GATEWAY_PORT)..."
    kill_port "$GATEWAY_PORT"
    nohup dotnet run --project apps/gateway --no-build --no-launch-profile > .logs/gateway.log 2>&1 &
  fi
  
  # Wait for Gateway
  sleep 3
  
  print_success "Backend servisleri başlatıldı"
  
  # Start Frontend
  if [ -d "apps/web" ]; then
    print_step "Frontend başlatılıyor..."
    kill_port "$FRONTEND_PORT"
    (cd apps/web && PORT="$FRONTEND_PORT" nohup pnpm dev > ../../.logs/web.log 2>&1 &)
    sleep 3
    print_success "Frontend başlatıldı"
  fi
}

# Verify services are running
verify_services() {
  print_step "Servisler kontrol ediliyor..."
  
  local all_ok=true
  
  check_health() {
    local label=$1
    local url=$2
    if curl -sf "$url" >/dev/null 2>&1; then
      echo -e "  ${GREEN}✓${NC} ${label}"
    else
      echo -e "  ${RED}✗${NC} ${label}"
      all_ok=false
    fi
  }
  
  check_health "Gateway (:$GATEWAY_PORT)" "http://localhost:${GATEWAY_PORT}/health"
  check_health "Platform API (:$PLATFORM_PORT)" "http://localhost:${PLATFORM_PORT}/health"
  check_health "Catalog API (:$CATALOG_PORT)" "http://localhost:${CATALOG_PORT}/health"
  check_health "Planning API (:$PLANNING_PORT)" "http://localhost:${PLANNING_PORT}/health"
  check_health "Settings API (:$SETTINGS_PORT)" "http://localhost:${SETTINGS_PORT}/health"
  check_health "Keycloak (:$KEYCLOAK_PORT)" "http://localhost:${KEYCLOAK_PORT}/health/ready"
  
  # Check Frontend
  sleep 2
  if curl -sf "http://localhost:${FRONTEND_PORT}" >/dev/null 2>&1; then
    echo -e "  ${GREEN}✓${NC} Frontend (:$FRONTEND_PORT)"
  else
    echo -e "  ${YELLOW}⋯${NC} Frontend (:$FRONTEND_PORT) - başlatılıyor..."
  fi
  
  # Check API endpoints
  if curl -sf "http://localhost:${GATEWAY_PORT}/api/events" >/dev/null 2>&1; then
    echo -e "  ${GREEN}✓${NC} Events API"
  else
    echo -e "  ${YELLOW}⋯${NC} Events API - başlatılıyor..."
  fi
  
  if $all_ok; then
    print_success "Tüm servisler çalışıyor"
  fi
}

# Print URLs
print_urls() {
  print_header "Erişim Bilgileri"
  
  echo -e "${GREEN}🌐 Web UI${NC}"
  echo "   http://localhost:$FRONTEND_PORT"
  echo ""
  
  echo -e "${YELLOW}🔐 Keycloak${NC}"
  echo "   http://localhost:$KEYCLOAK_PORT"
  echo "   Admin: $KEYCLOAK_ADMIN / $KEYCLOAK_ADMIN_PASSWORD"
  echo ""
  
  echo -e "${BLUE}🔌 API Gateway${NC}"
  echo "   http://localhost:$GATEWAY_PORT"
  echo "   Health: http://localhost:$GATEWAY_PORT/health"
  echo ""
  
  echo -e "${CYAN}📧 Mailhog${NC}"
  echo "   http://localhost:$MAILHOG_UI_PORT"
  echo ""
  
  echo -e "${CYAN}🐰 RabbitMQ${NC}"
  echo "   http://localhost:$RABBITMQ_MGMT_PORT"
  echo "   User: $RABBITMQ_USER / $RABBITMQ_PASS"
  echo ""
  
  echo -e "${CYAN}📊 Grafana${NC}"
  echo "   http://localhost:$GRAFANA_PORT"
  echo ""
  
  echo -e "${GREEN}👤 Test Kullanıcıları${NC}"
  echo "   ┌──────────────────────────┬───────────┬──────────────┐"
  echo "   │ Email                    │ Şifre     │ Rol          │"
  echo "   ├──────────────────────────┼───────────┼──────────────┤"
  echo "   │ super@calendarun.local   │ super123  │ Super Admin  │"
  echo "   │ admin@tenant1.local      │ admin123  │ Tenant Admin │"
  echo "   │ demo@calendarun.local    │ demo123   │ User         │"
  echo "   └──────────────────────────┴───────────┴──────────────┘"
  echo ""
  
  echo -e "${YELLOW}📝 Logları görüntülemek için:${NC}"
  echo "   tail -f .logs/gateway.log"
  echo "   tail -f .logs/catalog.log"
  echo "   tail -f .logs/settings.log"
  echo "   tail -f .logs/platform.log"
  echo "   tail -f .logs/planning.log"
  echo "   tail -f .logs/web.log"
  echo ""
  
  echo -e "${RED}🛑 Durdurmak için:${NC}"
  echo "   ./scripts/start-all.sh --stop"
  echo ""
}

# Wait for user interrupt
wait_for_interrupt() {
  echo -e "${GREEN}✅ Sistem çalışıyor. Durdurmak için Ctrl+C${NC}"
  echo ""
  
  # Trap Ctrl+C
  trap 'echo ""; stop_all; exit 0' INT TERM
  
  # Keep script running
  while true; do
    sleep 1
  done
}

# Main
main() {
  load_env

  if $STOP_ONLY; then
    ensure_docker_running
    stop_all
    exit 0
  fi
  
  print_header "Sistem Başlatılıyor"
  
  check_requirements
  ensure_docker_running
  stop_all 2>/dev/null || true
  check_infra_ports
  start_docker
  build_services
  run_migrations
  install_frontend
  start_services
  verify_services
  print_urls
  wait_for_interrupt
}

main
