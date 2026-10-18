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
    - kind: file
      path: src/BLVK.Pickly.Logistics.Application/Deliveries/Features/CreateDelivery/CreateDeliveryHandler.cs
    - kind: file
      path: src/BLVK.Pickly.Logistics.WebAPI/Features/Deliveries/Bookings/DeliveryBookingsController.cs
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

Command/query reference (copy these, not a new shape):
`src/BLVK.Pickly.Logistics.Application/Deliveries/Features/CreateDelivery/` — `CreateDeliveryCommand.cs`,
`CreateDeliveryHandler.cs`, `CreateDeliveryResult.cs`, `CreateDeliveryStatus.cs`; read side `GetDelivery/` uses
`GetDeliveryQuery.cs`. WebAPI: `Features/Deliveries/Bookings/DeliveryBookingsController.cs` with
`CreateDeliveryRequest.cs` and `CreateDeliveryRequestValidator.cs`. Rider equivalents live under
`Application/Riders/Features/<OperationName>/` and `WebAPI/Features/Riders/`.

## One handler per operation (constraint)

- Every API operation and every background-job unit of work has its own feature folder with a dedicated
  `<Operation>Handler` exposing `HandleAsync(<Operation>Command|Query, CancellationToken)`.
- Each operation defines its own `<Operation>Command` (writes) or `<Operation>Query` (reads), `<Operation>Result`, and —
  when outcomes branch — `<Operation>Status` in its own file with explicit one-based values. Do not share a result or
  status type across operations.
- Controllers stay thin: validate the request DTO with its FluentValidation validator, build the command/query (including
  `ActorContext`), call the handler, and map `Result.Status` to HTTP inline. Controllers never call repositories,
  persistence ports, or `DbContext`.
- Jobs/Consumer hosts resolve the operation's handler from a fresh DI scope; scheduling, polling, and failure isolation
  stay in the host, business decisions stay in the handler.
- Handlers are concrete classes registered with `services.AddScoped<XHandler>()` in
  `Application/ServiceCollectionExtensions.cs`. Do not introduce MediatR or another dispatcher/CQRS framework.
- **Do not create centralized application services that bundle unrelated endpoint use cases** (e.g. an
  `XOperationsService` with one method per endpoint). Shared Application helpers (`DeliveryQuotePreparationService`,
  `StakeholderResolver`) are acceptable only when they encapsulate one genuinely reusable responsibility used by several
  handlers.
- Persistence ports follow the same rule: prefer purpose-specific interfaces in `Domain/<Area>/Persistence/` (one per
  transactional use case, e.g. `IDeliveryBookingPersistence`, `IRiderOfferAcceptancePersistence`) over a broad store
  owning unrelated operations. Transactions, row/advisory locks, isolation levels and provider-specific SQL stay in
  Infrastructure; restructuring must never weaken them.
- Do not infer that every handler needs a matching persistence interface. Straightforward aggregate access uses the
  existing `IRepository<T>` and specification pattern. A dedicated port must earn its place through atomic
  multi-aggregate coordination, provider-specific SQL/PostGIS/locking, a complex projection, or bounded bulk work.
- A dedicated handler must own the use case, not merely forward it. Eligibility rules, workflow branching, aggregate
  transitions, retry/expiry decisions and application result selection belong in the handler or a reusable Domain/
  Application policy. Infrastructure ports expose data-access facts and database capabilities; they must not return
  application status codes or choose business outcomes.
- It is valid for a handler to coordinate repositories plus a narrow transaction/lock abstraction. The abstraction
  defines the required atomic boundary; Infrastructure implements EF tracking, isolation, advisory locks, PostGIS and
  provider-exception translation. Database-backed predicates that must execute as a bounded set query stay behind a
  narrow read port and return facts/projections for the handler to interpret.

## Rules

- WebAPI request DTOs and request validators live in the matching controller's feature folder at the HTTP edge, not in Application.
- Feature-specific helper classes live in the same feature folder when only that feature uses them.
- WebAPI controllers are organized by feature folder and use resource-oriented routes.
- Prefer controller-based endpoints over minimal APIs.
- Existing top-level feature groupings: `Authentication`, `Stakeholders`, `Payments`, `Notifications`, `Providers`, `ReferenceData`, `Deliveries`, `Locations`, `Riders` — place new features under the matching domain, or propose a new
  top-level domain folder only when the feature genuinely doesn't belong to an existing one.

See [[dto-records]] for the DTO/record shape used inside feature folders, [[unit-testing]] and [[integration-testing]] for how tests mirror this same layout.
