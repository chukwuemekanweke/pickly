# AGENTS

This repository is a `.NET 10` backend template (DDD + vertical slices: `Domain`, `Application`,
`Infrastructure`, `WebAPI`, `Consumer`, `Jobs`, `Contracts`, `DatabaseMigrator`). Durable architecture,
testing, convention, infrastructure, workflow and domain knowledge lives in a small OKF knowledge base
at `.okf/knowledge/` (~22 concepts) — this file is a router into it, not the knowledge base itself.

## Before non-trivial repository work

1. Identify the paths you're likely to touch.
2. Query OKF for relevant knowledge before broad exploration — via the `okf` MCP server
   (`okf_load_bundle` on `.okf/knowledge` once per session, then `okf_query`/`okf_search` with a short
   query for the feature/domain/technology involved), or `okf search -q "<topic>"` / `okf show -detail`
   on the CLI.
3. Retrieve and follow applicable `governance: constraint` concepts; read `governance: context`
   concepts only when you need the "why."
4. **Do not enumerate, read, or inject the complete `.okf/knowledge` bundle into context for normal
   tasks.** A handful of targeted concepts per task is normal; the whole bundle is not.
5. Inspect the actual source code at the relevant paths — source code is authoritative when it
   conflicts with a concept.
6. Implement, then verify (see below).
7. **Mandatory, every time code under `src/`, `tests/`, or the build/PR/verification workflow
   changes:** check OKF for a concept covering what you changed, and update it (or create one) if
   anything in it is now stale, incomplete, or missing — see
   `.okf/knowledge/workflows/okf-maintenance.md`. This is part of "done," not optional cleanup. It's
   fine to conclude no edit is needed, but only after checking — don't skip the check because the
   change looks small.

`docs/` is human-facing documentation. Do not use it as routine agent context, and never source new
OKF concepts from it — only read/edit it when a task explicitly asks for documentation work or points
you at a specific document.

## Verification (always, in this order)

1. `dotnet format BLVK.Pickly.Logistics.slnx style --no-restore --verify-no-changes --diagnostics IDE0005`
2. `dotnet build BLVK.Pickly.Logistics.slnx --no-restore`
3. `dotnet test BLVK.Pickly.Logistics.slnx --no-build`

Never run `dotnet build` and `dotnet test` concurrently — parallel execution can leave `testhost`
processes holding DLL locks and cause transient build failures.

## Git output

- Commit messages must use Conventional Commit format.
- Keep the commit subject concise.
- Add at most one short explanatory body sentence when the subject alone is insufficient.
- For pull-request work, retrieve the applicable OKF workflow before drafting.

## Always

- Never commit or print secrets, credentials, connection strings, or API keys.
- Keep this file small — durable rules and explanations belong in `.okf/knowledge/`, not here.
