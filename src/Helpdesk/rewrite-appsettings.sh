#!/bin/sh
set -e

# Applies runtime secrets from the environment into appsettings.json so the
# published image can ship without secrets baked in. Values are written to the
# on-disk config the app reads at startup.
FILE="${1:-/app/appsettings.json}"

if [ -z "${REDIS_PASSWORD:-}" ]; then
  echo "REDIS_PASSWORD is required" >&2
  exit 1
fi

subst() {
  # $1 JSON key name (assumed unique in the file); $2 replacement value
  # Uses | as sed delimiter. Escape sed's replacement metacharacters
  # (\, &, and the | delimiter) so arbitrary values round-trip safely.
  _key="$1"
  _val="$2"
  _esc_val="$(printf '%s' "$_val" | sed -e 's/[\&|]/\\&/g')"
  _tmp="$(mktemp)"
  sed "s|\"${_key}\": \"[^\"]*\"|\"${_key}\": \"${_esc_val}\"|" "$FILE" > "$_tmp"
  cat "$_tmp" > "$FILE"
  rm -f "$_tmp"
}

subst Redis "redis:6379,password=${REDIS_PASSWORD}"

if [ -n "${JWT_SECRET:-}" ]; then
  subst Secret "$JWT_SECRET"
fi
if [ -n "${HELPDESK_API_KEY:-}" ]; then
  subst HelpdeskApiKey "$HELPDESK_API_KEY"
fi
