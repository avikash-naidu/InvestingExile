# Domain Docs

Before using this file, read `docs/agents/cursor-skills.md` and every Cursor skill it names.

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Already written

Read these before exploring. They are the current product language.

- **`.cursor/rules/build-plan.mdc`**: product, slice order, stack, data rules, and scope.
- **`docs/STATUS.md`**: what the repo contains now, and which slice is next.
- **`.cursor/skills/investing-exile/reference.md`**: ingest, entities, and named-slice notes.

## Before exploring, read these

- **`GLOSSARY.md`** at the repo root, or
- **`GLOSSARY-MAP.md`** at the repo root if it exists: it points at one `GLOSSARY.md` per context. Read each one relevant to the topic.
- **`docs/adr/`**: read ADRs that touch the area you're about to work in. In multi-context repos, also check `src/<context>/docs/adr/` for context-scoped decisions.

If `GLOSSARY.md` or `docs/adr/` do not exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure

Single-context repo:

```
/
├── GLOSSARY.md
├── docs/adr/
├── docs/STATUS.md
└── .cursor/rules/build-plan.mdc
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `GLOSSARY.md`. Until that file exists, use the terms in `.cursor/rules/build-plan.mdc` (`League`, `Item`, `PriceSnapshot`, `InvestmentScore`, chaos, divine). Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, or a data rule in `.cursor/rules/build-plan.mdc`, surface it explicitly rather than silently overriding:

> _Contradicts the build plan (InvestmentScore is derived), but worth reopening because…_
