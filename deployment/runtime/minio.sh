#!/bin/sh
set -eu

exec su-exec minio minio server /var/lib/savebite/minio \
    --address 0.0.0.0:9000 --console-address 0.0.0.0:9001
