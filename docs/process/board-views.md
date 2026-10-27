# Board views

*Companion to [How we work](how-we-work.md) and [ADR-0005](../adr/0005-planning-and-flow.md), Decision 4. Drafted with AI assistance (Claude) on 2026-10-27. Human reader: @acn-dgopa. The issues are invented examples, named with letters so that nothing here links to a real issue; the filters are the real ones.*

One board, one saved view per role. Each view is a filter and a column field, nothing more. Below, each view as it looks on a normal day in an iteration. F is a feature, E an epic, D a design task, B a backend task, FE a frontend task, T a test task, X a bug, Q a vakt case.

## Iteration board, the default

Everyone. Board, columns are Status, sorted by Priority. Filter: `iteration:@current`

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| B5 Add via-party mode to Authorize API · Task · `area/authorization` | B2 ConnectionQuery: prune on resource filter · Task · PR open | B6 Query param for party switch · Task · PR open | B1 Backend: approval limited to RightKeys · merged |
| T4 Test: Delegation overview end to end · `kind/testplan` | B7 Change feed sync in Access Management · `status/blocked` | X2 Enduser-api: delegated clients 500 · Bug · PR open | T1 Test: Approval limited to RightKeys · approved |
| D2 Design: Consent dialog, rights selection · `kind/design` | T2 Test: Rights selection on approval · `kind/testplan` · Preparing | | B8 Drop old AM database schemas · merged |
| | D1 Design: System user delegation flow · `kind/design` | | D4 Design: Activity log pagination |
| | X1 RDF export omits contact points · Bug | | |

- No Backlog column. Everything here is in the iteration, so it is at least Ready.
- The only view where a design task, a developer task and a test task sit side by side.
- Done fills during the iteration; the agile coach archives it after the result meeting.

## Developers

Developers. Board, columns are Status, sorted by Priority. Filter: `type:Task,Bug,Enhancement -label:kind/testplan -label:kind/design iteration:@current`

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| B5 Add via-party mode to Authorize API · high · ↑ F5 | B2 ConnectionQuery: prune on resource filter · PR open | B6 Query param for party switch · PR open · `Resolves:` | B1 Backend: approval limited to RightKeys · merged 09:40 · tester mentioned in T1 |
| FE1 Frontend: select rights on request · normal · ↑ F1 · waited for D2, released today | B7 Change feed sync in Access Management · `status/blocked` by Team Core | X2 Enduser-api: delegated clients 500 · Bug · PR open | |
| | X1 RDF export omits contact points · Bug · ↑ F3 | | |

- Only tasks and bugs. No test or design tasks, no features, no Backlog.
- The parent feature (↑) is on every card, so a task is never picked without knowing which feature it serves.
- In review means take the next one. The card leaves on merge, not on approval.

## Test board

Testers. Board, columns are the **Test phase** field, not Status. Filter: `label:kind/testplan is:open`

| No phase | Preparing | Executing | Retest |
| --- | --- | --- | --- |
| T4 Test: Delegation overview end to end · ↑ F4 · next iteration | T2 Test: Rights selection on approval · ↑ F1 · feature 2 of 5 | T1 Test: Approval limited to RightKeys · ↑ F1 · developer mentioned tester 09:40 · design lead added | |
| | T5 Test: Via-party mode, alone · ↑ B5 | T3 Test: System user delegation flow · ↑ F2 · bug X3 filed | |

- Columns are the Test phase field. Status still says In progress for most of these on the iteration board; here the phase is what matters.
- The developer's mention is the signal: when the last task under a feature merges, the developer mentions the tester in the test task, and the tester drags the card from Preparing to Executing. An Action may do this later.
- Done is not a column. A closed test task leaves the view.
- Parent issue is a link, not a progress bar. How close a feature is to execution is read from the Features view.

## Design

Designers. Board, columns are Status, sorted by Iteration. Filter: `label:kind/design -status:Backlog`

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| D2 Design: Consent dialog, rights selection · ↑ F4 · Iteration 1 | D1 Design: System user delegation flow · ↑ F2 · Iteration 1 | | D4 Design: Activity log pagination · ↑ F6 · good enough to build |
| D3 Design: Activity log, empty state iteration · ↑ F6 · Iteration 2 | | | |

- The same shape as the developers' board: only design tasks, the parent feature (↑) on every card, no Backlog.
- Working one iteration ahead: D2 belongs to a feature the developers build next iteration; the design task is in this iteration.
- What waits on a design task is not on the card; Projects has no dependency field. The designer sees it in the task's own "blocking" list, or in the Area view where the feature's tasks sit together.
- D3 is design debt: a small follow-up iteration created after design review. Done is closed at "good enough to build"; the frontend tasks that waited are then moved to Ready by the designer.

## Features

Product owners, design lead, tech leads, area owners. Table, sorted by Milestone then Priority. Filter: `type:Epic,Feature is:open`

| Title | Type | Milestone | Priority | Status | Sub-issues | Iteration |
| --- | --- | --- | --- | --- | --- | --- |
| E1 Remove Altinn 2 roles | Epic | 2027-01 | high | In progress | 2 of 5 features | — |
| F1 Approval limited to RightKeys | Feature | 2026-12 | high | In progress | 4 of 5 · test task open | Iteration 1 |
| F4 Delegation overview | Feature | 2026-12 | normal | Ready | 0 of 6 | Iteration 2 |
| F2 System user delegation flow | Feature | 2026-12 | normal | In progress | 1 of 4 | Iteration 1 |
| F7 Bulk revoke | Feature | — | — | Backlog | — | — |
| E2 Move to DIS | Epic | 2027-03 | normal | Backlog | 0 of 3 features | — |

- No tasks here. Progress is read from sub-issue counts, never from individual tasks. The count includes the design and test tasks, so "4 of 5" with one open usually means waiting for test.
- Backlog is visible: features without a milestone, and prioritised features not yet refined. Setting a milestone is the signal to the area owner that refinement is next.
- Epic progress counts features, and the product owner closes the epic when the last one is Done.

## Area, one per area

The area owner. Board, columns are Status, grouped by Parent issue. Filter: `label:area/<name>`

**F1 Approval limited to RightKeys · 2026-12 · sub-issues 4 of 5 closed, test task open**

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| T1 Test: Approval limited to RightKeys · `kind/testplan` | | | B1 Backend: approval limited to RightKeys |
| | | | FE2 Frontend: select rights on approval |

**F4 Delegation overview · 2026-12 · refined, next iteration**

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| D2 Design: Consent dialog, rights selection · `kind/design` | | | |
| B9 Backend: overview query | | | |

**F7 Bulk revoke · no milestone · Backlog, not refined**

- The chain is visible per feature. F1 is waiting only for the tester; F4 has just been refined and its first tasks moved to Ready by hand.
- Backlog is visible here, as the unrefined feature at the bottom. This is where the area owner sees what to refine next.
- If the group-by menu does not offer Parent issue, sort by it instead; a feature's tasks still sit together.

## Vakt

The person on vakt this week. Board, columns are Status. Filter: `label:kind/question,kind/incident is:open`

| Backlog | In progress | Done |
| --- | --- | --- |
| Q1 Vendor asks: why is delegation rejected for org X · `kind/question` · from Slack | Q2 CRM case: client delegation not visible · `kind/question` · task B10 created | |
| | Q3 Authorize API 5xx spike, AT · `kind/incident` · in the iteration, bypassed Ready | |

- Backlog is a column here, because received-but-not-started is a real state for a vakt case.
- The incident is also on the iteration board, since it got the current iteration. The question cases are not.

## Bugs

Everyone. Board, columns are Status, sorted by Priority. Filter: `type:Bug is:open`

| Backlog | Ready | In progress | In review | Done |
| --- | --- | --- | --- | --- |
| X4 Delegation list shows wrong org name · `area/access-management-frontend` · no iteration | X3 Consent dialog loses selection on back · ↑ F2 · found in test | X1 RDF export omits contact points · ↑ F3 | X2 Enduser-api: delegated clients 500 · PR open | X5 Activity log pagination off by one · merged |

- Every open bug on one board, whatever feature it belongs to, so nothing found in test or reported from outside is lost between the role views.
- Bugs under a feature also show on the Developers board; this view is for the overview, not for pulling work.
- Backlog shows here, since a bug is often reported before anyone has decided when to fix it.

## Where Backlog shows

Backlog belongs in four views: Features, Area, Vakt and Bugs. Those are the views where someone decides what to do with an issue that is not yet started. The role boards for developers, designers and testers never show it.
