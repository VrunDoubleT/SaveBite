#!/bin/sh
set -eu

export RABBITMQ_MNESIA_BASE=/var/lib/savebite/rabbitmq
export RABBITMQ_LOG_BASE=/var/log/savebite/rabbitmq
exec su-exec rabbitmq rabbitmq-server
