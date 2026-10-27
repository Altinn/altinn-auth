# Board views for the working process

*Drafted with AI assistance (Claude) on 2026-10-28. Human reader: not confirmed. Companion to [ADR-0005](0005-planning-and-flow.md), section 9. The issues are invented examples that show the shape of each view; the filters are the real ones.*

One board, one saved view per role. Each view is a filter and a column field, nothing more. Below, each view as it looks on a normal day in a sprint.

## Sprint board, the default

Everyone. Board, columns are Status, sorted by Priority. Filter: `iteration:@current`

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| #4292 Add via-party mode to Authorize API · Task · `area/authorization` | #3899 ConnectionQuery: prune on resource filter · Task · PR #3925 | #3296 Query param for party switch, system user · Task · PR #2283 | #4605 Backend: approval limited to RightKeys · merged |
| #4712 Test: Delegation overview end to end · `kind/testplan` | #3745 Change feed sync in Access Management · `status/blocked` | #4511 Enduser-api: delegated clients 500 · Bug · PR #2131 | #4606 Test: Approval limited to RightKeys · approved |
| #4701 Design: Consent dialog, rights selection · `kind/design` | #4698 Test: Rights selection on approval · `kind/testplan` | | #3655 Drop old AM database schemas · merged |
| | #4688 Design: System user delegation flow · `kind/design` | | #4650 Design: Activity log pagination |
| | #4269 RDF export omits contact points · Bug | | |

- No Backlog column. Everything here is in the sprint, so it is at least Ready.
- The only view where a design task, a developer task and a test task sit side by side.
- Done fills during the sprint and is archived after the result meeting.

## Developers

Developers. Board, columns are Status, sorted by Priority. Filter: `type:Task,Bug -label:kind/testplan -label:kind/design iteration:@current`

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| #4292 Add via-party mode to Authorize API · high · ↑ #4290 Feature | #3899 ConnectionQuery: prune on resource filter · PR #3925 open | #3296 Query param for party switch · PR #2283 · `Resolves: #3296` | #4605 Backend: approval limited to RightKeys · merged 09:40 · tester mentioned in #4606 |
| #4610 Frontend: select rights on request · normal · ↑ #4603 Feature · was blocked by #4701, released today | #3745 Change feed sync in Access Management · `status/blocked` by Team Core | #4511 Enduser-api: delegated clients 500 · Bug · PR #2131 | |
| | #4269 RDF export omits contact points · Bug · ↑ #4100 Feature | | |

- Only tasks and bugs. No test or design tasks, no features, no Backlog.
- The parent feature (↑) is on every card, so a task is never picked without knowing which feature it serves.
- In review means take the next one. The card leaves on merge, not on approval.

## Test board

Testers. Board, columns are the **Test phase** field, not Status. Filter: `label:kind/testplan is:open`

| No phase | Preparing | Executing | Retest |
| --- | --- | --- | --- |
| #4712 Test: Delegation overview end to end · ↑ #4700 Feature · next sprint | #4698 Test: Rights selection on approval · ↑ #4603 Feature · feature 2 of 5 | #4606 Test: Approval limited to RightKeys · ↑ #4603 Feature · developer mentioned tester 09:40 · design lead added | |
| | #4713 Test: Via-party mode, alone · ↑ #4292 Task | #4690 Test: System user delegation flow · ↑ #4688 Feature · bug #4740 filed | |

- Columns are the Test phase field. Status still says In progress for most of these on the sprint board; here the phase is what matters.
- The developer's mention is the signal: when the last task under a feature merges, the developer mentions the tester in the test task, and the tester drags the card from Preparing to Executing. An Action may do this later.
- Done is not a column. A closed test task leaves the view.
- Parent issue is a link, not a progress bar. How close a feature is to execution is read from the Features view.

## Design

Designers. Table, sorted by Iteration then Status. Filter: `label:kind/design is:open`

| Title | Parent issue | Status | Iteration | Milestone | Blocks | Assignees |
| --- | --- | --- | --- | --- | --- | --- |
| #4688 Design: System user delegation flow | #4688 Feature | In progress | 2026-11-09 | 2026-12 | #4690, #4691 | designer |
| #4701 Design: Consent dialog, rights selection | #4700 Feature | Ready | 2026-11-09 | 2026-12 | #4703 | designer |
| #4650 Design: Activity log, empty state iteration | #4650 Feature | Ready | 2026-11-23 | 2027-01 | — | designer |

- Working one sprint ahead: row two belongs to a feature the developers build next sprint; the design task is in this sprint.
- Blocks shows what waits. When row one closes, the designer moves #4690 and #4691 to Ready for the frontend developers.
- Row three is design debt: a small follow-up iteration, created after design review.

## Features

Product owners, design lead, tech leads, area owners. Table, sorted by Milestone then Priority. Filter: `type:Epic,Feature is:open`

| Title | Type | Milestone | Priority | Status | Sub-issues | Iteration |
| --- | --- | --- | --- | --- | --- | --- |
| Remove Altinn 2 roles | Epic | 2027-01 | high | In progress | 2 of 5 features | — |
| Approval limited to RightKeys | Feature | 2026-12 | high | In progress | 4 of 5 · test task open | 2026-11-09 |
| Delegation overview | Feature | 2026-12 | normal | Ready | 0 of 6 | 2026-11-23 |
| System user delegation flow | Feature | 2026-12 | normal | In progress | 1 of 4 | 2026-11-09 |
| Bulk revoke | Feature | — | — | Backlog | — | — |
| Move to DIS | Epic | 2027-03 | normal | Backlog | 0 of 3 features | — |

- No tasks here. Progress is read from sub-issue counts, never from individual tasks. The count includes the design and test tasks, so "4 of 5" with one open usually means waiting for test.
- Backlog is visible, as features without milestone. Setting a milestone is the signal to the area owner that refinement is next.
- Epic progress counts features, and the product owner closes the epic when the last one is Done.

## Area, one per area

The area owner. Board, columns are Status, grouped by Parent issue. Filter: `label:area/<name> is:open`

**#4603 Feature · Approval limited to RightKeys · 2026-12 · sub-issues 4 of 5 closed, test task open**

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| #4606 Test: Approval limited to RightKeys · `kind/testplan` | | | #4605 Backend: approval limited to RightKeys |
| | | | #4611 Frontend: select rights on approval |

**#4700 Feature · Delegation overview · 2026-12 · refined, next sprint**

| Ready | In progress | In review | Done |
| --- | --- | --- | --- |
| #4701 Design: Consent dialog, rights selection · `kind/design` | | | |
| #4702 Backend: overview query | | | |

**#4720 Feature · Bulk revoke · no milestone · Backlog, not refined**

- The chain is visible per feature. The first is waiting only for the tester; the second has just been refined and its first tasks moved to Ready by hand.
- Backlog is visible here, as the unrefined feature at the bottom. This is where the area owner sees what to refine next.
- If the group-by menu does not offer Parent issue, sort by it instead; a feature's tasks still sit together.

## Vakt

The person on vakt this week. Board, columns are Status, no iteration. Filter: `label:kind/question,kind/incident is:open`

| Backlog | In progress | Done |
| --- | --- | --- |
| #4731 Vendor asks: why is delegation rejected for org X · `kind/question` · from Slack | #4728 CRM 48812: client delegation not visible · `kind/question` · task #4729 created | |
| | #4733 Authorize API 5xx spike, AT · `kind/incident` · in sprint, bypassed Ready | |

- Backlog is a column here, because received-but-not-started is a real state for a vakt case.
- The incident is also on the sprint board, since it got the current iteration. The question cases are not.

## Where Backlog shows

Backlog belongs in three views only: Features, Area and Vakt. Those are the three roles that do something with an unstarted issue. Everyone else never needs to see it.
