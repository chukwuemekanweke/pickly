---
name: okf
description: Query this repository's OKF knowledge base (.okf/knowledge) for durable architecture, testing, convention, infrastructure, workflow and domain rules before non-trivial repository work. Use before writing or editing code under src/ or tests/, and before touching build/PR/verification workflow. Do not use for pure documentation edits under docs/.
---

# Repository OKF knowledge base

This repository keeps a small, curated, retrieval-oriented knowledge base at `.okf/knowledge/` for
coding agents (currently ~22 concepts). It is separate from `docs/`, which is human-facing
documentation and is **not** part of this workflow (see AGENTS.md).

The MCP server `okf` (configured in `.mcp.json`, backed by the `okf` CLI) exposes this bundle. Use it
like this:

## 1. Load the bundle once per session

Call `okf_load_bundle` with `path: ".okf/knowledge"` the first time you need OKF in a session — the
query/search tools below return empty results until the bundle is loaded.

## 2. Find relevant concepts — do not read the whole bundle

Before modifying code under `src/` or `tests/`, or touching build/PR/verification workflow, call
`okf_query` (or `okf_search`) with a short query built from the feature/domain/technology you're
about to touch (e.g. `"caching"`, `"authentication"`, `"outbox"`, `"unit test naming"`). Results are
scored and include each concept's `concept_path` (e.g. `infrastructure\caching.md`) and its
`governance` tag (`constraint` vs `context`, visible via `okf_get_concept` or by reading the file).

**Do not** call `okf_list_concepts` with a large limit and read every result, and do not read every
file under `.okf/knowledge/` "just in case" — that defeats the point of a curated, small knowledge
base. Two or three targeted queries per task is normal; dozens of concepts loaded for one task is not.

## 3. Read only the concepts that matched

`okf_context`'s token-budgeted snippet retrieval does not reliably return body content in this OKF
version (a known limitation of the installed `0.4.0` CLI — it resolves `resource:` URIs incorrectly
and returns empty snippets). Use the Read tool on the concept's file directly instead, e.g.
`.okf/knowledge/infrastructure/caching.md` — concept files are small, plain Markdown with YAML
frontmatter (`governance`, `code_refs`, `tags`, etc.).

Prioritize concepts tagged/`governance: constraint` — these are rules you must follow, not just
background. `governance: context` concepts are optional depth for when you need to understand *why*.

## 4. Do the work

Inspect the actual source code at the relevant paths, then implement, following any constraints you
retrieved.

## 5. Keep OKF current — mandatory, every time

Before you consider the task done, check OKF again for whatever you just changed (same query style as
step 2). If a concept covers that area and your change made anything in it stale, incomplete, or
wrong, edit that concept file now. If your change introduced a durable architectural boundary, testing
convention, infrastructure pattern, or domain shape that no existing concept's `code_refs` reach,
create a new concept file instead of leaving it undocumented. See
`.okf/knowledge/workflows/okf-maintenance.md` for the exact steps and the frontmatter shape to use.

This check runs every time you touch `src/` or `tests/`, not only for changes that look architectural
— it's fine to conclude no edit is needed, but only after checking. Do not create new concepts that
just restate what a source file already says, and never source new concepts from `docs/`.
