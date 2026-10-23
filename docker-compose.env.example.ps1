param(
    [ValidateSet("Cloud", "Local")]
    [string] $InfrastructureMode = "Cloud"
)

$env:GITHUB_USERNAME = ""
$env:GITHUB_PAT = ""
$env:MAILTRAP_TOKEN = ""
$env:MAILTRAP_WEBHOOK_SIGNING_SECRET = ""
$env:MAILTRAP_FROM_ADDRESS = ""
$env:MAILTRAP_FROM_NAME = "BLVK.Pickly.Logistics"
$env:GOOGLE_AUTHENTICATION_ENABLED = "true"
$env:GOOGLE_CLIENT_ID = ""
$env:CORS_ALLOWED_ORIGINS_0 = "https://app.pikly.org"
$env:MAPBOX_ACCESS_TOKEN = ""
$env:GEOAPIFY_API_KEY = ""
$env:LOCATIONIQ_API_KEY = ""
$env:DATABASE_URL = "postgresql://<username>:<password>@<neon-host>/<database>?sslmode=require&channel_binding=require"
$env:DATABASE_URL_POOLED = "postgresql://<username>:<password>@<neon-pooler-host>/<database>?sslmode=require&channel_binding=require"
$env:CLOUDFLARE_R2_ENDPOINT = "https://<account-id>.r2.cloudflarestorage.com"
$env:CLOUDFLARE_R2_APPLICATION_FOLDER = "backend-project-template"
$env:CLOUDFLARE_R2_PUBLIC_BUCKET_NAME = ""
$env:CLOUDFLARE_R2_PRIVATE_BUCKET_NAME = ""
$env:CLOUDFLARE_R2_ACCESS_KEY_ID = ""
$env:CLOUDFLARE_R2_SECRET_ACCESS_KEY = ""
$env:CLOUDFLARE_R2_PUBLIC_BASE_URL = "https://<public-r2-domain>"
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "https://otlp-gateway-<zone>.grafana.net/otlp"
$env:OTEL_EXPORTER_OTLP_HEADERS = "Authorization=Basic%20<base64-credentials>"
$env:OTEL_EXPORTER_OTLP_PROTOCOL = "http/protobuf"
# Local OTel Collector tail sampling (local profile only; unused in cloud mode).
# $env:OTEL_TAIL_SAMPLING_LATENCY_MS = "1000"
# $env:OTEL_TAIL_SAMPLING_PERCENTAGE = "10"
$env:PYROSCOPE_SERVER_ADDRESS = "https://profiles-prod-<region>.grafana.net"
$env:PYROSCOPE_BASIC_AUTH_USER = "<profiles-instance-id>"
$env:PYROSCOPE_BASIC_AUTH_PASSWORD = "<access-policy-token>"
$env:RABBITMQ_HOSTNAME = "<cloud-rabbitmq-host>"
$env:RABBITMQ_PORT = "5672"
$env:RABBITMQ_USERNAME = "<cloud-rabbitmq-username>"
$env:RABBITMQ_PASSWORD = "<cloud-rabbitmq-password>"
$env:RABBITMQ_VIRTUAL_HOST = "/"
$env:REDIS_CONNECTION_STRING = "<cloud-redis-host>:<port>,password=<password>,ssl=True,abortConnect=False"

$composeArguments = @("compose", "-f", "docker-compose.yml")

if ($InfrastructureMode -eq "Local") {
    $composeArguments += @(
        "--profile", "local",
        "-f", "docker-compose.local.yml"
    )
}

$composeArguments += @("up", "-d", "--build", "--force-recreate")

Push-Location $PSScriptRoot
try {
    & docker @composeArguments
    $dockerExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $dockerExitCode
