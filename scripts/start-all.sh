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

# Check requirements
check_requirements() {
  print_step "Gereksinimleri kontrol ediliyor..."
  
  local missing=()
  
  command -v docker >/dev/null 2>&1 || missing+=("docker")
  command -v dotnet >/dev/null 2>&1 || missing+=("dotnet")
  command -v node >/dev/null 2>&1 || missing+=("node")
  command -v pnpm >/dev/null 2>&1 || missing+=("pnpm (npm install -g pnpm)")
  
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
  for port in 3000 8080 5101 5201 5301 5401; do
    kill_port $port
  done
  
  print_step "Docker container'ları durduruluyor..."
  docker compose -f infra/docker-compose.yml --env-file infra/local.env down 2>/dev/null || true
  
  print_success "Tüm servisler durduruldu"
}

# Start Docker infrastructure
start_docker() {
  print_step "Docker altyapısı başlatılıyor..."
  
  if $FRESH; then
    print_warning "Fresh mode: Volume'lar siliniyor..."
    docker compose -f infra/docker-compose.yml --env-file infra/local.env down -v 2>/dev/null || true
  fi
  
  # Start Docker with env file
  docker compose -f infra/docker-compose.yml --env-file infra/local.env up -d 2>&1 | grep -v "is already in use" || true
  
  # Wait for PostgreSQL
  print_step "PostgreSQL bağlantısı bekleniyor..."
  local max_wait=60
  local waited=0
  
  while ! docker compose -f infra/docker-compose.yml --env-file infra/local.env exec -T postgres pg_isready -U postgres >/dev/null 2>&1; do
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
    docker compose -f infra/docker-compose.yml --env-file infra/local.env exec -T postgres \
      psql -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = '$db'" | grep -q 1 || \
      docker compose -f infra/docker-compose.yml --env-file infra/local.env exec -T postgres \
      psql -U postgres -c "CREATE DATABASE $db;" 2>/dev/null || true
  done
  print_success "Database'ler hazır"
  
  # Wait for Redis
  print_step "Redis bağlantısı bekleniyor..."
  waited=0
  while ! docker compose -f infra/docker-compose.yml --env-file infra/local.env exec -T redis redis-cli ping >/dev/null 2>&1; do
    if [ $waited -ge 30 ]; then
      print_warning "Redis hala hazır değil, devam ediliyor..."
      break
    fi
    sleep 1
    waited=$((waited + 1))
  done
  print_success "Redis hazır"
  
  # Wait for Keycloak
  print_step "Keycloak bağlantısı bekleniyor (bu biraz sürebilir)..."
  waited=0
  max_wait=120
  
  while ! curl -sf http://localhost:8180/health/ready >/dev/null 2>&1; do
    if [ $waited -ge $max_wait ]; then
      print_warning "Keycloak hala hazır değil, devam ediliyor..."
      break
    fi
    sleep 3
    waited=$((waited + 3))
    echo -ne "\r  Bekleniyor... ${waited}s / ${max_wait}s"
  done
  echo ""
  
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
  
  # Start Platform API first (needed for membership validation)
  if [ -d "services/platform/src/Platform.Api" ]; then
    echo "  → Platform API (:5401)..."
    kill_port 5401
    nohup dotnet run --project services/platform/src/Platform.Api --no-build --no-launch-profile > .logs/platform.log 2>&1 &
    sleep 3
  fi
  
  # Start Catalog API
  if [ -d "services/catalog/src/Catalog.Api" ]; then
    echo "  → Catalog API (:5101)..."
    kill_port 5101
    nohup dotnet run --project services/catalog/src/Catalog.Api --no-build --no-launch-profile > .logs/catalog.log 2>&1 &
  fi
  
  # Start Planning API
  if [ -d "services/planning/src/Planning.Api" ]; then
    echo "  → Planning API (:5201)..."
    kill_port 5201
    nohup dotnet run --project services/planning/src/Planning.Api --no-build --no-launch-profile > .logs/planning.log 2>&1 &
  fi
  
  # Wait for APIs to start
  sleep 5
  
  # Start Gateway
  if [ -d "apps/gateway" ]; then
    echo "  → Gateway (:8080)..."
    kill_port 8080
    nohup dotnet run --project apps/gateway --no-build --no-launch-profile > .logs/gateway.log 2>&1 &
  fi
  
  # Wait for Gateway
  sleep 3
  
  print_success "Backend servisleri başlatıldı"
  
  # Start Frontend
  if [ -d "apps/web" ]; then
    print_step "Frontend başlatılıyor..."
    kill_port 3000
    (cd apps/web && nohup pnpm dev > ../../.logs/web.log 2>&1 &)
    sleep 3
    print_success "Frontend başlatıldı"
  fi
}

# Verify services are running
verify_services() {
  print_step "Servisler kontrol ediliyor..."
  
  local all_ok=true
  
  # Check Gateway
  if curl -sf http://localhost:8080/health >/dev/null 2>&1; then
    echo -e "  ${GREEN}✓${NC} Gateway (:8080)"
  else
    echo -e "  ${RED}✗${NC} Gateway (:8080)"
    all_ok=false
  fi
  
  # Check Frontend
  sleep 2
  if curl -sf http://localhost:3000 >/dev/null 2>&1; then
    echo -e "  ${GREEN}✓${NC} Frontend (:3000)"
  else
    echo -e "  ${YELLOW}⋯${NC} Frontend (:3000) - başlatılıyor..."
  fi
  
  # Check API endpoints
  if curl -sf http://localhost:8080/api/events >/dev/null 2>&1; then
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
  echo "   http://localhost:3000"
  echo ""
  
  echo -e "${YELLOW}🔐 Keycloak${NC}"
  echo "   http://localhost:8180"
  echo "   Admin: admin / admin"
  echo ""
  
  echo -e "${BLUE}🔌 API Gateway${NC}"
  echo "   http://localhost:8080"
  echo "   Health: http://localhost:8080/health"
  echo ""
  
  echo -e "${CYAN}📧 Mailhog${NC}"
  echo "   http://localhost:8025"
  echo ""
  
  echo -e "${CYAN}🐰 RabbitMQ${NC}"
  echo "   http://localhost:15672"
  echo "   User: guest / guest"
  echo ""
  
  echo -e "${CYAN}📊 Grafana${NC}"
  echo "   http://localhost:3001"
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
  if $STOP_ONLY; then
    stop_all
    exit 0
  fi
  
  print_header "Sistem Başlatılıyor"
  
  check_requirements
  stop_all 2>/dev/null || true
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
