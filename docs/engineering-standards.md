# Engineering Standards

## Naming and Layering
- `*.Api` exposes HTTP endpoints only; controllers should stay thin and delegate to Application handlers.
- `*.Application` owns use cases and DTOs; avoid direct HTTP concerns here.
- `*.Infrastructure` contains persistence and external integrations.
- Use `*Dto` for read models, `*Request` for API inputs, `*Command`/`*Query` for handlers.

## Time and Timezone
- Store all timestamps in UTC (`DateTimeOffset` with offset `+00:00`).
- Date-only query params use `yyyy-MM-dd`; interpret as UTC day boundaries.
- Date-time query params use ISO-8601 and are normalized to UTC.
- Never use local server time in persistence or filtering.

## Enums and Magic Strings
- Frontend: keep shared values in `apps/web/src/lib/constants/*`.
- Backend: use enums or shared constants in `building-blocks/Common`.
- Avoid stringly-typed state machines; map numeric enums at the edges.

## Error Handling Contract
- APIs return RFC7807 ProblemDetails.
- Required fields: `type`, `title`, `status`, `detail`, `traceId`, `code`.
- Validation errors include `errors` dictionary when available.

## Local Development
- Start everything: `./scripts/start-all.sh`
- Run a single API: `dotnet run --project services/<service>/src/<Service>.Api`
- Frontend: `pnpm dev` in `apps/web`
- Debugging: set `ASPNETCORE_ENVIRONMENT=Development`, attach Rider/VS to the API process.
