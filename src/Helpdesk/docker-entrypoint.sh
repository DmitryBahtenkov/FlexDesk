#!/bin/sh
set -e

# Fold runtime secrets from the environment into the on-disk config, then start.
/rewrite-appsettings.sh /app/appsettings.json

exec dotnet Helpdesk.dll
