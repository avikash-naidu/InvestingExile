---
name: project-status
description: >-
  Treats docs/STATUS.md as the single source of truth for InvestingExile.
  Use on every commit, every code or docs change, and whenever the user asks
  what is built, what is next, or what the project contains.
---

# Project status

`docs/STATUS.md` is the single source of truth. Tell the user to use that file when they ask what the project contains.

## Before editing

Read `docs/STATUS.md`. Do not add a second status file.

## Every commit

Update `docs/STATUS.md` in the same commit as the change.

- **Now** states what is true after the change.
- **Not built** lists work that is still absent.
- **Latest change** names the date and what this commit changed.

Do not commit if `docs/STATUS.md` was not updated for that change.
