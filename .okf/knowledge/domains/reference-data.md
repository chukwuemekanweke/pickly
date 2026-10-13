---
type: domain
title: Reference Data Domain
description: Countries, currencies, and languages reference data
resource: okf://knowledge/domains/reference-data
tags:
    - context
    - domain
    - reference-data
governance: context
code_refs:
    - src/BLVK.Pickly.Logistics.Application/ReferenceData/**
    - src/BLVK.Pickly.Logistics.Domain/ReferenceData/**
    - src/BLVK.Pickly.Logistics.WebAPI/Features/ReferenceData/**
    - tests/unit/BLVK.Pickly.Logistics.Application.UnitTests/ReferenceData/**
    - tests/integration/BLVK.Pickly.Logistics.WebAPI.IntegrationTests/ReferenceData/**
sources:
    - kind: file
      path: src/BLVK.Pickly.Logistics.Domain/ReferenceData/Entities/Country.cs
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Scope

Read-mostly reference data: `Country`/`Currency`/`CountryCurrency` (payments/stakeholder-facing) and `Language` (stakeholder preferences, see [[stakeholders]]). Exposed via
`GetCountries`/`GetLanguages` features and seeded through `DatabaseMigrator` post-deploy scripts rather than created via API. Query paths use the specification pattern described in [[persistence]]
(`ReferenceData/Specifications`).

Follows the standard [[vertical-slices]] feature-folder shape and the [[unit-testing]]/[[integration-testing]] conventions.
