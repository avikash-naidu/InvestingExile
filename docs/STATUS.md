# Status

This file is the single source of truth for what InvestingExile contains. Every commit updates this file in the same commit.

## Now

- `backend/InvestingExile.sln` is a .NET 8 solution.
- Projects in that solution: InvestingExile.Domain, InvestingExile.Pipeline, InvestingExile.Api, InvestingExile.Tests.
- `InvestingExile.Domain` is the class library under `backend/`. It holds temporary `League`, `Item`, and `PriceSnapshot` classes for the price tables. `AppDbContext` is not in the project yet.
- `InvestingExile.Pipeline` is a .NET 8 console app under `backend/`. It references Domain. Its `Program` entry point does not fetch prices. The ingest command is still issue #5.
- Api has no item route. Tests check that the solution lists those four projects and no others, that Domain is a class library with those temporary types and without `AppDbContext`, and that Pipeline is that console app without a poe.ninja call.
- `.cursor/rules/build-plan.mdc` is the MVP build plan and loads in every chat. `.cursor/skills/investing-exile/` holds the slice workflow and the poe.ninja notes.
- Pull request #34 stays closed.

## Not built

`AppDbContext`, the EF Core migration, Postgres, price ingest, the item grid, scoring, patch notes, and hosting.

## Latest change

2026-10-04: Made `InvestingExile.Pipeline` the console host for price ingest, with a Domain reference and no ingest command yet.
