---
type: workflow
title: Pull Request Workflow
description: Mandatory evidence-based workflow for complete branch PR titles and descriptions
resource: okf://knowledge/workflows/pull-requests
tags: [constraint, workflow, pull-requests, git]
governance: constraint
code_refs:
  - .github/pull_request_template.md
sources:
  - kind: file
    path: AGENTS.md
generated:
  at: "2026-09-24T00:00:00Z"
  by: human
status: stable
---

## Mandatory workflow

When asked for a PR description, PR content, PR summary, PR title, or equivalent:

1. Determine the branch being described.
2. Determine the base branch, normally `main`, unless repository state or the
   user's instruction establishes another base.
3. Find the merge base between the branch and base.
4. Inspect every commit introduced by the branch.
5. Inspect the complete branch diff from the merge base through the branch tip.
6. Read `.github/pull_request_template.md` and use it as the required PR
   description structure.
7. Complete the template from repository evidence and return ready-to-paste
   Markdown.

The description must cover the branch's full scope. Do not summarize only the
latest commit, staged or unstaged changes, files changed in the current session,
or any other partial working-tree view.

## Evidence and writing rules

- Do not invent tests, implementation details, risks, screenshots, dependencies,
  configuration changes, or behavioral changes.
- If a template section does not apply, say so concisely instead of manufacturing
  content.
- Write directly and naturally, as one engineer explaining the change to
  another. Avoid marketing language, generic filler, exaggerated claims,
  repetitive summaries, and wording that sounds generated.
- Preserve technical implementation details and trade-offs when they materially
  help reviewers.
- Prefer a concise Conventional Commit-style PR title that states the main change
  and affected scope, such as `feat(scope): summary`.
