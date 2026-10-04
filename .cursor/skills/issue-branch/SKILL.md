---
name: issue-branch
description: >-
  Creates a git branch for a GitHub issue, sets that project task to In
  Progress, and links the branch to the issue before implementation starts.
  Use whenever the user starts a new task, a new issue, or asks to implement,
  continue, or work on a GitHub issue, including InvestingExile slice work.
---

# Issue branch

GitHub issue operations live in `docs/agents/issue-tracker.md`. This skill is the start-of-work procedure: branch, project Status, and the link between them.

Before starting on any new issue, create a new branch to track that issue. Do this before the first edit.

## Before editing

1. Read the issue number from the user or with `gh issue view`.
2. If the current branch already tracks that issue, stay on it. Otherwise start from `main` and create the branch:

   `git checkout main`
   `git checkout -b <number>-<short-slug>`

   Match existing names, such as `32-a1-create-investingexileapi`.

3. On the GitHub Project **InvestingExile MVP** (project `1`, owner `avikash-naidu`), set that issue's Status to `In Progress`:

   `gh project item-edit 1 --owner avikash-naidu --url <issue-url> --field "Status" --value "In Progress"`

4. Associate that branch with the issue, then point the local branch at the linked remote branch:

   `gh issue develop <number> --name <branch> --base main`
   `git branch -u origin/<branch>`

   Do not pass `--checkout`. The branch already exists locally. If `gh issue develop --list <number>` already shows that branch, skip the develop command and only set the upstream.

5. Then implement only that issue.

Do not start the work on `main`.
