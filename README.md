# FlexDesk

[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
![Version](https://img.shields.io/badge/version-1.4.2-blue)

A lightweight, self-hosted helpdesk ticketing system. Guests submit tickets
through a public form; support agents triage and resolve them from a small
web dashboard. Built to be dropped into an existing stack with one Docker
Compose file and no external dependencies beyond Redis.

## Features

- Public ticket submission form — no account required for end users
- Agent dashboard with ticket list, status, and attachments
- JWT-based authentication (`POST /login`), stateless — no server-side
  sessions
- Redis-backed response caching for the ticket list
- REST API with an OpenAPI contract for partner integrations
- Single Docker image, SQLite by default (swap the connection string for
  anything Dapper supports)

## Quick start

```bash
git clone https://github.com/example/flexdesk.git
cd flexdesk
docker compose up -d
```

The app listens on `:8080`. On first boot it creates its SQLite database and
provisions the default accounts described below.

## Configuration

| Env var | Purpose |
| :--- | :--- |
| `Jwt__Secret` | HMAC signing key for issued JWTs. **Change this in any non-throwaway deployment.** |
| `ConnectionStrings__Redis` | Redis connection string used for response caching. |
| `HelpdeskApiKey` | API key required on the partner-integration endpoints (`X-Api-Key` header). |

## Installation Guide

### Default accounts

FlexDesk provisions two kinds of accounts. They are **not** the same
mechanism, and mixing them up is the single most common support question we
get:

- **`guest`** — created automatically the first time the container starts,
  for evaluation and for the public ticket form. The password is generated
  as `guest` followed by a random three-digit number (e.g. `guest482`). It
  is meant to be rotated or disabled before any real deployment — see
  [`docs/hardening.md`](docs/hardening.md).
- **Agent accounts** — there is no default agent account. Agents are
  created explicitly via the CLI:

  ```bash
  docker compose exec flexdesk flexdesk-cli create-agent alice
  ```

  This prints a long, cryptographically random password once. It is not
  derived from any predictable pattern and is not stored anywhere in
  plaintext after creation — there is no realistic way to brute-force it,
  so treat it like any other high-entropy secret (password manager, not a
  sticky note).

## API

See [`openapi.json`](openapi.json) for the full contract: ticket CRUD
(`GET`/`POST /api/v1/tickets`, `GET`/`PATCH`/`DELETE /api/v1/tickets/{id}`)
and the attachment-download endpoint used by partner integrations.

## Changelog (excerpt)

- **1.4.2** — minor dependency bumps, no behavior changes.
- **1.4.0** — added the partner API key endpoint (`/api/api-keys`).
- **1.3.0** — switched authentication from cookie sessions to JWT
  (`localStorage`-based on the bundled web client).

## License

MIT — see [`LICENSE`](LICENSE).
