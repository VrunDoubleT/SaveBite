#!/bin/sh
set -eu

attempt=0
until su-exec rabbitmq rabbitmqctl -q list_users >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    if [ "$attempt" -ge 120 ]; then
        echo "RabbitMQ did not become ready." >&2
        exit 1
    fi
    sleep 1
done

if su-exec rabbitmq rabbitmqctl -q list_users | awk '{print $1}' | grep -Fxq "$RABBITMQ_USER"; then
    su-exec rabbitmq rabbitmqctl change_password "$RABBITMQ_USER" "$RABBITMQ_PASSWORD" >/dev/null
else
    su-exec rabbitmq rabbitmqctl add_user "$RABBITMQ_USER" "$RABBITMQ_PASSWORD" >/dev/null
fi
su-exec rabbitmq rabbitmqctl set_permissions -p / "$RABBITMQ_USER" '.*' '.*' '.*' >/dev/null
su-exec rabbitmq rabbitmqctl set_user_tags "$RABBITMQ_USER" administrator >/dev/null
touch /run/savebite/rabbitmq.ready
