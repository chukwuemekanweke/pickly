---
type: workflow
title: Production Deployment to Hetzner
description: GitHub Actions deploys exact-SHA application images to the existing Hetzner Compose project after CI succeeds
resource: okf://knowledge/workflows/production-deployment
tags:
    - constraint
    - workflow
    - deployment
    - github-actions
    - docker-compose
    - production
governance: constraint
code_refs:
    - .github/workflows/ci.yml
sources:
    - kind: file
      path: .github/workflows/ci.yml
generated:
    at: "2026-10-19T00:00:00Z"
    by: codex
status: stable
---
## Trigger and ordering

Production deployment runs only for successful pushes to `main`, after every matrix leg of
`docker-build-push` succeeds. The job uses the GitHub `production` environment, a non-canceling
concurrency group, a remote `flock`, and the monotonically increasing GitHub run ID so an older run
cannot overwrite a newer release.

## SSH boundary

Host, user, port, encrypted private key, private-key passphrase, and pinned known-hosts data are
production environment secrets. The workflow loads the key into an ephemeral `ssh-agent` through a
non-interactive `SSH_ASKPASS` helper, unsets the key material before connecting, and removes the
temporary files and agent on exit. SSH uses batch mode, the dedicated identity only, and strict
host-key checking. Client keepalives cover the silent container-health polling periods so an idle
connection does not interrupt deployment. The deployment user must have non-interactive Docker
access; granting Docker access is effectively granting root-level control of the host and must be
an explicit server-administration decision.

## Deployment invariants

- `/opt/pickly/compose.yml`, `/opt/pickly/.env`, and `/opt/pickly/app.env` remain server-managed.
- `PICKLY_VERSION` is the full triggering Git SHA for all four application images; `latest` is never
  used for deployment.
- Compose configuration and required service names are checked before containers are changed.
- PostgreSQL must already be healthy and is never recreated by the deployment.
- The long-running `database-migrator` is recreated alone and must become healthy before it is
  stopped, removed, and followed by the application services.
- `webapi`, `consumer`, and `jobs` are recreated without dependencies and must all become healthy.
- Successful and previous deployed revisions are recorded for a simple manual rollback; database
  migrations are never automatically reversed.

## Public ingress

`api.pikly.org` terminates HTTPS at the separately managed `pickly-caddy` container. It runs the
official `caddy:2-alpine` image with host networking, uses `/opt/pickly/Caddyfile`, and persists ACME
state in the `pickly-caddy-data` and `pickly-caddy-config` Docker volumes. Caddy proxies public
requests to the WebAPI's loopback binding at `127.0.0.1:8080`; `/metrics` is deliberately answered
with `404` instead of being exposed publicly. The application deployment workflow leaves this
long-running ingress container untouched.
