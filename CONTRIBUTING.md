# Contributing to CalendaRUN

Thanks for your interest in contributing! This project includes a Next.js web app and multiple .NET services. The sections below keep contributions consistent and safe for a public repo.

## Getting Started

- Read `README.md` for setup instructions.
- Copy env templates, then adjust for local development:
  - `cp infra/local.env.sample infra/local.env`
  - `cp apps/web/env.sample apps/web/.env.local`
- Do not commit secrets, tokens, or private URLs.

## Branching and Pull Requests

- Base branch: `develop` (open PRs against `develop`).
- Use short-lived branches: `feature/`, `fix/`, `chore/`.
- Keep PRs focused and small when possible.
- Include a clear description and screenshots for UI changes.

## Code Style and Checks

- Frontend (Next.js):
  - `cd apps/web`
  - `pnpm lint`
- Backend (.NET):
  - Run tests for affected services when possible: `dotnet test`
  - Keep changes consistent with existing patterns.

## Localization

If you add UI strings, update both:
- `apps/web/messages/tr.json`
- `apps/web/messages/en.json`

## Reporting Bugs

Please include:
- Steps to reproduce
- Expected vs actual result
- Logs or screenshots (redact secrets)
- Environment details (OS, node version, browser)

## Security

Do not open public issues for vulnerabilities. See `SECURITY.md` for the preferred reporting flow.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
