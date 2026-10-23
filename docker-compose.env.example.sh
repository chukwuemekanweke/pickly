#!/usr/bin/env bash

export GITHUB_USERNAME=""
export GITHUB_PAT=""
export MAILTRAP_TOKEN=""
export MAILTRAP_WEBHOOK_SIGNING_SECRET=""
export MAILTRAP_FROM_ADDRESS=""
export MAILTRAP_FROM_NAME="BLVK.Pickly.Logistics"
export GOOGLE_AUTHENTICATION_ENABLED="true"
export GOOGLE_CLIENT_ID=""
export CORS_ALLOWED_ORIGINS_0="https://app.pikly.org"
export MAPBOX_ACCESS_TOKEN=""
export GEOAPIFY_API_KEY=""
export LOCATIONIQ_API_KEY=""
export DATABASE_URL="postgresql://<username>:<password>@<neon-host>/<database>?sslmode=require&channel_binding=require"
export DATABASE_URL_POOLED="postgresql://<username>:<password>@<neon-pooler-host>/<database>?sslmode=require&channel_binding=require"
export CLOUDFLARE_R2_ENDPOINT="https://<account-id>.r2.cloudflarestorage.com"
export CLOUDFLARE_R2_APPLICATION_FOLDER="backend-project-template"
export CLOUDFLARE_R2_PUBLIC_BUCKET_NAME=""
export CLOUDFLARE_R2_PRIVATE_BUCKET_NAME=""
export CLOUDFLARE_R2_ACCESS_KEY_ID=""
export CLOUDFLARE_R2_SECRET_ACCESS_KEY=""
export CLOUDFLARE_R2_PUBLIC_BASE_URL="https://<public-r2-domain>"
export OTEL_EXPORTER_OTLP_ENDPOINT="https://otlp-gateway-<zone>.grafana.net/otlp"
export OTEL_EXPORTER_OTLP_HEADERS="Authorization=Basic%20<base64-credentials>"
export OTEL_EXPORTER_OTLP_PROTOCOL="http/protobuf"
# Local OTel Collector tail sampling (local profile only; unused in cloud mode).
# export OTEL_TAIL_SAMPLING_LATENCY_MS="1000"
# export OTEL_TAIL_SAMPLING_PERCENTAGE="10"
export PYROSCOPE_SERVER_ADDRESS="https://profiles-prod-<region>.grafana.net"
export PYROSCOPE_BASIC_AUTH_USER="<profiles-instance-id>"
export PYROSCOPE_BASIC_AUTH_PASSWORD="<access-policy-token>"
export RABBITMQ_HOSTNAME="<cloud-rabbitmq-host>"
export RABBITMQ_PORT="5672"
export RABBITMQ_USERNAME="<cloud-rabbitmq-username>"
export RABBITMQ_PASSWORD="<cloud-rabbitmq-password>"
export RABBITMQ_VIRTUAL_HOST="/"
export REDIS_CONNECTION_STRING="<cloud-redis-host>:<port>,password=<password>,ssl=True,abortConnect=False"

infrastructure_mode="${1:-Cloud}"

case "${infrastructure_mode}" in
  Cloud)
    docker compose -f docker-compose.yml up -d --build --force-recreate
    ;;
  Local)
    docker compose -f docker-compose.yml --profile local -f docker-compose.local.yml up -d --build --force-recreate
    ;;
  *)
    echo "Infrastructure mode must be Cloud or Local." >&2
    exit 1
    ;;
esac
