# InvestingExile reference

Read this when implementing ingest, entities, or a named slice. The build plan rule stays in effect.

## poe.ninja

- Currency overview: `https://poe.ninja/api/data/currencyoverview?league={league}&type=Currency`
- Item overview: `https://poe.ninja/api/data/itemoverview?league={league}&type={type}`
- No API key. League is a CLI argument. Do not hardcode a league name.
- Currency row: `currencyTypeName`, `chaosEquivalent`, `receiveSparkLine` / `paySparkLine` (`data`, `totalChange`), `detailsId`. Listing count may be on the pay or receive side (`count`).
- Item row: `name`, `baseType`, `variant`, `chaosValue`, `divineValue`, `listingCount`, `sparkline.data`, `icon`, `detailsId`.
- Slice A categories: `Currency`, `Scarab`, `Fragment`. Stop there.
- Sparkline points from the payload are what the first grid draws. Repeated hourly snapshots are the long-term series.
- Chaos Orb's chaos value is 1. Divine's chaos value is `chaosEquivalent`.
- Hour bucket: truncate snapshot time to the UTC hour. Upsert the same league, item, and hour in place.
- Patch notes are HTML from the official site, stored raw, parsed only against a committed fixture. Not part of the price command.

## Slice prompts

Each prompt is one chat. Do not paste the next prompt into the same chat.

### A1 — prices in Postgres

Follow the investing-exile skill. Implement session A1 only.

Build docker-compose Postgres 16 and a price ingest command. The .NET 8 solution already exists.

Entities this session: League, Item, PriceSnapshot. One migration.

Ingest command: `dotnet run --project InvestingExile.Pipeline -- --league "<name>"`

Fetch poe.ninja currency overview plus Scarab and Fragment. Upsert items by category + name + variant. Write a PriceSnapshot for the current UTC hour. Running it twice in the same hour updates that snapshot and does not insert a second item or a second snapshot row.

Done when: InvestingExile.Tests proves the double-run rule against the compose database (or Testcontainers if compose is awkward in CI), using a saved poe.ninja JSON fixture for the HTTP body. A manual run against a real league name passed in also persists rows.

Do not build the React app, scoring, patch notes, auth, Redis, or cloud infra.

Close the A1 sub-issues that passed, then close the A1 parent. Update docs/STATUS.md to next session A2 and its parent issue number. Commit this session.

### A2 — item grid

Follow the investing-exile skill. Implement session A2 only. Do not start session B.

Read the rules and the existing ingest code before adding an endpoint.

Add GET /items. Return name, category, variant, icon, chaos value, divine value, listing count, sparkline points, and the snapshot hour. Read the latest snapshot per item. No cache.

Scaffold frontend/ with Vite, React, and TypeScript. Show a grid of item cards from that endpoint: icon, name, chaos price, divine price, and a sparkline.

Done when: with the API and `npm run dev` running, the grid shows rows already in the local database. Reloading the page shows the same items. dotnet test still passes.

Do not add filters, auth, admin pages, scoring, or hosting.

Close the A2 sub-issues that passed, then close the A2 parent. Update docs/STATUS.md to next session B and its parent issue number. Commit this session.

### B — tested score

Follow the investing-exile skill. Implement session B only. Do not start session C.

Write docs/SCORING.md first, in plain language, then implement that formula. Do not invent a second formula in code.

Formula to document and implement:

- Align each item's snapshots by whole hours since that league's start time.
- Compare the latest two leagues that have data for the item.
- Use chaos value as the price. Keep the divine/chaos ratio beside it so a divine spike is visible and is not treated as the item moving.
- Down-weight points with a low listing count.
- InvestmentScore stores direction (up, down, flat), a numeric magnitude, and the inputs that produced them (league ids, hours compared, chaos values, listing counts, divine ratio).

Seed the test from committed JSON fixtures for two leagues, not from a live poe.ninja call. The test asserts the exact direction and magnitude for those fixtures.

Show direction and magnitude on the existing item card.

Done when: `dotnet test` fails if the fixture score changes, and the grid shows the score for scored items. Items with only one league have no score and still render.

Do not parse patch notes. Do not add Redis, auth, or cloud hosting.

Close the B sub-issues that passed, then close the B parent. Update docs/STATUS.md to next session C and its parent issue number. Commit this session.

### C — patch notes and overrides

Follow the investing-exile skill. Implement session C only. Do not start session D.

Add a separate pipeline command for patch notes. Do not call it from the price ingest command.

Store the raw HTML. Parse a committed sample HTML file into PatchChange rows. change_type is only for obvious cases you put in the fixture (a buff keyword and a removal keyword). Expected rows for that file are asserted in a test.

A PatchChange must reference an item or a mechanic. Enforce that in the database. A mechanic-level change is returned for every item linked through ItemMechanic.

Add an admin override endpoint protected by one shared secret from configuration. Creating an override also writes an AuditLog row in the same database transaction. GET /items applies the override on read and does not change the InvestmentScore row. Test both of those.

Done when: the fixture parse test passes, a mechanic flag shows on each tagged item in GET /items, and the override test shows the API value changed while the score row did not.

Do not add an LLM client, user accounts, Cognito, or a watchlist.

Close the C sub-issues that passed, then close the C parent. Update docs/STATUS.md to next session D and its parent issue number. Commit this session.

### D — smallest host

Follow the investing-exile skill. Implement session D only.

Host the app that already runs locally. Do not redesign it.

Add Dockerfiles for the API and the pipeline. Serve the built frontend as static files. docker compose locally runs Postgres, the API, and a one-shot pipeline command. Document the exact commands in the README.

Add one GitHub Actions workflow that runs tests and builds the images. Do not add a deploy workflow, Terraform, Kubernetes, Redis, or Cognito.

If you recommend a cloud host, name one small option (a single VM or one container service plus managed Postgres) and stop after the README says how to point it at that host. Do not provision cloud resources unless credentials were already given and this chat explicitly says to apply them.

Done when: `docker compose up` serves the grid from the API container, and the workflow file runs tests on push.

Close the D sub-issues that passed, then close the D parent. Update docs/STATUS.md to say the MVP host is in place and that Redis, Cognito, and EKS are not scheduled. Commit this session.
