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

# Stop all processes
stop_all() {
  print_header "Servisleri Durdurma"
  
  print_step "Docker container'ları durduruluyor..."
  docker compose -f infra/docker-compose.yml down 2>/dev/null || true
  
  print_step "Arka plan process'leri durduruluyor..."
  pkill -f "dotnet.*Platform.Api" 2>/dev/null || true
  pkill -f "dotnet.*Catalog.Api" 2>/dev/null || true
  pkill -f "dotnet.*Planning.Api" 2>/dev/null || true
  pkill -f "dotnet.*Gateway" 2>/dev/null || true
  pkill -f "dotnet.*Settings.Api" 2>/dev/null || true
  pkill -f "next-server" 2>/dev/null || true
  
  print_success "Tüm servisler durduruldu"
}

# Start Docker infrastructure
start_docker() {
  print_step "Docker altyapısı başlatılıyor..."
  
  if $FRESH; then
    print_warning "Fresh mode: Volume'lar siliniyor..."
    docker compose -f infra/docker-compose.yml down -v 2>/dev/null || true
  fi
  
  docker compose -f infra/docker-compose.yml up -d
  
  # Wait for PostgreSQL
  print_step "PostgreSQL bağlantısı bekleniyor..."
  local max_wait=30
  local waited=0
  
  while ! docker compose -f infra/docker-compose.yml exec -T postgres pg_isready -U calendarun >/dev/null 2>&1; do
    if [ $waited -ge $max_wait ]; then
      print_warning "PostgreSQL hala hazır değil, devam ediliyor..."
      break
    fi
    sleep 1
    waited=$((waited + 1))
    echo -ne "\r  Bekleniyor... ${waited}s / ${max_wait}s"
  done
  echo ""
  print_success "PostgreSQL hazır"
  
  # Wait for Redis
  print_step "Redis bağlantısı bekleniyor..."
  waited=0
  while ! docker compose -f infra/docker-compose.yml exec -T redis redis-cli ping >/dev/null 2>&1; do
    if [ $waited -ge $max_wait ]; then
      print_warning "Redis hala hazır değil, devam ediliyor..."
      break
    fi
    sleep 1
    waited=$((waited + 1))
    echo -ne "\r  Bekleniyor... ${waited}s / ${max_wait}s"
  done
  echo ""
  print_success "Redis hazır"
  
  # Wait for Keycloak (takes longer)
  print_step "Keycloak'ın hazır olması bekleniyor (90 saniye)..."
  max_wait=90
  waited=0
  
  while ! curl -sf http://localhost:8180/health/ready >/dev/null 2>&1; do
    if [ $waited -ge $max_wait ]; then
      print_warning "Keycloak hala hazır değil, devam ediliyor..."
      break
    fi
    sleep 2
    waited=$((waited + 2))
    echo -ne "\r  Bekleniyor... ${waited}s / ${max_wait}s"
  done
  echo ""
  
  print_success "Docker altyapısı hazır"
}

# Build all services
build_services() {
  print_step "Backend servisleri derleniyor..."
  
  dotnet build CalendaRun.sln --configuration Release --verbosity quiet 2>/dev/null || \
  dotnet build CalendaRun.sln --configuration Release
  
  print_success "Build tamamlandı"
}

# Run migrations
run_migrations() {
  print_step "Database migration'ları uygulanıyor..."
  
  # Platform - using DesignTimeDbContextFactory
  if [ -d "services/platform/src/Platform.Infrastructure" ]; then
    echo "  → Platform DB..."
    dotnet ef database update \
      --project services/platform/src/Platform.Infrastructure \
      --startup-project services/platform/src/Platform.Api \
      2>&1 | grep -v "^The Entity Framework tools version" | grep -v "^An error occurred while accessing" || true
  fi
  
  # Catalog - using DesignTimeDbContextFactory
  if [ -d "services/catalog/src/Catalog.Infrastructure" ]; then
    echo "  → Catalog DB..."
    dotnet ef database update \
      --project services/catalog/src/Catalog.Infrastructure \
      --startup-project services/catalog/src/Catalog.Api \
      2>&1 | grep -v "^The Entity Framework tools version" | grep -v "^An error occurred while accessing" || true
  fi
  
  # Planning - using DesignTimeDbContextFactory
  if [ -d "services/planning/src/Planning.Infrastructure" ]; then
    echo "  → Planning DB..."
    dotnet ef database update \
      --project services/planning/src/Planning.Infrastructure \
      --startup-project services/planning/src/Planning.Api \
      2>&1 | grep -v "^The Entity Framework tools version" | grep -v "^An error occurred while accessing" || true
  fi
  
  print_success "Migration'lar tamamlandı"
}

# Install frontend dependencies
install_frontend() {
  print_step "Frontend bağımlılıkları kuruluyor..."
  
  if [ -d "apps/web" ] && [ ! -d "apps/web/node_modules" ]; then
    (cd apps/web && pnpm install --frozen-lockfile 2>/dev/null || pnpm install)
  fi
  
  print_success "Frontend hazır"
}

# Start all services
start_services() {
  print_step "Servisler başlatılıyor..."
  
  # Create log directory
  mkdir -p .logs
  
  # Start Platform API
  if [ -d "services/platform/src/Platform.Api" ]; then
    echo "  → Platform API (:5401)..."
    dotnet run --project services/platform/src/Platform.Api --no-build > .logs/platform.log 2>&1 &
  fi
  
  # Wait for Platform API (needed for membership validation)
  sleep 3
  
  # Start Catalog API
  if [ -d "services/catalog/src/Catalog.Api" ]; then
    echo "  → Catalog API (:5101)..."
    dotnet run --project services/catalog/src/Catalog.Api --no-build > .logs/catalog.log 2>&1 &
  fi
  
  # Start Planning API
  if [ -d "services/planning/src/Planning.Api" ]; then
    echo "  → Planning API (:5201)..."
    dotnet run --project services/planning/src/Planning.Api --no-build > .logs/planning.log 2>&1 &
  fi
  
  # Start Gateway
  if [ -d "apps/gateway" ]; then
    echo "  → Gateway (:8080)..."
    dotnet run --project apps/gateway --no-build > .logs/gateway.log 2>&1 &
  fi
  
  # Wait for backend services
  sleep 3
  
  # Start Frontend
  if [ -d "apps/web" ]; then
    echo "  → Frontend (:3000)..."
    (cd apps/web && pnpm dev > ../.logs/web.log 2>&1) &
  fi
  
  print_success "Tüm servisler başlatıldı"
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
  echo ""
  
  echo -e "${CYAN}📧 Mailhog${NC}"
  echo "   http://localhost:8025"
  echo ""
  
  echo -e "${CYAN}🐰 RabbitMQ${NC}"
  echo "   http://localhost:15672"
  echo "   User: guest / guest"
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
  echo "   tail -f .logs/platform.log"
  echo "   tail -f .logs/catalog.log"
  echo "   tail -f .logs/planning.log"
  echo "   tail -f .logs/gateway.log"
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
  trap 'stop_all; exit 0' INT TERM
  
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
  print_urls
  wait_for_interrupt
}

main

