# Backend Project Template

A .NET 10 backend starter organized around DDD boundaries, vertical slices in the application layer, modular schemas, Redis caching, a transactional outbox with RabbitMQ dispatching, OpenTelemetry, and containerized development dependencies.

## Included

- `src/BLVK.Pickly.Logistics.Domain`: entities, repository/specification abstractions, and infrastructure-facing interfaces
- `src/BLVK.Pickly.Logistics.Application`: vertical slices for use cases, DTOs, handlers, and specifications
- `src/BLVK.Pickly.Logistics.Infrastructure`: EF Core, Redis cache, JWT, OTP delivery, telemetry, and other implementations
- `src/BLVK.Pickly.Logistics.DatabaseMigrator`: dedicated deployment-time migrator that runs pre-deploy SQL, EF Core migrations, seed data, and post-deploy SQL
- `src/BLVK.Pickly.Logistics.WebAPI`: controller-based HTTP host and presentation layer
- `src/BLVK.Pickly.Logistics.Consumer`: worker placeholder for async message consumption with readiness and liveness endpoints
- `src/BLVK.Pickly.Logistics.Jobs`: worker placeholder for scheduled work and transactional outbox dispatching with readiness and liveness endpoints
- `tests/unit/BLVK.Pickly.Logistics.Application.UnitTests`: unit tests for application handlers and use cases
- `tests/unit/BLVK.Pickly.Logistics.Domain.UnitTests`: unit tests for domain entities and behavior
- `tests/unit/BLVK.Pickly.Logistics.Infrastructure.UnitTests`: unit tests for infrastructure components
- `tests/unit/BLVK.Pickly.Logistics.WebAPI.UnitTests`: unit tests for WebAPI-specific helpers
- `tests/unit/BLVK.Pickly.Logistics.Consumer.UnitTests`: unit tests for consumer worker behavior
- `tests/unit/BLVK.Pickly.Logistics.Jobs.UnitTests`: unit tests for scheduled jobs worker behavior
- `tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests`: WebAPI integration tests using PostgreSQL and Redis testcontainers
- `tests/integration/BLVK.Pickly.Logistics.Consumer.IntegrationTests`: consumer host integration tests using PostgreSQL, RabbitMQ, and Redis testcontainers
- `tests/integration/BLVK.Pickly.Logistics.Jobs.IntegrationTests`: jobs host integration tests using PostgreSQL, RabbitMQ, and Redis testcontainers

## Architecture Notes

- Domain owns entities plus contracts such as repositories, cache interfaces, token generation, and OTP delivery
- Application keeps vertical slices by feature and depends only on the domain
- Infrastructure contains EF Core persistence, Redis caching, JWT generation, observability, and other implementation details
- Asynchronous integration messages are persisted through a transactional outbox and dispatched from the Jobs service through RabbitMQ
- Database changes are applied by a separate migrator service before the other services are deployed
- The migrator exposes readiness and liveness endpoints so deployment orchestration can distinguish between startup and completed database work
- WebAPI is only the presentation host and endpoint mapping layer
- Schemas are separated by domain using `authentication` and `reference_data`
- `TimeProvider` is the standard time abstraction used across handlers and infrastructure

## Template Usage

Install the template from the repository root:

```powershell
dotnet new install .
```

Create a new solution:

```powershell
dotnet new backend-template --organizationAbbreviation CN --clientName Acme --clientProjectName Ordering -o .\CN.Acme.Ordering
```

This creates a fresh project tree without the template repository's `.git` history.

The generated root name becomes `{OrganizationAbbreviation}.{ClientName}.{ClientProjectName}` and is applied to the solution, projects, folders, and namespaces. The organization abbreviation is intended for short forms such as `CN` and should be at most 3 characters.

If you want an interactive prompt instead of typing the parameters yourself, run:

```powershell
.\scripts\New-BackendProject.ps1
```

The script prompts for organization abbreviation, client name, and client project name, then installs the local template and creates the solution for you. If you leave organization blank, it defaults to `CN`.
When `git` is available on `PATH`, the script also initializes a new repository in the generated project directory.

There is also a bash version:

```bash
./scripts/New-BackendProject.sh
```

It supports the same inputs and generated naming convention.
When `git` is available on `PATH`, it also initializes a new repository in the generated project directory.

## Local Development

Restore, build, and test:

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
dotnet restore
dotnet build
dotnet test
```

## Cloudflare R2 avatar uploads

Avatar files are uploaded by the browser directly to the private R2 bucket through a 10-minute presigned `PUT`. The API validates the quarantined object and conditionally copies the exact validated ETag into the public bucket before updating the stakeholder profile.

Configure `ObjectStorage:CloudflareR2` with the R2 S3 API endpoint, public and private bucket names, credentials, an application folder, and the public CDN/custom-domain base URL. `PublicBaseUrl` is used only for completed public objects; presigned uploads always use the S3 API endpoint.

The private bucket also needs environment-specific CORS configuration. Allow `PUT` from each frontend origin, allow the `Content-Type` request header, and optionally expose `ETag`. Do not use a wildcard production origin unless that access is intentional.

Configure an R2 lifecycle rule that deletes objects under this prefix after a short retention period:

```text
{ApplicationFolder}/quarantine/avatars/
```

After completion, the API records a `DeleteQuarantinedObject` command in the outbox with the upload state change. The Consumer handles that reusable command and deletes the private object through the configured storage provider. The lifecycle rule remains the fallback for abandoned uploads or commands that exhaust their retries. Keep the private bucket non-public; only validated copies belong in the configured public bucket.

The presigned upload workflow is implemented by `FileUploadService`. Feature modules provide an `IFileUploadPolicy` that defines their size limit, allowed content types and extensions, signature validation, expiry, object-key layout, and whether the validated object is promoted to public or private storage. All modules persist the common lifecycle in `FileUploadSessions`, scoped by tenant, owner type, owner ID, purpose, and policy key. Each module supplies a server-controlled owner-type string, such as `stakeholder` or `contract`, so IDs from different aggregate types cannot be confused. `InitiatedByStakeholderId` separately records the actor that started the upload when one exists and remains nullable for system-initiated uploads. A module creates its own association record only when the completed file has additional domain meaning, such as a KYC document or contract attachment referencing the shared session. Avatars need no association table because completion updates the stakeholder profile directly. `AvatarUploadPolicy` is the profile-avatar implementation; document modules can provide their own policy without duplicating session columns, storage validation, or promotion logic.

## Git workflow automation

The Windows PowerShell workflow command automates branch creation, validation,
AI-assisted commit and pull-request drafting, publishing, and returning to an
updated `main` after merge. It requires Git, GitHub CLI (`gh`), the .NET 10 SDK,
and an installed and authenticated Codex CLI. On Windows, the workflow selects a
Codex executable that has its matching sandbox helper, including the version
bundled with the Codex VS Code extension.

This repository publishes through the `chukwuemekanweke` GitHub account without
switching the globally active `gh` login. Git pushes use the
`github-chukwuemekanweke` SSH host alias. Pull-request commands read the
account-specific token from an encrypted local credential and verify the account
before doing any remote work.

Configure the SSH alias in `%USERPROFILE%\.ssh\config`:

```sshconfig
Host github-chukwuemekanweke
    HostName github.com
    User git
    IdentityFile ~/.ssh/id_ed25519_chukwuemekanweke
    IdentitiesOnly yes
```

Configure this clone to use that alias if its `origin` still uses HTTPS:

```powershell
git remote set-url origin git@github-chukwuemekanweke:chukwuemekanweke/dotnet-core-project-template.git
```

Save the PAT once after cloning the repository:

```powershell
.\scripts\git-workflow.ps1 setup-auth
```

Use a PAT that can manage this repository. The command validates the account
before saving the credential under
`%LOCALAPPDATA%\BLVK.Pickly.Logistics`. Windows encrypts the token for the
current user on the current computer, so it is loaded automatically on later
workflow runs. During publishing, the workflow also exposes that credential as
`GITHUB_USERNAME` and `GITHUB_PAT` for authenticated package access.
`CHUKWUEMEKANWEKE_GITHUB_TOKEN` remains available as a temporary override for the
current shell. Do not add the private key, token, or encrypted credential file to
this repository.

Start a feature branch from the latest `origin/main`:

```powershell
.\scripts\git-workflow.ps1 start -Epic 12 -Feature 34 -Label "user-profile"
```

This creates a branch named `epic-12/be-34-user-profile`. For work without an
epic or user story, provide only a branch name:

```powershell
.\scripts\git-workflow.ps1 start my-branch
# Equivalent named option:
.\scripts\git-workflow.ps1 start -BranchName "my-branch"
```

Standalone names are normalized under `feature/cn/`: `my-branch` and
`feature/my-branch` become `feature/cn/my-branch`, while
`feature/cn/my-branch` remains unchanged.

After making changes, stage only the intended files and publish them:

```powershell
git add src tests
git diff --cached
.\scripts\git-workflow.ps1 publish
```

`publish` builds the solution and then runs the tests using the existing restore.
Run `dotnet restore` separately after dependency changes. Codex inspects a
disposable Git worktree containing the branch history and staged diff, and
drafts a Conventional Commit message, PR title, and completed
`.github/pull_request_template.md`. The temporary worktree is removed before the
command pauses for approval of the commit message and again for editing or
approval of the PR. PRs are drafts by default; pass `-Ready` to create a
ready-for-review PR, or `-SkipChecks` only when the checks have already been run
separately. If a push or PR creation is interrupted, run `publish` again; with
no staged changes, it resumes PR creation from the commits already on the
feature branch.

Once GitHub reports the current branch's PR as merged, update `main` with:

```powershell
.\scripts\git-workflow.ps1 finish
```

The local feature branch is deliberately preserved. Use `status` for Git and PR
status, or `check` to run the repository validation suite without publishing.

Run the database migrator on its own:

```powershell
dotnet run --project src/BLVK.Pickly.Logistics.DatabaseMigrator
```

The migrator executes scripts in:

- `src/BLVK.Pickly.Logistics.DatabaseMigrator/Scripts/PreDeploy`
- `src/BLVK.Pickly.Logistics.DatabaseMigrator/Scripts/PostDeploy`

In `docker compose`, the migrator stays running and only becomes healthy after the database work completes. The other services depend on that liveness state and will not start while the migrator is still unhealthy or has failed.

The migrator health endpoints are:

- Readiness: `http://localhost:8080/health/readiness`
- Liveness: `http://localhost:8080/health/liveness`

`/health/readiness` returns healthy while the migrator is available to execute the deployment work. `/health/liveness` only returns healthy after pre-deploy SQL, EF migrations, seed data, and post-deploy SQL have all completed successfully.

Start the development stack with Neon and Grafana Cloud (the default):

```powershell
Copy-Item .env.example .env
# Fill in .env with your credentials, then:
docker compose up --build
```

The default Compose mode starts only the application services. PostgreSQL, Redis, RabbitMQ, RabbitScout, Prometheus, Tempo, Loki, Pyroscope, the OpenTelemetry Collector, and Grafana use their cloud variants and are not started locally. `DATABASE_URL` is used for migrations and writes, while `DATABASE_URL_POOLED` is used for reads. Both Neon `postgresql://` URLs and Npgsql connection strings are supported. `REDIS_CONNECTION_STRING` and the `RABBITMQ_*` variables configure the cloud Redis and RabbitMQ connections.

Grafana Cloud receives logs, metrics, and traces through its OTLP gateway. Copy `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_HEADERS` from the OpenTelemetry card in your Grafana Cloud stack. Profiles use the URL and credentials shown in the stack's Profiles details.

Cloud dependency links for the default mode:

- PostgreSQL: [Neon Console](https://console.neon.tech/)
- Redis: [Redis Cloud Console](https://cloud.redis.io/)
- RabbitMQ: [CloudAMQP API](https://api.cloudamqp.com/) and the [configured broker host](https://yak.lmq.cloudamqp.com/)

To use local PostgreSQL, Redis, RabbitMQ, and the Grafana observability stack instead, add the `local` profile and the checked-in local override:

```powershell
docker compose --profile local -f docker-compose.yml -f docker-compose.local.yml up --build
```

The override selects all local infrastructure addresses and does not require the Neon, cloud Redis, cloud RabbitMQ, or Grafana Cloud variables. Omit `--profile local` and `docker-compose.local.yml` to return to the cloud-default mode.

In local mode, the OpenTelemetry Collector tail-samples traces before they reach Tempo (errors and slow traces are always kept, normal traffic is sampled at `OTEL_TAIL_SAMPLING_PERCENTAGE`). Cloud mode exports directly to Grafana Cloud, where Adaptive Traces handles retention instead. See `docs/observability/tail-sampling.md`.

The PowerShell helper exposes the same choice through one parameter. It defaults to `Cloud`:

```powershell
# Neon + cloud Redis + cloud RabbitMQ + Grafana Cloud
.\docker-compose.local.ps1

# Local PostgreSQL + Redis + RabbitMQ + Grafana stack
.\docker-compose.local.ps1 -InfrastructureMode Local
```

Create the ignored helper by copying `docker-compose.env.example.ps1` to `docker-compose.local.ps1`, then fill in the cloud credentials. The single local override replaces all cloud infrastructure when `InfrastructureMode` is `Local`.

If you prefer an environment-exporting helper script, create your own ignored local script first.

For Bash:

1. Create `docker-compose.local.sh`.
2. Copy the contents of `docker-compose.env.example.sh` into `docker-compose.local.sh`.
3. Replace the placeholder values with your cloud values.
4. Run it with `Cloud` or `Local`; it defaults to `Cloud`.

Example:

```bash
cp docker-compose.env.example.sh docker-compose.local.sh
chmod +x docker-compose.local.sh
```

Use the Bash helper in cloud or local mode:

```bash
# Cloud infrastructure (default)
./docker-compose.local.sh
./docker-compose.local.sh Cloud

# Local PostgreSQL, Redis, RabbitMQ, and Grafana stack
./docker-compose.local.sh Local
```

For PowerShell, follow the same pattern with `docker-compose.local.ps1`:

1. Copy `docker-compose.env.example.ps1` to `docker-compose.local.ps1`.
2. Fill in its environment variable assignments.
3. Select all cloud or all local infrastructure through `InfrastructureMode`.

Run the local script instead of calling `docker compose` directly when you need those exported variables.

Useful endpoints:

- API: `http://localhost:8080`
- OpenAPI: `http://localhost:8080/openapi/v1.json`
- Metrics: `http://localhost:8080/metrics`
- Health: `http://localhost:8080/health`
- RabbitMQ: `amqp://localhost:5672`
- RabbitMQ Management: `http://localhost:15672`
- Grafana, Prometheus, Tempo, and Pyroscope: local URLs are available only when the `local` profile is enabled

The `consumer` and `jobs` containers expose internal `/health/readiness` and `/health/liveness` endpoints for orchestration. In `docker compose`, both services wait for the database migrator to complete successfully before starting.

## Profiling

Both observability modes include Grafana Pyroscope continuous profiling.

- In cloud mode, the `webapi`, `consumer`, and `jobs` containers push profiles to `PYROSCOPE_SERVER_ADDRESS` using the configured basic-auth credentials.
- In local mode, Grafana provisions a `Pyroscope` data source automatically and the containers push profiles to `http://pyroscope:4040`.
- The current profiling setup is container-only. Grafana's .NET profiler currently supports Linux on `amd64`, so `dotnet run` on Windows/macOS is not profiled by this configuration.

Open Grafana Cloud, or local Grafana at `http://localhost:3000` when the `local` profile is enabled, and use Profiles Drilldown or Explore to inspect:

- `blvkpicklylogistics.webapi`
- `blvkpicklylogistics.consumer`
- `blvkpicklylogistics.jobs`
