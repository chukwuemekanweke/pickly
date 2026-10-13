---
type: testing
title: Unit Testing Conventions
description: Naming, framework, layout and coverage rules for unit tests across Application, Domain, Infrastructure, WebAPI, Consumer and Jobs
resource: okf://knowledge/testing/unit-testing
tags:
    - constraint
    - testing
    - unit-tests
governance: constraint
code_refs:
    - tests/unit/**
sources:
    - kind: file
      path: AGENTS.md
generated:
    at: "2026-09-23T00:00:00Z"
    by: human
status: stable
---
## Projects

Separate unit test projects per production project: `Application.UnitTests`, `Domain.UnitTests`, `Infrastructure.UnitTests`, `WebAPI.UnitTests`, `Consumer.UnitTests`, `Jobs.UnitTests`.

## Layout

- Mirror the production project's feature-folder structure inside the matching test project (see [[vertical-slices]]). Application unit tests must not sit flat at the project root.
- Each feature gets its own test folder; one test case per file.
- If a controller exposes multiple actions, add a subfolder per endpoint under that controller's feature folder; a single-endpoint controller keeps its test directly in the feature folder.

## Naming

- File and class name: `When_{ActionUnderTest}_With{ParametersOfTest}_Should` — stop at `Should`. The asserted outcome goes only in the test method name, never appended to the file/class name.
- Example: file/class `When_CompletingPasswordReset_WithValidOtp_Should`, method `ResetPassword`.

## Tooling and style

- `NSubstitute` for mocks/substitutes/fakes, `Shouldly` for assertions.
- Prefer method-local scenario variables over repeated string literals; reuse helper factory methods/test context builders where available.
- Keep tests focused on the unit's behavior, not infrastructure wiring.

## Coverage requirement

Every WebAPI controller, every Application handler, every Consumer handler, and every Jobs background service must have unit tests covering happy path, failure paths, and edge cases. Add these when
you add or modify the corresponding production code, not as an afterthought.
