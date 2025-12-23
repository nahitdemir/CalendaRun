# CalendaRun 🏃‍♂️

Koşu ve outdoor etkinlikleri için planlama ve bildirim platformu.

## 🚀 Hızlı Başlangıç

### Gereksinimler

- Docker & Docker Compose
- .NET 8 SDK
- Node.js 18+ & pnpm
- dotnet-ef tool (`dotnet tool install -g dotnet-ef`)

### Tek Komutla Başlat

```bash
# 1. Ortam değişkenlerini kopyala
cp infra/local.env.sample infra/local.env

# 2. Altyapı + migration + seed
./scripts/bootstrap.sh

# 3. Tüm servisleri başlat
./scripts/dev.sh
```

### 🧪 Demo Test

```bash
# Events listesi
curl http://localhost:5101/events | jq

# Plan oluştur (email gönderilecek)
curl -X POST http://localhost:5201/plan \
  -H "Content-Type: application/json" \
  -H "X-Dev-User: demo@calendarun.local" \
  -d '{"eventId":"22222222-2222-2222-2222-222222222222"}'

# Gateway üzerinden
curl http://localhost:8080/api/events | jq
```

📧 **Mailhog'da email kontrol et:** http://localhost:8025

## 📐 Mimari

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Gateway (YARP)                            │
│                          localhost:8080                             │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│  Catalog.Api  │   │ Planning.Api  │   │ Settings.Api  │
│  :5101        │   │  :5201        │   │   :5301       │
└───────┬───────┘   └───────┬───────┘   └───────┬───────┘
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────────────────────────────────────────────┐
│                    PostgreSQL :55432                   │
│    catalogdb    │   planningdb    │    settingsdb     │
└───────────────────────────────────────────────────────┘

                    ┌───────────────┐
                    │  RabbitMQ     │
                    │  :5672/:15672 │
                    └───────┬───────┘
                            │
                            ▼
              ┌─────────────────────────┐
              │  Notifications.Worker   │
              │  (Consumer + Dispatcher)│
              └─────────────┬───────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │    Mailhog      │
                   │   :1025/:8025   │
                   └─────────────────┘
```

## 🗂️ Proje Yapısı

```
CalendaRun/
├── apps/
│   └── gateway/              # YARP Gateway
├── building-blocks/
│   ├── Common/               # Shared utilities (ProblemDetails, UseCase)
│   ├── Contracts/            # Event contracts (v1 schemas)
│   │   ├── Planning/
│   │   ├── Settings/
│   │   └── Notifications/
│   └── Settings/             # Settings client library
├── services/
│   ├── catalog/              # Event catalog service
│   │   └── src/
│   │       ├── Catalog.Api
│   │       ├── Catalog.Application
│   │       ├── Catalog.Domain
│   │       └── Catalog.Infrastructure
│   ├── planning/             # User planning service
│   │   └── src/
│   │       ├── Planning.Api
│   │       ├── Planning.Application
│   │       ├── Planning.Domain
│   │       └── Planning.Infrastructure
│   ├── notifications/        # Notification service
│   │   └── src/
│   │       ├── Notifications.Worker
│   │       ├── Notifications.Application
│   │       ├── Notifications.Domain
│   │       └── Notifications.Infrastructure
│   └── settings/             # Settings service
│       └── src/
│           ├── Settings.Api
│           ├── Settings.Application
│           ├── Settings.Domain
│           └── Settings.Infrastructure
├── infra/
│   ├── docker-compose.yml
│   ├── local.env.sample
│   └── postgres-init/
└── scripts/
    ├── bootstrap.sh
    └── dev.sh
```

## 🔌 API Endpoints

### Catalog API (:5101)

| Method | Path | Description |
|--------|------|-------------|
| GET | /health | Health check |
| GET | /events | List all events |

### Planning API (:5201)

| Method | Path | Description |
|--------|------|-------------|
| GET | /health | Health check |
| POST | /plan | Create a plan (requires X-Dev-User header) |

### Settings API (:5301)

| Method | Path | Description |
|--------|------|-------------|
| GET | /health | Health check |
| GET | /settings?keys=a,b&tenantId=... | Bulk get settings |
| GET | /settings/{key}?tenantId=... | Get single setting |
| PUT | /settings/{key}?tenantId=... | Update setting |
| GET | /settings/version?tenantId=... | Get version |

## 📨 Event Contracts

### planning.userplanned.v1
```json
{
  "userId": "guid",
  "userEmail": "string",
  "eventId": "guid",
  "planItemId": "guid",
  "timezone": "Europe/Istanbul",
  "occurredAt": "2025-01-01T00:00:00Z"
}
```

### settings.changed.v1
```json
{
  "tenantId": "string|null",
  "keys": ["string"],
  "version": 1,
  "occurredAt": "2025-01-01T00:00:00Z"
}
```

## 🔧 Konfigürasyon (Settings)

| Key | Default | Description |
|-----|---------|-------------|
| notifications.smtp.host | localhost | SMTP host |
| notifications.smtp.port | 1025 | SMTP port |
| notifications.smtp.from | noreply@calendarun.local | From email |
| notifications.email.subject_template | You planned event: {EventId} | Subject template |
| notifications.email.body_template | ... | Body template |
| notifications.reminder.offsets_minutes | [1440, 60, 15] | Reminder times |
| notifications.dispatcher.max_attempts | 3 | Max retry attempts |
| notifications.dispatcher.batch_size | 50 | Batch size |
| planning.default_timezone | Europe/Istanbul | Default timezone |
| planning.max_plans_per_user | 100 | Max plans per user |

## 🌐 Useful URLs

| Service | URL |
|---------|-----|
| Gateway | http://localhost:8080 |
| Catalog API | http://localhost:5101 |
| Planning API | http://localhost:5201 |
| Settings API | http://localhost:5301 |
| Mailhog | http://localhost:8025 |
| RabbitMQ | http://localhost:15672 (guest/guest) |
| Grafana | http://localhost:3000 |

## 📝 Development

### Migration oluştur
```bash
dotnet ef migrations add MigrationName \
  --project services/catalog/src/Catalog.Infrastructure \
  --startup-project services/catalog/src/Catalog.Api
```

### Migration uygula
```bash
dotnet ef database update \
  --project services/catalog/src/Catalog.Infrastructure \
  --startup-project services/catalog/src/Catalog.Api
```

## 🧱 Tech Stack

- **.NET 8** - API & Worker
- **PostgreSQL 16** - Database
- **Redis** - Cache
- **RabbitMQ** - Message broker
- **MassTransit** - Message bus abstraction
- **Entity Framework Core** - ORM
- **YARP** - Reverse proxy
- **Serilog** - Structured logging
- **Mailhog** - Email testing

## 📄 License

MIT
