---
type: workflow
title: Repository Verification Order
description: Format, then build, then test — always sequential, never parallel dotnet build/test
resource: okf://knowledge/workflows/verification
tags:
    - constraint
    - workflow
    - verification
    - build
    - test
governance: constraint
code_refs:
    - BLVK.Pickly.Logistics.slnx
    - .github/workflows/ci.yml
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Rule

Always run build and test sequentially. Never run `dotnet build` and `dotnet test` concurrently in this repository — parallel execution can leave `testhost` processes holding DLL locks and cause transient copy failures during build.

## Order

1. `dotnet format BLVK.Pickly.Logistics.slnx style --no-restore --verify-no-changes --diagnostics IDE0005`
2. `dotnet build BLVK.Pickly.Logistics.slnx --no-restore`
3. `dotnet test BLVK.Pickly.Logistics.slnx --no-build`

Run this as the standard verification flow after any code change, before considering work complete.
