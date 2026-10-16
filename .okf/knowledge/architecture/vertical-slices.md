---
type: architecture
title: Vertical Slice Feature Organization
description: How Application and WebAPI code is organized by feature folder rather than technical layer, and what files a feature folder should contain
resource: okf://knowledge/architecture/vertical-slices
tags:
    - constraint
    - architecture
    - vertical-slice
    - webapi
    - application
governance: constraint
code_refs:
    - src/BLVK.Pickly.Logistics.Application/**/Features/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/**
sources:
    - kind: file
      path: AGENTS.md
    - kind: file
      path: src/BLVK.Pickly.Logistics.Application/Authentication/Features/SignUp/SignUpHandler.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Rule

Organize code by feature, not by technical type (no repo-wide `Commands/`, `Handlers/`, `Validators/` folders). Each feature gets its own folder containing its command/request, response, result, validator, and handler together.

## Reference layout

`src/BLVK.Pickly.Logistics.Application/Authentication/Features/SignUp/`:
`SignUpCommand.cs`, `SignUpResponse.cs`, `SignUpResult.cs`, `SignUpValidator.cs`, `SignUpHandler.cs`.

Matching WebAPI:
`src/BLVK.Pickly.Logistics.WebAPI/Features/Authentication/Registrations/RegistrationsController.cs` and `SignUpRequest.cs` in the same folder.

## Rules

- WebAPI request DTOs and request validators live in the matching controller's feature folder at the HTTP edge, not in Application.
- Feature-specific helper classes live in the same feature folder when only that feature uses them.
- WebAPI controllers are organized by feature folder and use resource-oriented routes.
- Prefer controller-based endpoints over minimal APIs.
- Existing top-level feature groupings: `Authentication`, `Stakeholders`, `Payments`, `Notifications`, `Providers`, `ReferenceData`, `Deliveries`, `Locations` — place new features under the matching domain, or propose a new
  top-level domain folder only when the feature genuinely doesn't belong to an existing one.

See [[dto-records]] for the DTO/record shape used inside feature folders, [[unit-testing]] and [[integration-testing]] for how tests mirror this same layout.
