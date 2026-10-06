# Status

This file is the single source of truth for what InvestingExile contains. Every commit updates this file in the same commit.

## Now

- `backend/InvestingExile.sln` is a .NET 8 solution.
- Projects in that solution: InvestingExile.Domain, InvestingExile.Pipeline, InvestingExile.Api, InvestingExile.Tests.
- `InvestingExile.Domain` is the class library under `backend/`. It holds `League`, `Item`, `PriceSnapshot`, and `AppDbContext`. Item identity is category + name + variant. `DetailsId` is stored and is not the key. `PriceSnapshot` is keyed by league, item, and UTC hour bucket. A timestamp written into that key is truncated to the UTC hour, and the table rejects a value that is not already on that hour. The snapshot stores chaos value, divine value, and listing count. One migration, `AddLeagueItemPriceSnapshot`, creates those three tables.
- `InvestingExile.Pipeline` is a .NET 8 console app under `backend/`. It references Domain. Its `Program` entry point does not fetch prices. The ingest command is still issue #5.
- `InvestingExile.Api` is the .NET 8 HTTP read API under `backend/`. It references Domain. Its `Program` builds and runs with no routes. Swagger uses OpenAPI 3.0.1. `GET /items` is still issue #8.
- `InvestingExile.Tests` is the xUnit project under `backend/`. It does not hold the double-run poe.ninja fixture; that test is still issue #6.
- `docker-compose.yml` runs Postgres 16. The database name is `investingexile`. The API, pipeline, and frontend are not in that compose file.
- Tests check that the solution lists those four projects and no others, that Domain is a class library with the price model and `AppDbContext`, that an item is unique on category + name + variant, that a snapshot is unique on league + item + hour bucket, that one migration creates those three tables, that Pipeline is that console app without a poe.ninja call, that Api is that web host without an item route, that the xUnit project has no saved poe.ninja JSON fixture, and that the compose file uses `postgres:16` with database `investingexile`.
- `.cursor/skills/issue-branch/` says to create a branch for the GitHub issue, set that InvestingExile MVP task to In Progress, and link the branch to the issue before the first edit.
- `.cursor/rules/build-plan.mdc` is the MVP build plan and loads in every chat. `.cursor/skills/investing-exile/` holds the slice workflow and the poe.ninja notes.
- `.cursor/rules/agent-skills.mdc` and `AGENTS.md` tell skills in `.agents/skills/` to read `docs/agents/cursor-skills.md` before they act. That file names `.cursor/skills/issue-branch/`, `.cursor/skills/investing-exile/`, and `.cursor/skills/project-status/`. Tracker, labels, and domain docs stay in `docs/agents/`.
- `.gitignore` leaves `.agents/` and `skills-lock.json` untracked. Those are the installed Matt Pocock skill pack. It also leaves the local teaching files untracked: `lessons/`, `reference/`, `assets/`, `MISSION.md`, `RESOURCES.md`, and `NOTES.md`.
- Pull request #34 stays closed.

## Not built

Price ingest, the item grid, scoring, patch notes, and hosting. The compose file does not run the API or the pipeline.

## Latest change

2026-10-06: `.gitignore` leaves the local teaching files untracked, including `lessons/`.
