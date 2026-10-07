# InvestingExile

## What this is

InvestingExile is a league-start investment tracker for Path of Exile. In the first hours of a new league, currency, fragments, and other exchange items move as players learn the economy. This project keeps those prices from poe.ninja, then uses earlier leagues and later patch notes to show which items are worth buying and why.

You use it to decide what to buy at league start: see an item's chaos price and divine price, how that price moved in previous leagues, and whether a patch changed the item or a mechanic it depends on.

This page is what a visitor reads. Agents keep the running record in [docs/STATUS.md](docs/STATUS.md).

## Plan

The initial plan, in slice order. Later slices add to Completed below. They leave this plan as it was written.

- **A1 — Prices in Postgres.** Store league prices in Postgres 16, database `investingexile`: a league, an item (category + name + variant), and an hourly price snapshot. One migration creates those tables. A price ingest command reads poe.ninja and upserts the current UTC hour. A test runs that ingest twice against a saved poe.ninja fixture and checks the second run updates the snapshot in place.
- **A2 — Item grid.** `GET /items` returns the latest snapshot. A Vite React grid shows each item's icon, name, chaos price, divine price, and sparkline.
- **B — Tested score.** `docs/SCORING.md` is the only score formula. Two-league fixtures assert direction and magnitude. An item seen in only one league has no score and still renders.
- **C — Patch notes and overrides.** A separate patch-note command records a change against an item or a mechanic, and mechanic flags reach the items they affect. A shared-secret override changes what `GET /items` returns and leaves the stored investment score unchanged.
- **D — Smallest host.** Docker images for the API and the pipeline, the built frontend as static files, and compose running Postgres, the API, and a one-shot pipeline. The README names one small host. GitHub Actions runs the tests and builds the images.

## Completed

After A1, the repo contains:

- Prices in Postgres 16, database `investingexile`. Tables `League`, `Item`, and `PriceSnapshot` come from one migration. An item is category + name + variant. A snapshot is league + item + UTC hour, and it stores chaos value, divine value, and listing count.
- The price ingest command: `dotnet run --project InvestingExile.Pipeline -- --league "<name>"`. It fetches poe.ninja prices and, in one transaction, upserts the league, the items, and the snapshot for the current UTC hour. Running it again in that same hour updates the snapshot in place.
- The double-run test in `InvestingExile.Tests`. It runs that ingest twice against Testcontainers Postgres 16, using a saved poe.ninja JSON fixture for the HTTP body, and checks that one league, one item, and one snapshot remain.
