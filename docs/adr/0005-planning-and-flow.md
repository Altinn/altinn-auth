# ADR-0005: How work is planned and flows on the shared board

- **Status:** Proposed
- **Date:** 2026-10-27
- **Deciders:** `@altinn/team-access-management` and `@altinn/team-access-info`, through six working groups after the workshop on 2026-10-19, a joint decision meeting, and review of [PR #4577](https://github.com/Altinn/altinn-auth/pull/4577)
- **Supplements:** [ADR-0004](0004-issue-and-label-model.md). **Amends** ADR-0004 in two places, listed under Decision 5.

Drafted with AI assistance (Claude) on 2026-10-27 from the working groups' pages and revised through review in the PR. Human reader of the final wording: not confirmed. This record holds the decisions. How the process is run day to day is the handbook [How we work](../process/how-we-work.md), and each role's board view is drawn in [Board views](../process/board-views.md).

**TL;DR**

- One board for both teams, five Status columns: Backlog, Ready, In progress, In review, Done. Every role has its own issue; you get it when the previous one closes, and you close it when your part is done. Closing is what moves cards; the handoff between roles is a move or a mention by hand for now.
- Work is features under epics. Refinement, in the sprint before build, gives a feature enough to start: goal, main acceptance criteria, named people, a first task. The developer splits the rest during the sprint. A task is done when merged. A feature is done when its test task is approved and the change is in production.
- Two-week sprints from 2026-11-09. Each role owns one kind of decision and has the last word on it. Area owners with deputies own their area end to end.
- Two amendments to ADR-0004: `kind/design` is a new `kind/*` value, and the horizon field is not on the board.
- Tried unchanged for three sprints and reviewed at the retro on 2026-12-18. The release process is decided later.

## Context

ADR-0004 gave every issue a type, a component and a capability, moved releases to milestones, and set up one board for both teams. A first proposal for how work flows on that board was discussed at a workshop on 19 October. The feedback was that it started from a solution before the problems were named, that horizon, priority and milestone together were too much to take in, that roles were undefined so everyone owned everything and nobody acted, and that not all work can be split small.

Six working groups then wrote their part of the process from the problems up. The problems they named: nobody has the overview across the two teams, nobody owns an area, two sets of meetings, nobody plans who tests before a change reaches AT, and closed has never meant verified. This ADR records what they decided. The handbook describes how it is run.

## Decision

### 1. Working method

We work in two-week sprints, shared by both teams, on one board. Work is organised as features under epics, and every feature is split into tasks as sub-issues: design, backend, frontend, test. Refinement produces enough to start; the developer splits the rest during the sprint. The sprint, the meetings and the roles are described in the handbook, sections 1 to 3.

One rule drives the flow: **every role has its own issue on the board. You get it when the previous issue closes, and you close it when your part is done.** A merged pull request closes the task and the board moves it to Done. Closing a dependency makes the next issue eligible for its handoff; in the first iteration the handoff itself is a move or a mention by the person who finished. An automation for it is a later iteration (Consequences).

The process has no name and follows no named method. Its parts are called what they do. Where a practice from Scrum or Kanban fits, we use it; where it does not, we leave it out.

### 2. Agreements

**Definition of Ready** means enough to start, not fully specified. A feature moves to Ready when all of these hold: it has a priority and a milestone, and belongs to an epic where one exists; it states the goal and the main acceptance criteria, written so that someone other than the author can test against them; it has a named developer and a named tester, and a designer where users will see the change; and its first task exists as a sub-issue. The developer splits the rest into tasks during the sprint, pulling in the people they need, and the tester adds test tasks as the split shows what can be tested alone. Until a feature has that much it stays in Backlog, whatever its priority. Work that cannot be shipped in pieces is split by steps of work instead, and may span sprints as long as one task closes per sprint; what cannot even do that stays in In progress with one line of status before each result meeting.

**Definition of Done**, at two levels. A task is done when its pull request is merged and the automated tests are green; the unit and integration tests for the change are part of the task and are written before it goes to In review. A feature is done when all its tasks are merged, design review is approved for changes users will see, the test task is closed with an approved result, and the change is in production. A standalone task or bug, one with no feature, that needs manual verification gets its own test task when the pull request is opened, the same way a feature does. A test task is normally created in refinement but can be added at any point when development shows a change needs manual testing.

**Decisions.** One role has the last word: product owner on priority, tech lead on technology, design lead on design, area owner on their area. A discussion in a meeting is timeboxed; not agreed, it leaves the meeting and the owner books a short one with the people it needs. The facilitator reads decisions back at the end of every meeting. Sprint goals are written in the iteration and in Slack, a decision about an issue on the issue, a decision for the whole team in `docs/decisions.md`, one row per decision. The repository is public, so the log records that a decision was made and where the details live, never details of a vulnerability or an incident with customer data; the [data policy](../ai/data-policy.md) applies.

### 3. How work ends

| What | Closed by | When |
| --- | --- | --- |
| Developer task | The merge | The pull request merges |
| Design task | The designer | The design is good enough to start building |
| Test task | The tester | The result is written and approved |
| Feature | The tester | Checked in production after the weekly deploy |
| Epic | The product owner, in the result meeting | The last feature is closed, which says the roadmap goal is met |

The result meeting gives every unfinished issue a decision: continue, split or drop. The agile coach archives Done after the result meeting.

### 4. The board

- **One board**, Team Autorisasjon, for both teams. No boards per role; one saved view per role instead. Bugs show on the Developers view.
- **Five Status columns.** Backlog: not in a sprint, whether new, triaged or refined. Ready: refined and in the sprint, the top card is the one to take. In progress: being worked, few per person, no hard limit. In review: pull request open; kept short, since reviewing others' pull requests comes before taking a new task. Done: closed.
- **Hierarchy.** Epic, feature, task, from ADR-0004's issue types. A task is a sub-issue of its feature where it has one; a bug or a small task may stand alone. Test tasks carry `kind/testplan`, design tasks `kind/design`. A task that waits for another is marked "blocked by" it, as ADR-0004 requires when the blocker is known.
- **Fields.** Iteration is the sprint, set at planning; a design task made in refinement for the next sprint gets the current iteration, so design runs a sprint ahead. Milestone is a dated roadmap target, set by the product owner; features carry it, epics need none. Deadlines from outside live on the milestone and are the product owner's to hold; developers work to the sprint. Priority orders work within the current and next sprint; `urgent` requires a date in the issue. Test phase, Preparing, Executing, Retest, is set by the tester on test tasks and gives the test board its columns. There is no horizon field: the current iteration is now, a milestone without an iteration is next, no milestone is later.
- **Views.** Seven, each a filter and a column field: Sprint (default), Developers, Test board by Test phase, Design (table), Features (table), one per area, Vakt. Role views start at Ready; Backlog is seen in Features, Area and Vakt. Drawn in [Board views](../process/board-views.md).
- **Automation.** Built in only: auto-add from the repository, item closed to Done, item reopened to In progress. Archiving is by hand after the result meeting. The handoff Action is a later iteration.

### 5. Amendments to ADR-0004

- `kind/design` is a new value in the `kind/*` group, for design tasks under a feature.
- The horizon field that ADR-0004 decision 5 put on the board is not used. Now, next and later are read from iteration and milestone as in Decision 4. If the product owners want a Now, Next, Later roadmap view, it is a field on epics only.

ADR-0004 carries an "Amended by ADR-0005" note on its status line; it is otherwise unchanged.

### Alternatives considered

- **A Test column after In review**, proposed in review on 2026-10-14. Rejected by the test and developer groups: verification is a test task that flows through the same columns, so a task closes on merge as GitHub expects and the board needs no extra state.
- **Closed and Done as two states of one issue**, the board status saying whether a merged issue is verified. Rejected: it needs custom automation and two readings of one card; two levels of issue, task and feature, say the same with GitHub's defaults.
- **Full breakdown before planning**: every task created and every acceptance criterion written in refinement. Rejected in review: refinement becomes the bottleneck and a feature that misses it waits a sprint. Ready means enough to start; the developer splits the rest.
- **A Refinement column and a must-test label**, proposed on 2026-10-21. Refinement is a Backlog state, not a column, since Ready already means refined; the must-test need is met by a test task under the issue, the same mechanism as for features.
- **Design review before testing starts rather than in parallel**, proposed by the test group on 2026-10-23. This record runs review and functional test in parallel from the feature's test task once it is in AT, with the task closing when both approve, since sequencing them adds a wait for every user-facing feature. To be confirmed with the test group at the decision meeting.
- **Keep the horizon field**, as ADR-0004 decided. Rejected after the workshop feedback that three scheduling fields were too many; the board already shows the three states.
- **A hard limit on issues in progress.** Rejected by the developers: a number makes people cut issues to fit it; the sprint's iteration is the limit.
- **Automating the handoffs from the start.** Deferred: the handoffs are by hand for the first sprints, so the chain is understood before it is automated.

## Consequences

- **Preconditions before the first sprint**, each needing to be in [#4088](https://github.com/Altinn/altinn-auth/issues/4088), which today covers only Status, Iteration, Team, Priority and the built-in workflows: the labels brought in line with ADR-0004 and `kind/design` created, the repository has `Testplan` and no `kind/testplan`, `kind/incident`, `kind/security` or `status/blocked` today; the test plan form changed to set `kind/testplan` and a design form added; the Test phase field; the seven views; the Priority values as named here. Until the labels exist, the views match nothing.
- The meetings in the handbook replace each team's grooming, team meeting and Friday meeting from 2026-11-09.
- The `ukesrapport` skill ([#4615](https://github.com/Altinn/altinn-auth/pull/4615)) should distinguish closed tasks, merged work, from closed features, delivered work.
- A decision log is started at `docs/decisions.md`.
- Tried unchanged for three sprints and reviewed at the retro that closes sprint 3, on 2026-12-18, and again in January when the teams become one. A change to a decision here is a new ADR that supersedes this one; a change to how the process is run is a change to the handbook.
- Decided later: the release process, by the release group, and the handoff Action, specified in #4088 once the labels and forms exist.

## References

- [ADR-0004: One issue and label model for the monorepo](0004-issue-and-label-model.md). Board set-up issue: [#4088](https://github.com/Altinn/altinn-auth/issues/4088).
- Handbook: [How we work](../process/how-we-work.md). Views: [Board views](../process/board-views.md).
- Shared board: [Team Autorisasjon](https://github.com/orgs/Altinn/projects/196).
- Working-group pages: comments on [PR #4577](https://github.com/Altinn/altinn-auth/pull/4577).
- Agent skills for the weekly report, board status and closure candidates: [#4615](https://github.com/Altinn/altinn-auth/pull/4615).
- Practice elsewhere: [VS Code issue tracking](https://github.com/Microsoft/vscode/wiki/Issue-Tracking), [Kubernetes issue triage](https://www.k8s.dev/docs/guide/issue-triage/), [GitHub Docs, best practices for projects](https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/best-practices-for-projects).
