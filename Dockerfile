FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS api-build
WORKDIR /src
COPY SaveBite.Backend/SaveBite.Api/SaveBite.Api.csproj SaveBite.Api/
RUN dotnet restore SaveBite.Api/SaveBite.Api.csproj
COPY SaveBite.Backend/SaveBite.Api/ SaveBite.Api/
RUN dotnet publish SaveBite.Api/SaveBite.Api.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS deployment-build
WORKDIR /src
COPY deployment/SaveBite.Deployment/SaveBite.Deployment.csproj SaveBite.Deployment/
RUN dotnet restore SaveBite.Deployment/SaveBite.Deployment.csproj
COPY deployment/SaveBite.Deployment/ SaveBite.Deployment/
RUN dotnet publish SaveBite.Deployment/SaveBite.Deployment.csproj -c Release --no-restore -o /out

FROM node:24-alpine AS frontend-build
WORKDIR /src
COPY SaveBite.Fontend/package.json SaveBite.Fontend/package-lock.json ./
RUN npm ci
COPY SaveBite.Fontend/ ./
ENV VITE_API_URL=/api
RUN npm run build

FROM golang:1.25-alpine AS minio-build
ARG MINIO_VERSION=RELEASE.2025-10-15T17-29-55Z
RUN apk add --no-cache git ca-certificates
RUN CGO_ENABLED=0 go install github.com/minio/minio@${MINIO_VERSION}

FROM cloudflare/cloudflared:latest AS cloudflared

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
RUN apk add --no-cache ca-certificates curl nginx postgresql16 postgresql16-client icu-libs tzdata \
        postgresql16-contrib \
        rabbitmq-server redis su-exec supervisor \
    && mkdir -p /etc/rabbitmq \
    && rabbitmq-plugins enable --offline rabbitmq_management \
    && addgroup -S -g 10001 minio \
    && adduser -S -D -u 10001 -G minio minio \
    && mkdir -p /app /opt/savebite/private /opt/savebite/scripts /run/savebite \
        /var/lib/savebite \
    && chmod 700 /opt/savebite/private /run/savebite
WORKDIR /app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
COPY --from=api-build /out/ /app/
COPY --from=deployment-build /out/ /opt/savebite/deployment/
COPY --from=frontend-build /src/dist/ /usr/share/nginx/html/
COPY --from=minio-build /go/bin/minio /usr/local/bin/minio
COPY --from=cloudflared /usr/local/bin/cloudflared /usr/local/bin/cloudflared
COPY .env /opt/savebite/private/root.env
COPY SaveBite.Backend/.env /opt/savebite/private/backend.env
COPY deployment/config/nginx.conf /etc/nginx/http.d/default.conf
COPY deployment/config/supervisord.conf /etc/supervisord.conf
COPY deployment/runtime/*.sh /opt/savebite/scripts/
RUN chmod 600 /opt/savebite/private/*.env \
    && chmod 755 /opt/savebite/scripts/*.sh \
    && mkdir -p /var/lib/savebite/postgres /var/lib/savebite/redis \
        /var/lib/savebite/rabbitmq /var/lib/savebite/minio /var/lib/savebite/keys \
        /var/log/savebite/rabbitmq \
    && chown -R postgres:postgres /var/lib/savebite/postgres \
    && chown -R redis:redis /var/lib/savebite/redis \
    && chown -R rabbitmq:rabbitmq /var/lib/savebite/rabbitmq /var/log/savebite/rabbitmq \
    && chown -R minio:minio /var/lib/savebite/minio \
    && chown -R app:app /var/lib/savebite/keys \
    && chown app:app /var/lib/savebite
EXPOSE 80 5432 6379 5672 15672 9000 9001
VOLUME ["/var/lib/savebite"]
HEALTHCHECK --interval=10s --timeout=5s --start-period=120s --retries=12 \
    CMD curl -fsS http://127.0.0.1/api/health/ready >/dev/null && curl -fsS http://127.0.0.1/healthz >/dev/null || exit 1
ENTRYPOINT ["/opt/savebite/scripts/entrypoint.sh"]
