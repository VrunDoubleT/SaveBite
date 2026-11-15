#!/bin/sh
set -eu

data=/var/lib/savebite/postgres
if [ ! -s "$data/PG_VERSION" ]; then
    password_file=/run/savebite/postgres-password
    printf '%s\n' "$POSTGRES_PASSWORD" > "$password_file"
    chown postgres:postgres "$password_file"
    chmod 600 "$password_file"
    su-exec postgres initdb -D "$data" -U "$POSTGRES_USER" \
        --pwfile="$password_file" --auth-local=trust --auth-host=scram-sha-256
    rm -f "$password_file"
    su-exec postgres pg_ctl -D "$data" -o "-h 127.0.0.1" -l /run/savebite/postgres/init.log -w start
    PGPASSWORD="$POSTGRES_PASSWORD" createdb -h 127.0.0.1 -U "$POSTGRES_USER" "$POSTGRES_DB"
    su-exec postgres pg_ctl -D "$data" -m fast -w stop
fi

# Apply equally to existing volumes; host access always requires SCRAM authentication.
cat > /run/savebite/postgres/pg_hba.conf <<'EOF'
local all all trust
host all all 0.0.0.0/0 scram-sha-256
EOF
chown postgres:postgres /run/savebite/postgres/pg_hba.conf
chmod 600 /run/savebite/postgres/pg_hba.conf
exec su-exec postgres postgres -D "$data" -h 0.0.0.0 -c hba_file=/run/savebite/postgres/pg_hba.conf
