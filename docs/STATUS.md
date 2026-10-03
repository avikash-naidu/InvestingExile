# Status

This file is the single source of truth for what InvestingExile contains. Every commit updates this file in the same commit.

## Now

- `backend/InvestingExile.sln` is a .NET 8 solution.
- Projects in that solution: InvestingExile.Domain, InvestingExile.Pipeline, InvestingExile.Api, InvestingExile.Tests.
- `InvestingExile.Domain` is the class library under `backend/`. It holds temporary `League`, `Item`, and `PriceSnapshot` classes for the price tables. `AppDbContext` is not in the project yet.
- Pipeline has no ingest command. Api has no item route. Tests check that the solution lists those four projects and no others, and that Domain is a class library with those temporary types and without `AppDbContext`.
- Pull request #34 stays closed.

## Not built

`AppDbContext`, the EF Core migration, Postgres, price ingest, the item grid, scoring, patch notes, and hosting.

## Latest change

2026-10-04: Added temporary `League`, `Item`, and `PriceSnapshot` classes in `InvestingExile.Domain`. The EF model and migration remain for issue #4.
