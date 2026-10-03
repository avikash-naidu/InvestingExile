# Status

This file is the single source of truth for what InvestingExile contains. Every commit updates this file in the same commit.

## Now

- `main` contains `backend/InvestingExile.sln`, a .NET 8 solution.
- Projects in that solution: InvestingExile.Domain, InvestingExile.Pipeline, InvestingExile.Api, InvestingExile.Tests. They are the default templates.
- Domain has no entities. Pipeline has no ingest command. Api has no item route. Tests check that the solution lists those four projects and no others.
- Pull request #34 stays closed. `main` was fast-forwarded to the same commits.

## Not built

Postgres, price ingest, the item grid, scoring, patch notes, and hosting.

## Latest change

2026-10-03: Fast-forwarded `main` to the .NET 8 solution, this status file, and the `project-status` skill. Pull request #34 stays closed.
