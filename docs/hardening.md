# Hardening FlexDesk for production

FlexDesk ships with defaults tuned for evaluation, not for a public
deployment. Work through this checklist before exposing an instance to
untrusted users.

## 1. Rotate or disable the evaluation guest account

On first boot FlexDesk seeds a `guest` account with a short, generated
starter password (`guest` plus a random three-digit number, printed to the
container log). This account exists so you can click through the product
immediately — it is **not** meant to survive into production.

- Disable it once real accounts exist, or
- Reset its password to a long, random value.

The generated password is low entropy by design and must never be relied on
as a security boundary.

## 2. Set a strong JWT signing secret

`Jwt__Secret` defaults to a well-known development value. Issued tokens are
only as trustworthy as this key.

- Generate at least 32 random bytes and pass them via `JWT_SECRET`.
- Rotate the secret if you suspect it has leaked; this invalidates all
  outstanding tokens.

## 3. Set a real partner API key

The partner integration endpoints authenticate with a single shared key sent
in the `X-Api-Key` header (`HelpdeskApiKey` / `HELPDESK_API_KEY`). The default
is a placeholder.

- Issue a unique, high-entropy key per environment.
- Treat it as a secret: it grants full ticket CRUD and attachment access.

## 4. Protect configuration and uploads

FlexDesk reads its runtime configuration from `appsettings.json` on disk and
stores uploaded attachments under the uploads root.

- Run the container as a non-root user (the published image already does).
- Keep the uploads directory on storage that is not web-servable except
  through the application.
- Restrict file reads to the uploads root; do not expose the application's
  working directory to the public.

## 5. Terminate TLS in front of FlexDesk

The app speaks plain HTTP on `:8080`. Put it behind a reverse proxy
(nginx, Caddy, a cloud load balancer) that terminates TLS, and forward only
over a trusted network.

## 6. Lock down Redis

Redis is used for response caching. Keep it on a private network, require a
password (`REDIS_PASSWORD`), and never publish its port to the internet.

## 7. Keep dependencies current

Track releases and apply dependency updates promptly — the changelog notes
which releases are security-relevant.
