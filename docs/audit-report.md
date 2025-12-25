# Audit Report

## Frontend Findings

### Critical
- Hook ordering bug causes runtime crash when `EventDetailPage` exits early before all hooks run. `apps/web/src/app/events/[id]/page.tsx`  
  Why: React requires hooks to run in the same order across renders; conditional early returns broke it.  
  Fix: move all hooks above early returns (implemented).

### High
- `settingsApi.getDistances` passes `skipAuth`/`skipTenant` but `api.get` ignores config, so headers leak and public endpoint can fail. `apps/web/src/lib/api-client.ts`  
  Why: auth failures silently fall back to defaults; tenant-specific distances never load.  
  Fix: allow `api.get` to accept config + tenant override (implemented).
- Two API clients (`api.ts` vs `api-client.ts`) disagree on base paths and error handling. `apps/web/src/lib/api.ts`, `apps/web/src/lib/api-client.ts`  
  Why: inconsistent auth headers and `/api` prefix usage leads to hard-to-debug behavior.  
  Fix: consolidate on `api-client.ts`, migrate server components.

### Medium
- Distance parsing + plan state normalization duplicated across pages. `apps/web/src/app/page.tsx`, `apps/web/src/app/plan/page.tsx`, `apps/web/src/app/events/[id]/page.tsx`, `apps/web/src/components/plan-item-card.tsx`  
  Why: drift and edge-case bugs (e.g., missing mapping) appear in multiple places.  
  Fix: shared normalizers + constants (implemented).
- Magic strings for roles, milestone types, plan states, routes are scattered. `apps/web/src/contexts/auth-context.tsx`, `apps/web/src/components/milestone-timeline.tsx`, `apps/web/src/app/admin/*`  
  Why: brittle comparisons and inconsistent casing across screens.  
  Fix: centralize in `apps/web/src/lib/constants/*`.
- i18n bypassed for user-visible text in a few places. `apps/web/src/app/plan/page.tsx`, `apps/web/src/app/page.tsx`  
  Why: strings won’t be translated or consistent with locale files.  
  Fix: add missing keys to locale messages and replace inline text.

### Low
- `ProblemDetails.code` extension not modeled on the frontend. `apps/web/src/lib/api-client.ts`  
  Why: clients can’t display meaningful error codes.  
  Fix: include `code` in interface + surfaced on error (implemented).
- No frontend test runner configured.  
  Why: hard to validate normalizers and API parsing.  
  Fix: adopt minimal test setup (e.g., vitest) or add contract tests in BE.

## Backend Findings

### High
- Date range parsing inconsistent across services; audit log endpoints depend on model binder (local time), events use strict UTC. `services/*/src/*/Controllers/*AuditLogsController.cs`, `services/catalog/src/Catalog.Api/Controllers/EventsController.cs`  
  Why: off-by-one-day filters depending on server locale/timezone.  
  Fix: shared UTC-aware `DateQueryParser` (implemented).
- Error responses inconsistent (`{ error: ... }`, `Forbid`, etc.), missing `traceId` and error codes. Multiple API controllers.  
  Why: frontend ProblemDetails handling can’t rely on a contract.  
  Fix: standardize on `ProblemDetailsFactory` (implemented).

### Medium
- Header names stringly typed across controllers; error messages reference raw header names. Multiple API controllers.  
  Why: inconsistencies across services when headers change.  
  Fix: shared `HeaderNames` + avoid header references in Application error messages (implemented).
- `Program.cs` repeated auth/logging/settings wiring across services. `services/*/src/*/Program.cs`  
  Why: drift risk, harder to update security/logging in one place.  
  Fix: common extensions for auth + logging or shared defaults.
- Hardcoded service endpoints (`localhost` for settings/RabbitMQ/Redis). `services/*/src/*/Program.cs`  
  Why: breaks in container or deployment environments.  
  Fix: use env-first configuration with sensible fallbacks.

### Low
- Hardcoded audit action/entity type lists. `services/platform/src/Platform.Api/Controllers/AuditLogsController.cs`  
  Why: can drift from domain events.  
  Fix: move to shared constants or build from domain events.
- `Result` pattern duplicated across services. `services/*/src/*/Application/Common/Result.cs`  
  Why: repeated logic and inconsistent semantics over time.  
  Fix: shared result type in building-blocks (optional).

## Shared / DevOps Findings

### Medium
- `infra/local.env` and `infra/local.env.sample` drift; keys like `KEYCLOAK_PORT` are missing from local env.  
  Why: local startup behaves differently for different machines.  
  Fix: keep env files in sync and document required keys.
- Docker + frontend port collisions (Grafana vs frontend). `infra/local.env`  
  Why: `start-all.sh` can kill the wrong process.  
  Fix: separate ports (implemented previously).

## Refactor Plan (Prioritized PRs)

- PR-01 (Frontend): constants + normalizers for plan/event distances, fix hooks ordering, `api.get` config support, update distance fetching, add FE error code handling, add docs.  
- PR-02 (Backend): shared UTC date parsing for query filters + tests.  
- PR-03 (Backend): shared header constants + consistent ProblemDetails mapping across services.  
- PR-04 (Frontend): consolidate `api.ts` into `api-client.ts`, remove duplicate API surface.  
- PR-05 (Backend): shared auth/logging/config extensions; remove `localhost` hardcoding.  
