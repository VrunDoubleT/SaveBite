# MinIO Community is source-only; build upstream rather than use a removed image.
FROM golang:1.25-alpine AS build
ARG MINIO_VERSION=RELEASE.2025-10-15T17-29-55Z
RUN apk add --no-cache git ca-certificates
RUN CGO_ENABLED=0 go install github.com/minio/minio@${MINIO_VERSION}
FROM alpine:3.22
RUN apk add --no-cache ca-certificates curl && addgroup -g 10001 minio && adduser -D -u 10001 -G minio minio && mkdir /data && chown minio:minio /data
COPY --from=build /go/bin/minio /usr/local/bin/minio
USER minio
EXPOSE 9000 9001
ENTRYPOINT ["minio"]
CMD ["server", "/data", "--console-address", ":9001"]
