---
type: workflow
title: Keeping the OKF Knowledge Base Current
description: Mandatory step - after every code change, check for and update/create the covering OKF concept before the task is done
resource: okf://knowledge/workflows/okf-maintenance
tags:
    - constraint
    - workflow
    - okf
    - knowledge-maintenance
governance: constraint
code_refs:
    - .okf/knowledge/**
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Mandatory: this is a required step, not optional guidance

Every task that changes code under `src/` or `tests/`, or changes build/PR/verification workflow, ends
with an explicit OKF check — this is part of "done," the same way running tests is. Do not skip it
because a change looks small.

1. **Check.** Query OKF for the area you just touched (`okf search -q "<topic>"` or the `okf_query`
   MCP tool). Identify which existing concept(s), if any, describe that area (via `code_refs` overlap
   with the paths you changed).
2. **Update if the concept is now wrong or incomplete.** If a covering concept exists and your change
   made any statement in it inaccurate, incomplete, or outdated (a new field, a changed pattern, a
   renamed type referenced in the concept, a new `code_refs` path that should be covered), edit that
   concept in place — do not leave it silently stale. This is required whenever the concept's claims
   no longer match the code, however small the drift looks.
3. **Create if nothing covers it.** If your change introduces a new durable pattern, boundary,
   convention, infrastructure integration, or domain/feature area that no existing concept's
   `code_refs` reach, add a new concept file (see "How to add a concept" below) rather than leaving it
   undocumented.
4. **Skip only when nothing changed that a concept would describe.** Routine feature work that
   exactly follows an already-documented convention (e.g. a new feature folder that matches
   [[vertical-slices]] exactly) needs no edit — but you must still have done step 1 to reach that
   conclusion, not assumed it.

## How to add a concept

1. Add a new concept file under the appropriate `.okf/knowledge/<category>/` folder, following the
   frontmatter shape used by the other concepts in that folder (`type`, `title`, `description`,
   `resource`, `tags`, `governance: constraint|context`, `code_refs`, `sources`, `generated`, `status`).
2. Keep concepts scoped to real paths — don't widen `code_refs` to the whole repo, and never point
   `code_refs` at `docs/**`.
3. Run `okf lint -strict` and fix anything it flags before finishing.

## Do not

- Do not import or duplicate `docs/` content into `.okf/knowledge/` — `docs/` is human-facing documentation, not agent knowledge (see AGENTS.md).
- Do not install an OKF git hook in this repository; knowledge updates are explicit/agent-driven for now, not automatic on commit.
- Do not re-run `okf init`; use `okf add`/manual edits for incremental changes.
