# CalendaRun 🏃‍♂️

Koşu ve outdoor etkinlikleri için planlama ve bildirim platformu.

## ✨ Özellikler

- **Multi-tenant Architecture**: Her organizasyon kendi tenant'ında izole çalışır
- **Keycloak Authentication**: OIDC tabanlı güvenli kimlik doğrulama
- **Role-based Authorization**: super_admin, tenant_admin, tenant_user rolleri
- **CQRS-lite Pattern**: Command/Query separation ile clean architecture
- **Event-driven Architecture**: RabbitMQ ile asenkron iletişim
- **Modern UI**: Sports-themed design system with i18n (TR/EN)
- **Centralized Settings**: Dinamik ayar yönetimi ve cache invalidation

---

## 🚀 Projeyi Çalıştırma (Adım Adım)

### 📋 Gereksinimler

Başlamadan önce şunların kurulu olduğundan emin ol:

```bash
# Versiyonları kontrol et
docker --version          # Docker 20.0+
docker compose version    # Docker Compose 2.0+
dotnet --version          # .NET 8.0+
node --version            # Node.js 18+
pnpm --version            # pnpm 8+ (yoksa: npm install -g pnpm)
dotnet ef --version       # dotnet-ef (yoksa: dotnet tool install -g dotnet-ef)
```

---

## ⚡ Tek Komutla Başlat (Önerilen)

```bash
# Tüm sistemi başlat (Docker + Backend + Frontend)
./scripts/start-all.sh

# Fresh start (tüm verileri sil ve sıfırdan başla)
./scripts/start-all.sh --fresh

# Tüm servisleri durdur
./scripts/start-all.sh --stop
```

Bu komut otomatik olarak:
1. ✅ Docker altyapısını başlatır (Postgres, Redis, RabbitMQ, Keycloak, Mailhog)
2. ✅ Keycloak'ın hazır olmasını bekler
3. ✅ Database migration'larını uygular
4. ✅ Frontend bağımlılıklarını yükler
5. ✅ Tüm backend servislerini başlatır
6. ✅ Frontend'i başlatır
7. ✅ Erişim URL'lerini gösterir

> **Not:** İlk çalıştırmada build süresi ~2-3 dakika sürebilir.

---

### 🔧 Manuel Kurulum (Alternatif)

#### Adım 1: Ortam Değişkenlerini Hazırla

```bash
cd CalendaRun
cp infra/local.env.sample infra/local.env
cp apps/web/env.sample apps/web/.env.local
```

> Not: `infra/local.env.sample` ve `apps/web/env.sample` sadece yerel gelistirme icindir. Public repo icin gercek sifreleri veya secretlari bu dosyalara yazmayin.

#### Adım 2: Docker Altyapısını Başlat

```bash
# PostgreSQL, Redis, RabbitMQ, Keycloak, Mailhog
docker compose -f infra/docker-compose.yml up -d

# Servislerin hazır olmasını bekle (Keycloak ~30-60 saniye sürebilir)
echo "Keycloak başlatılıyor, lütfen bekle..."
sleep 45

# Servislerin durumunu kontrol et
docker compose -f infra/docker-compose.yml ps
```

Beklenen çıktı:
```
NAME                SERVICE       STATUS
calendarun-db       postgres      running (healthy)
calendarun-redis    redis         running
calendarun-rabbit   rabbitmq      running
calendarun-keycloak keycloak      running
calendarun-mailhog  mailhog       running
```

#### Adım 3: Database Migration'ları Uygula

```bash
# Bootstrap script'i çalıştır (migration + seed data)
chmod +x scripts/bootstrap.sh
./scripts/bootstrap.sh
```

> **Not:** Eğer bootstrap.sh hata verirse, migration'ları manuel çalıştır:
> ```bash
> # Platform DB
> cd services/platform/src/Platform.Api
> dotnet ef database update --project ../Platform.Infrastructure
> 
> # Catalog DB
> cd services/catalog/src/Catalog.Api
> dotnet ef database update --project ../Catalog.Infrastructure
> 
> # Planning DB
> cd services/planning/src/Planning.Api
> dotnet ef database update --project ../Planning.Infrastructure
> ```

#### Adım 4: Backend Servisleri Başlat

**5 ayrı terminal aç** ve her birinde bir servisi başlat:

```bash
# Terminal 1: Gateway (API Proxy)
cd apps/gateway
dotnet run
# Beklenen: "Now listening on: http://localhost:8080"

# Terminal 2: Platform API (Tenant & Users)
cd services/platform/src/Platform.Api
dotnet run
# Beklenen: "Now listening on: http://localhost:5401"

# Terminal 3: Catalog API (Events)
cd services/catalog/src/Catalog.Api
dotnet run
# Beklenen: "Now listening on: http://localhost:5101"

# Terminal 4: Planning API (User Plans)
cd services/planning/src/Planning.Api
dotnet run
# Beklenen: "Now listening on: http://localhost:5201"

# Terminal 5: Frontend
cd apps/web
pnpm install
pnpm dev
# Beklenen: "ready started server on http://localhost:3000"
```

#### Adım 5: Servisleri Doğrula

```bash
# Health check'ler
curl http://localhost:5401/health  # Platform API
curl http://localhost:5101/health  # Catalog API
curl http://localhost:5201/health  # Planning API
curl http://localhost:8080/health  # Gateway
```

---

## 🌐 Erişim URL'leri

| Servis | URL | Açıklama |
|--------|-----|----------|
| 🎨 **Web UI** | http://localhost:3000 | Next.js Frontend |
| 🔐 **Keycloak** | http://localhost:8180 | Identity Provider |
| 🚪 **Gateway** | http://localhost:8080 | API Gateway |
| 📧 **Mailhog** | http://localhost:8025 | Email Test UI |
| 🐰 **RabbitMQ** | http://localhost:15672 | Message Broker |

---

## 🔑 Giriş Yapma

### Web UI (In-app Auth)

1. http://localhost:3000 adresine git
2. **"Giriş Yap"** butonuna tıkla ("/login" sayfası uygulama icinde acilir)
3. `/register` ile yeni hesap olustur veya Keycloak Admin Console'da kullanici ekleyip `/login` ile giris yap.

Notlar:
- `/register`, `/forgot-password` ve `/reset-password` akislari Keycloak Admin API gerektirir.
- Tokenlar httpOnly cookie ile tutulur; tarayici tarafinda localStorage kullanilmaz.
- E-posta sifre sifirlama icin SMTP ayarlarinin yapili olmasi gerekir (dev icin Mailhog kullanabilirsiniz).

### Keycloak Ayarları (In-app Auth)

- `calendarun-web` client:
  - **Direct Access Grants** aktif (password grant).
  - **Valid Redirect URIs**: `http://localhost:3000/*`
  - **Web Origins**: `http://localhost:3000`
- `calendarun-admin` client:
  - **Service Accounts** aktif.
  - Service account rolü: `realm-management` altında `manage-users`, `view-users`.
- Realm SMTP ayarları doğru olmalı (password reset e-postası için).

---

## 📱 Web UI Kullanım Rehberi

### Ana Sayfalar

| Sayfa | URL | Açıklama |
|-------|-----|----------|
| **Keşfet** | `/` | Tüm yarış etkinliklerini listele ve filtrele |
| **Giriş** | `/login` | Uygulama içi giriş ekranı |
| **Kayıt** | `/register` | Yeni hesap oluştur |
| **Şifre Sıfırla** | `/forgot-password` | Şifre sıfırlama bağlantısı iste |
| **Yeni Şifre** | `/reset-password` | Yeni şifre belirle |
| **Etkinlik Detay** | `/events/{id}` | Etkinlik detayları, milestones, kayıt linki |
| **Planım** | `/plan` | Planladığın yarışları takip et |
| **Ayarlar** | `/settings` | Profil bilgileri |

### Admin Sayfaları (Tenant Admin)

| Sayfa | URL | Açıklama |
|-------|-----|----------|
| **Etkinlik Yönetimi** | `/admin/events` | Firma etkinliklerini oluştur/düzenle |
| **Kullanıcılar** | `/admin/users` | Firma kullanıcılarını gör |
| **Audit Log** | `/admin/audit` | Firma denetim kayıtları |

### Super Admin Sayfaları

| Sayfa | URL | Açıklama |
|-------|-----|----------|
| **Firmalar** | `/super-admin/tenants` | Tüm firmaları yönet |
| **Tüm Etkinlikler** | `/super-admin/events` | Global etkinlik listesi |
| **Global Audit** | `/super-admin/audit` | Tüm sistem denetim kayıtları |

### Dil Değiştirme

Sağ üst köşedeki 🌐 ikonuna tıklayarak **Türkçe/English** arasında geçiş yap.

---

## 🧪 API Test Örnekleri

```bash
# Ornek: BFF uzerinden login olup cookie ile API cagrilari
EMAIL="you@example.com"
PASSWORD="your-password"
TENANT_ID="00000000-0000-0000-0000-000000000001"

curl -s -X POST "http://localhost:3000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}" \
  -c /tmp/calendarun.cookies >/dev/null

# Etkinlikleri listele
curl -b /tmp/calendarun.cookies \
  -H "X-Tenant-Id: $TENANT_ID" \
  http://localhost:3000/api/events | jq

# Firmaları listele (super admin)
curl -b /tmp/calendarun.cookies \
  http://localhost:3000/api/super-admin/tenants | jq

# Plan oluştur
curl -X POST http://localhost:3000/api/plan \
  -b /tmp/calendarun.cookies \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "Content-Type: application/json" \
  -d '{"eventId":"22222222-2222-2222-2222-222222222222"}'
```

---

## 🛠️ Sorun Giderme

### Keycloak başlamıyor

```bash
# Keycloak loglarını kontrol et
docker compose -f infra/docker-compose.yml logs keycloak

# Keycloak'ı yeniden başlat
docker compose -f infra/docker-compose.yml restart keycloak
```

### Database bağlantı hatası

```bash
# PostgreSQL çalışıyor mu kontrol et
docker compose -f infra/docker-compose.yml ps postgres

# Connection string'i kontrol et
cat infra/local.env | grep POSTGRES
```

### Frontend 401 hatası

1. Oturumu kapatıp tekrar giriş yap (`/login`)
2. Tarayıcıda `calendarun_access_token` ve `calendarun_refresh_token` cookie'lerini temizle
3. Keycloak client ayarlarında **Direct Access Grants** açık mı kontrol et

### Port çakışması

```bash
# Hangi portlar kullanılıyor kontrol et
lsof -i :3000   # Frontend
lsof -i :8080   # Gateway
lsof -i :5401   # Platform API
lsof -i :5101   # Catalog API
lsof -i :5201   # Planning API
```

---

## 🏢 Multi-Tenant Yapısı

### Rol Hiyerarşisi

```
super_admin (Global)
├── Tüm tenant'ları görür/yönetir
├── Yeni tenant oluşturur
└── Global ayarları değiştirir

tenant_admin (Firma Bazlı)
├── Kendi firmasını yönetir
├── Kullanıcı davet eder
└── Etkinlik CRUD yapar

tenant_user (Firma Bazlı)
├── Etkinlikleri görür
└── Kendi planlarını yönetir
```

### Tenant Header

Tüm tenant-scoped API isteklerinde `X-Tenant-Id` header'ı gereklidir:

```bash
curl -H "Authorization: Bearer $TOKEN" \
     -H "X-Tenant-Id: 00000000-0000-0000-0000-000000000001" \
     http://localhost:8080/api/events
```

---

## 📐 Sistem Mimarisi

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Next.js Frontend (:3000)                          │
│                        Tailwind + shadcn/ui + i18n                          │
└────────────────────────────────────┬────────────────────────────────────────┘
                                     │
┌────────────────────────────────────┴────────────────────────────────────────┐
│                              Gateway (YARP)                                  │
│                             localhost:8080                                   │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  JWT Validation (Keycloak)  │  Tenant Validation (Platform API)     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────┬────────────────────────────────────────┘
                                     │
         ┌───────────────────────────┼───────────────────────────┐
         ▼                           ▼                           ▼
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  Platform.Api   │         │  Catalog.Api    │         │  Planning.Api   │
│  :5401          │         │  :5101          │         │  :5201          │
│  • Tenants      │         │  • Events       │         │  • Plans        │
│  • Memberships  │         │  • Audit Logs   │         │  • Users        │
│  • Invites      │         │                 │         │  • Audit Logs   │
└─────────────────┘         └─────────────────┘         └─────────────────┘
         │                           │                           │
         └───────────────────────────┴───────────────────────────┘
                                     │
                          ┌──────────┴──────────┐
                          │  PostgreSQL :55432  │
                          └─────────────────────┘
```

---

## 🗂️ Proje Yapısı

```
CalendaRun/
├── apps/
│   ├── gateway/                    # YARP API Gateway
│   └── web/                        # Next.js Frontend
│       ├── src/
│       │   ├── app/                # App Router pages
│       │   ├── components/         # UI components
│       │   ├── contexts/           # React contexts (auth, locale)
│       │   ├── hooks/              # Custom hooks
│       │   └── lib/                # API client, utilities
│       └── messages/               # i18n (tr.json, en.json)
├── services/
│   ├── platform/                   # Tenant & Membership service
│   ├── catalog/                    # Event catalog service
│   ├── planning/                   # User planning service
│   └── notifications/              # Notification worker
├── building-blocks/
│   ├── Common/                     # Shared utilities
│   └── Contracts/                  # Event contracts
├── infra/
│   ├── docker-compose.yml          # Docker services
│   ├── keycloak/                   # Keycloak realm config
│   └── local.env.sample            # Environment template
├── scripts/
│   ├── bootstrap.sh                # Initial setup
│   └── dev.sh                      # Start all services
└── tests/                          # Unit & Integration tests
```

---

## 📝 Geliştirme

### Yeni Migration Oluştur

```bash
cd services/platform/src/Platform.Api
dotnet ef migrations add MigrationName --project ../Platform.Infrastructure
dotnet ef database update --project ../Platform.Infrastructure
```

### Frontend Geliştirme

```bash
cd apps/web
pnpm dev          # Development server
pnpm build        # Production build
pnpm lint         # ESLint check
```

### Yeni i18n Key Ekleme

1. `apps/web/messages/tr.json` ve `en.json` dosyalarına ekle
2. Component'te `t("key.path")` ile kullan

---

## 🧱 Tech Stack

| Katman | Teknoloji |
|--------|-----------|
| **Frontend** | Next.js 14, TypeScript, Tailwind CSS, shadcn/ui |
| **Backend** | .NET 8, ASP.NET Core, Entity Framework Core 8 |
| **Database** | PostgreSQL 16 |
| **Cache** | Redis |
| **Message Broker** | RabbitMQ + MassTransit |
| **Auth** | Keycloak (OIDC) + BFF (httpOnly cookies) |
| **Gateway** | YARP |

---

## 🤝 Contributing

Katki yapmak icin [CONTRIBUTING.md](CONTRIBUTING.md) dosyasina goz atabilirsiniz.

---

## 🔒 Security

Guvenlik aciklarini bildirmek icin [SECURITY.md](SECURITY.md) dosyasini kullanin. Lutfen public issue acmayin.

---

## 📄 License

MIT
