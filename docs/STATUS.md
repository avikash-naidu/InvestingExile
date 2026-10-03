# Status

This file is the single source of truth for what InvestingExile contains. Every commit updates this file in the same commit.

## Now

- `backend/InvestingExile.sln` is a .NET 8 solution on branch `29-a1-create-backendinvestingexilesln`.
- Projects in that solution: InvestingExile.Domain, InvestingExile.Pipeline, InvestingExile.Api, InvestingExile.Tests. They are the default templates.
- Domain has no entities. Pipeline has no ingest command. Api has no item route. Tests check that the solution lists those four projects and no others.
- Pull request #34 was closed without merging. `main` does not contain the solution.

## Not built

Postgres, price ingest, the item grid, scoring, patch notes, and hosting.

## Latest change

2026-10-03: Closed pull request #34. Added this file, the `project-status` skill, and an always-on rule so each later commit records its change here. The README points at this file.
