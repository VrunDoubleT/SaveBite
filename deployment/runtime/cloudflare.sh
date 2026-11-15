#!/bin/sh
set -eu

if [ "${SAVEBITE_ENABLE_CLOUDFLARE:-true}" != "true" ]; then
    exec tail -f /dev/null
fi

attempt=0
until curl -fsS http://127.0.0.1/api/health/ready >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    if [ "$attempt" -ge 180 ]; then
        echo "Backend did not become ready for the public tunnel." >&2
        exit 1
    fi
    sleep 1
done

exec su-exec app dotnet /opt/savebite/deployment/SaveBite.Deployment.dll
