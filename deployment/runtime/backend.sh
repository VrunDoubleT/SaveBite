#!/bin/sh
set -eu

attempt=0
until pg_isready -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null \
    && redis-cli --no-auth-warning -a "$REDIS_PASSWORD" ping 2>/dev/null | grep -q PONG \
    && [ -f /run/savebite/rabbitmq.ready ] \
    && curl -fsS http://127.0.0.1:9000/minio/health/live >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    if [ "$attempt" -ge 180 ]; then
        echo "Services did not become ready." >&2
        exit 1
    fi
    sleep 1
done

exec su-exec app dotnet /app/SaveBite.Api.dll
