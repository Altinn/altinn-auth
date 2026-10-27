# Board set-up

*What the Team Autorisasjon board, the labels and the issue forms must look like for [ADR-0005](../adr/0005-planning-and-flow.md) to work. Drafted with AI assistance (Claude) on 2026-10-27; checked against the repository's labels and forms the same day. Human reader: @acn-dgopa. The work is tracked in [#4088](https://github.com/Altinn/altinn-auth/issues/4088). Each line is one change, so it can be answered "done" or "not possible" on its own.*

**TL;DR**

- Labels first, since the forms and the views depend on the names.
- Two fields to create, Iteration and Test phase; one to trim, Status.
- Seven views, each a filter and a column field, nothing else.
- Done when one feature has been walked through the board by hand and every card ended in Done.

## Labels

Managed in the repository settings; there is no labels file. The repository does not yet match ADR-0004.

- Rename `Testplan` → `kind/testplan`, `Blocked` → `status/blocked`, `kind/deploy_patch` → `kind/deploy-patch`. Renaming keeps the label on existing issues.
- Create `kind/incident`, `kind/security`, `kind/design`.
- Remove `status/duplicate` and `status/wontfix`; GitHub's close reasons replace them, as ADR-0004 decided.
- Retire `kind/bug`, `kind/feature-request` and `kind/user-story`; issue types replace them. Remove from issues first, then delete.
- One colour for all `kind/*`, another for `status/*`.

## Issue forms

In `.github/ISSUE_TEMPLATE/`. A form applies a label only if the label exists, so do the labels first.

- `6-testplan.yml`: label `kind/testplan` instead of `Testplan`. Add a checklist of the preparation steps: test data, tools, test cases, environment, then execution and result.
- `3-task.yml`: add a dropdown "Can be tested alone?" with yes, no, don't know.
- New `7-design.yml`, "Design task": type Task, labels `kind/design` and `status/triage`. Fields: TL;DR, the component dropdown as in the other forms, "User research needed before building?", "Tasks that wait for this design", "Good enough to build when".

## Project fields

- Status: remove the Test column. Keep Backlog, Ready, In progress, In review, Done. Set each column's description from the ADR, Decision 4.
- New iteration field `Iteration`: two weeks, first iteration starting Monday 2026-11-09. Create at least six ahead.
- New single select `Test phase`: Preparing, Executing, Retest. Only test tasks use it.
- Hide Estimate from all views. No area field, no horizon field.

## Views

Filter and column field, nothing else. Iteration is the project default. Remove views not in this list.

1. Iteration, board by Status: `iteration:@current`
2. Developers, board by Status: `type:Task,Bug,Enhancement -label:kind/testplan -label:kind/design iteration:@current`
3. Test board, board by Test phase: `label:kind/testplan is:open`
4. Design, board by Status: `label:kind/design is:open -status:Backlog`
5. Features, table with Sub-issues progress: `type:Epic,Feature is:open`
6. Area, one per area, board by Status grouped by Parent issue: `label:area/<name> is:open`
7. Vakt, board by Status: `label:kind/question,kind/incident is:open`

Two things to confirm on the real project rather than assume: that `type:` is offered in the filter box, and that group-by offers Parent issue. If not, the Developers view excludes by label only, and the Area view sorts by Parent issue instead. The views are drawn with example issues in [Board views](board-views.md).

## Built-in workflows

In the project's Workflows settings.

- Auto-add: on, `is:issue is:open` from Altinn/altinn-auth. Item added → Backlog.
- Item closed → Done. Item reopened → In progress.
- Auto-archive: off. The agile coach archives Done after the result meeting.
- Pull request merged: off; pull requests are not on the board.

## Acceptance

With test issues that are deleted afterwards:

1. Create a feature; set a milestone. It shows in the Features view.
2. From it, create sub-issues with the forms: a design task, two backend tasks, a frontend task, a test task. Each shows in its role's view; the feature reads 0 of 5.
3. Set the Iteration on the tasks; move the design task and the backend tasks to Ready. They show on the Iteration board.
4. Close the design task; move the frontend task to Ready.
5. Close the backend tasks and the frontend task. The feature reads 4 of 5; drag the test task to Executing.
6. Close the test task, then the feature. Every card is in Done.
7. Reopen the feature. Its Status is In progress.

## Later

A GitHub Action that makes the handoffs automatic when an issue closes: move what waited for it to Ready, move the test task to Executing, add the design lead, and comment to the area owner when a feature gets a milestone. Not part of the first set-up; it matches on the labels above, so it comes after them. It needs a token with project write access, which `ASSIGN_PROJECT_TOKEN` already is, and can reuse the field lookup in `.github/scripts/add-to-project.mts`.
