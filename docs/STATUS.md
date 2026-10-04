# Status

This file is the single source of truth for what InvestingExile contains. Every commit updates this file in the same commit.

## Now

- `backend/InvestingExile.sln` is a .NET 8 solution.
- Projects in that solution: InvestingExile.Domain, InvestingExile.Pipeline, InvestingExile.Api, InvestingExile.Tests.
- `InvestingExile.Domain` is the class library under `backend/`. It holds temporary `League`, `Item`, and `PriceSnapshot` classes for the price tables. `AppDbContext` is not in the project yet.
- `InvestingExile.Pipeline` is a .NET 8 console app under `backend/`. It references Domain. Its `Program` entry point does not fetch prices. The ingest command is still issue #5.
- `InvestingExile.Api` is the .NET 8 HTTP read API under `backend/`. It references Domain. Its `Program` builds and runs with no routes. Swagger uses OpenAPI 3.0.1. `GET /items` is still issue #8.
- Tests check that the solution lists those four projects and no others, that Domain is a class library with those temporary types and without `AppDbContext`, that Pipeline is that console app without a poe.ninja call, and that Api is that web host without an item route.
- `.cursor/skills/issue-branch/` says to create a branch for the GitHub issue, set that InvestingExile MVP task to In Progress, and link the branch to the issue before the first edit.
- `.cursor/rules/build-plan.mdc` is the MVP build plan and loads in every chat. `.cursor/skills/investing-exile/` holds the slice workflow and the poe.ninja notes.
- Pull request #34 stays closed.

## Not built

`AppDbContext`, the EF Core migration, Postgres, price ingest, the item grid, scoring, patch notes, and hosting.

## Latest change

2026-10-04: Made `InvestingExile.Api` the HTTP read host, with a Domain reference and no item route yet. Its Swagger description is OpenAPI 3.0.1 so the bundled Swagger UI can render it. New tasks start on a branch linked to the issue, with that project task set to In Progress.
