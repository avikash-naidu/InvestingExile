# Status

This file is the single source of truth for what InvestingExile contains. Every commit updates this file in the same commit.

## Now

- `backend/InvestingExile.sln` is a .NET 8 solution.
- Projects in that solution: InvestingExile.Domain, InvestingExile.Pipeline, InvestingExile.Api, InvestingExile.Tests.
- `InvestingExile.Domain` is the class library under `backend/`. It holds temporary `League`, `Item`, and `PriceSnapshot` classes for the price tables. `AppDbContext` is not in the project yet.
- Pipeline has no ingest command. Api has no item route. Tests check that the solution lists those four projects and no others, and that Domain is a class library with those temporary types and without `AppDbContext`.
- `.cursor/rules/build-plan.mdc` is the MVP build plan and loads in every chat. `.cursor/skills/investing-exile/` holds the slice workflow and the poe.ninja notes.
- Pull request #34 stays closed.

## Not built

`AppDbContext`, the EF Core migration, Postgres, price ingest, the item grid, scoring, patch notes, and hosting.

## Latest change

2026-10-04: Added `.cursor/rules/build-plan.mdc` and the `investing-exile` skill so the MVP build plan loads from the repo.
