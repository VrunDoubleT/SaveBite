#!/bin/sh
set -eu

mkdir -p /run/savebite/postgres /run/postgresql /var/lib/savebite/keys /var/lib/savebite/postgres \
    /var/lib/savebite/redis /var/lib/savebite/rabbitmq /var/lib/savebite/minio \
    /var/log/savebite/rabbitmq
chown app:app /var/lib/savebite /var/lib/savebite/keys
chown postgres:postgres /var/lib/savebite/postgres
chown postgres:postgres /run/postgresql /run/savebite/postgres
chmod 700 /run/savebite/postgres
chown redis:redis /var/lib/savebite/redis
chown rabbitmq:rabbitmq /var/lib/savebite/rabbitmq /var/log/savebite/rabbitmq
chown minio:minio /var/lib/savebite/minio

dotnet /opt/savebite/deployment/SaveBite.Deployment.dll --runtime-env
chmod 755 /run/savebite
set -a
. /run/savebite/runtime.env
set +a
exec /usr/bin/supervisord -n -c /etc/supervisord.conf
