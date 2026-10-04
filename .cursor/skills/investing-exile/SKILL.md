---
name: investing-exile
description: >-
  Builds the InvestingExile Path of Exile league-start app one slice at a time.
  Use when the user mentions slice or session A1, A2, B, C, or D, poe.ninja
  ingest, investment score, patch notes, the MVP build plan, or asks to
  continue InvestingExile.
---

# InvestingExile slice

## Start

1. Read `docs/STATUS.md`. If the user asked for a later slice than the one that file describes as current, stop and say which slice is next.
2. Read [reference.md](reference.md) when the work is ingest, entities, or a named slice prompt.
3. The task list is the GitHub Project **InvestingExile MVP**. Work only the open sub-issues of the current slice. Do not invent issues.

## Finish

1. Run `dotnet test` from `backend/`. If frontend changed, run the frontend test script too.
2. Close each sub-issue whose done-when passed, then close the parent issue. Leave any failed task open and say so.
3. Update `docs/STATUS.md`: what is true now, the next slice name, and that slice's parent issue number. Quote the done-when that passed.
4. Commit only when the user or the pasted prompt says to commit. One commit for the slice. The message says why the slice exists, not a file list.

## Stay inside the slice

- Search the repo before adding a project, entity, or endpoint.
- Do not start the next slice in this chat.
- Do not file extra tasks beyond the GitHub Project.
- Do not add dependencies listed in `.cursor/rules/build-plan.mdc`.
