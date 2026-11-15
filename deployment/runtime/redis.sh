#!/bin/sh
set -eu

exec su-exec redis redis-server \
    --bind 0.0.0.0 \
    --appendonly yes \
    --dir /var/lib/savebite/redis \
    --requirepass "$REDIS_PASSWORD"
