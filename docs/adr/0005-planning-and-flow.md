# ADR-0005: How work is planned and flows on the shared board

- **Status:** Proposed
- **Date:** 2026-10-13
- **Deciders:** `@altinn/team-access-management` and `@altinn/team-access-info`, through review of the PR that adds this ADR
- **Supplements:** [ADR-0004](0004-issue-and-label-model.md)

Drafted with AI assistance (Claude) on 2026-10-13 as a proposal for the two teams to discuss before the first joint planning. The open questions at the end are for the reviewers to settle; the decision section records the proposal as it stands.

**TL;DR**

- The shared project board is the single source of truth for what is being worked on and where it stands. Work that is not on the board does not exist. Moving your own card is part of finishing the work.
- ADR-0004 put priority, horizon and progress on the board. This ADR defines what they mean and how we use them: horizon answers *when*, priority answers *in which order within that*, milestone answers *which target*. `urgent` requires a date.
- Six columns, each a handoff to a different person: Backlog → Ready → In progress → In review → Test → Done. Closed and Done are two states: an issue closes when its pull request merges, and the board status says whether it is verified. Production follows the weekly release routine, not the board and not the milestone.
- Two-week cycles shared by both teams from now: planning opens the cycle, review closes it, and weekly refinement fills Ready from the top of the prioritised view. Every discussion ends with one written line in the issue.
- The Team field becomes Fokusområde on 1 January 2027, by a CODEOWNERS change as ADR-0004 foresees. Fokusområde is a set of `area/*` components, not a layer.

## Context

ADR-0004 gave every issue a type, a component and a capability, moved releases to milestones, and put priority, horizon and progress on the shared project board as fields. It stopped there on purpose: it is a model for *classifying* work, not for *planning* it.

The next questions follow from it, and this ADR proposes answers so the two teams can decide them together:

- What each board field means, who sets it, and when.
- What each column means, and when a card moves.
- How `status/triage`, `status/draft` and `status/blocked` relate to the board's status.
- Which issues need a milestone, and what a milestone is called.
- How we plan a period, how we read what was delivered, and how a discussion is closed.
- How quarterly goals, roadmap items and Epics link, so "why does this matter" can be read from the board.

## Decision

We will use one flow, with one mechanism per question, built on ADR-0004.

1. **The board is the truth.** All planned work is an issue on the shared project board. Work handled within the weekly vakt rotation is the one exception (6); it becomes an issue when it needs more than the vakt can do. A developer moves their own card as the work moves; the verifier moves it to Done. Progress is read from the board status and from the Friday review post, never from whether the issue is open or closed.

2. **Status (columns)** answers *where the work is*. Each column is a state of the work; moving between them is a handoff to a different person:

    | Column      | Meaning                                                                                  | Next person              |
    | ----------- | ---------------------------------------------------------------------------------------- | ------------------------ |
    | Backlog     | Known, not planned. May lack milestone, priority and acceptance criteria.                | Product owner, at triage |
    | Ready       | Definition of Ready met (7). Anyone can pull it.                                         | A developer              |
    | In progress | Being built. Column limit: roughly the number of developers; personal rule: at most two. | The developer            |
    | In review   | Pull request open. Reviewed by someone other than the author.                            | A reviewer               |
    | Test        | Merged and deployed to AT. Verified by someone other than the author.                    | A tester or peer         |
    | Done        | Definition of Done met (8).                                                              | Nobody                   |

    Production is not a column. Merging to main deploys to AT automatically; TT02 is deployed on Wednesdays and production on Tuesdays, and whatever is merged by then goes with that routine. A milestone does not gate deploys (6).

3. **`status/*` labels and board status.** `status/triage` and `status/draft` only exist on issues in Backlog; moving to Ready removes them. `status/blocked` is a label on a card that keeps its column, so blocked work stays visible where it stopped, with a "blocked by" link when the blocker is an issue.

4. **Horizon** answers *when*, before a release is decided. It is the product owner's quick sort at triage; the milestone is the commitment, made at planning. `now` = belongs to the current milestone, which must then be set; `next` = candidate for the next milestone, decided at the next planning; `later` = no milestone, not refined. Horizon has to earn its place: if after four cycles it is mostly empty or always says what the milestone already says, it is dropped and "no milestone" means later (open question 7).

5. **Priority** answers *in which order within the horizon*, and is set by the product owner and adjusted at refinement: `urgent` = has a hard external date, stated in the issue; `high` = must be in the milestone; `normal` = should be; `low` = if there is time. An `urgent` issue without a date is `high`.

6. **Milestone** answers *which target*. A milestone is a planning bucket: the issues committed to a named target with a due date, named by year and month (`2026-12`). It is read at planning and review, not by the deploy routine: a milestone is closed when its issues are closed, and production follows the weekly release (2) regardless of whether the milestone is complete. Every issue in Ready or later has a milestone. Backlog issues may lack a milestone. Work handled by the vakt rotation is not on this board; see open question 6 for when it becomes an issue. 
    - **Incidents.** An incident is an issue from the moment it is confirmed: kind/incident, priority urgent (the date is now), placed directly in In progress by the person on vakt, bypassing Ready. It gets the current milestone, or a patch milestone if it ships on its own. Column limits do not apply to it. When it is resolved, the fix follows the normal path from In review onward, and any follow-up work (root cause, hardening, missing monitoring) becomes ordinary issues in Backlog. It is mentioned in the Friday review even if it is closed, so the cycle’s record shows what displaced planned work.

7. **Definition of Ready.** Milestone, at least one `area/*`, horizon, priority, an issue type, and acceptance criteria that a developer other than the author could test against. Small enough to finish within one cycle; if not, it is an Epic with sub-issues.

8. **Definition of Done.** Reviewed by someone other than the author; merged; deployed to AT; verified in AT by someone other than the author; public API changes reflected in the OpenAPI specification; the card is in Done. Documentation for a change that users or vendors will notice is part of the same issue, not a follow-up. Closed and Done are two states. An issue is closed when its pull request merges; the PR uses Closes #123 as today. Closed means merged, not verified. The board’s “Item closed” workflow sets Status to Test (merge deploys to AT, so the change is ready to verify there). The verifier moves the card to Done. If verification fails, the issue is reopened; “Item reopened” moves it back to In progress and notifies the owner. Board views have no is:open filter, and auto-archive is on status:Done, not is:closed.

9. **Cycle and meetings.** Two-week cycles, shared by both teams from the next cycle, so that the way of working is already common when the teams become one. Three fixed meetings, each with one job:

    - **Planning, first Monday of the cycle, 30 minutes, both teams.** The product owner states which goals the cycle serves. The team pulls from the top of Ready. The result is the cycle's iteration on the board. Nothing else is discussed.
    - **Refinement, once a week, 45 minutes at most.** Each team keeps its existing slot until January, but the shape is the same: triage of `status/triage` issues first (type, area, horizon, priority), then fill Ready from the top of the prioritised view by checking the Definition of Ready, then stop. It is not a forum for open-ended discussion.
    - **Review, last Friday of the cycle, 30 minutes, both teams.** What reached Done against the milestone, what did not and why, one thing to do differently. One person posts the answers in the team channel; that post is the status report. The delivery part of it comes from the ukesrapport skill (#4615): closed issues, merged pull requests, releases and Epic progress for the week, grouped per area/*. The team adds what did not reach Done, why, and one thing to do differently. Until the skill reads board status, it reports what is merged; what is verified is read from the Done column.

    Planning and review are new; refinement replaces today's grooming in each team rather than adding to it. No other recurring meeting is added without naming which existing one shrinks or ends. Days are a proposal; what matters is that planning opens the cycle and review closes it.

10. **Discussions end in writing.** A discussion starts with the question to settle and a time limit. It ends with one line in the issue: *decided* (what), *parked* (until when), or *to clarify* (by whom). When the time limit runs out, the group decides whether to continue, park, or take it offline with two named people. Architectural or cross-component decisions become an ADR.

11. **Goals.** Quarterly goals live in digdir/roadmap, as the roadmap issues the product owners already maintain: three to five per quarter, each with a single owner. Every Epic in this repository links to the roadmap issue it serves, and the roadmap issue links back to its Epics. Every planned issue here is a sub-issue of such an Epic, so “why does this matter” can be followed from any issue to its goal in two clicks. The roadmap issue holds the why and the expected result; the Epic holds the what.
                                    
12. **Default view.** The team’s default view is Priority board, filtered on the current milestone. That view is the prioritised list the team pulls from; the Backlog column on the board shows what is not started, not what comes next.

### Alternatives considered

- **Keep Done as "in production".** Honest about outcomes, but cards wait for the weekly release, column limits lose meaning, and the Friday review cannot separate finished from shipped. Rejected: the deploy routine answers "shipped"; the board answers "finished".
- **No Test column; In review covers verification.** Fewer columns and closer to continuous-delivery practice. But verification in AT today is manual, done by a different person, and the people who verify need a queue to pull from. Rejected for now; the Test column is removed when end-to-end tests can run before merge (see open question 4).
- **Priority only, no horizon.** Simpler. But without a time bucket everything that matters tends to become `urgent`, and the order inside "now" is lost. Rejected: both fields, with horizon set first.
- **A separate team contract outside the repository.** Easier to write, but it is not reviewed, versioned or found like code. Rejected: this ADR is the team contract for planning and flow, and is revised like any other ADR.

## Consequences

- Column limits become real: starting new work means finishing or returning something first. The first Monday planning sets the In progress column to its limit.
- `urgent` requires a date in the issue; otherwise it is `high`.
- Refinement has a fixed shape: triage first, then Definition of Ready, then stop. Open-ended discussion gets a timeboxed slot with a written outcome, or a separate meeting with two named people.
- The Estimate field is hidden until someone asks for it. The Test column is added. The default view is the priority board filtered on the current milestone.
- Issues close on merge. The board’s “Item closed” workflow sets Status to Test, “Item reopened” sets In progress, and the verifier moves the card to Done. Views drop the is:open filter; auto-archive is on status:Done.
- With closed meaning merged, open issues that are actually finished become easier to miss. The lukkekandidater skill (#4615) reports them without closing anything; running it before refinement keeps the Backlog honest.
- This ADR is reviewed after four cycles (mid-December) and again after the team becomes one (January). A change to column meanings, field semantics or cadence is a change to this ADR; a change to the fokusområde mapping is a change to CODEOWNERS.

## Common cases — draft for the workshop

*Proposed handling, to be settled together. Each line is one sentence on purpose; what survives the workshop moves into the Decision section.*

| Case | Proposed handling |
| --- | --- |
| **Small fix** (own area, under half a day, no behaviour change users or vendors would notice) | Create the issue, set type and area, current milestone, priority `normal`, straight to In progress. Flows through Review and Test as usual. If any condition fails, it goes to Backlog and triage. |
| **Blocked by another team** (Team Core, Team Dialogporten, a Service Owner, Jurist, a vendor) | The card keeps its column and gets `status/blocked` with a "blocked by" link. Counted in the Friday review. Not moved back to Ready. |
| **Verification fails in Test** | Back to In progress, not Review. Same issue, no new one; the tester writes what failed in the issue. |
| **Card sits in In progress across two cycles** | The Friday review decides: finish, split, or return to Ready. The decision is written in the issue. |
| **Security or dependency alert** | High or critical: taken by whoever can, with the area owner as fallback if nobody has picked it within a day; small-fix lane, in before the next train. Lower: Backlog and triage. |
| **Cross-area issue** | One assignee from the primary area, a reviewer from the other. Two `area/*` labels, one owner. |
| **Urgent request from outside** (management, a vendor, another team) | Enters Backlog with `urgent` *and* a date; the product owner decides what leaves the cycle to make room. Nothing is added without something coming out. |
| **Spike or investigation** | Timeboxed issue whose acceptance criterion is a written answer in the issue, not code. The answer often becomes the acceptance criteria for the real issue. |
| **Developer unavailable mid-issue** | Back to Ready with a handover note in the issue, or a named person takes over. Never left in In progress unassigned. |

## Open questions for the reviewers

1. **Column limits.** Is roughly one In progress card per developer the right limit, and do we hold it, or is a limit the board shows but nobody enforces?
2. **Who sets what.** The proposal is product owner for horizon, priority and milestone; area owner for type and area; the developer for status. Is that the right split, and does it hold for issues that span both teams?
3. **Roadmap items without an expected result.** the team plans Epics against a roadmap item that states what should be true when it is done. An item without that stays in Backlog, however important it seems. Two things for the reviewers to confirm: that the rule is acceptable, and whether any item in the December milestone needs its expected result written before the first planning.
4. **When can the Test column go away?** Where do we verify, and which columns follow review? Today a change is verified in AT after it is merged, which is why the Test column exists. Two variants were proposed in review. (a) Verify in AT: In review → Test (AT, set on close) → Done (verified). Same-day feedback; Done does not say where the code is. (b) Verify in TT02: In review → Awaiting deploy (AT, set on close) → Test (TT02, moved on Wednesday) → Done (verified, ships with Tuesday’s release), optionally with a Tested OK step. Closer to prod; verification waits for the weekly deploy. Which do we want, what does the Friday review report under each, and can the deploy workflow on DIS label issues with tt02/prod so the board shows where the code is? Longer term: when end-to-end tests can run before merge, the Test column goes away under either variant.
5. **What a fokusområde is.** ADR-0004 makes the component (area/*) the unit of ownership and treats frontend/backend as a layer label. Should a fokusområde follow the same idea — a set of components owned end to end, through backend and frontend — rather than being split by layer? If so, which components go together, and what does that mean for who pairs with whom until June?
6. **Ongoing work and vakt.** Vendor and client questions in Slack and support cases from the CRM are handled by the weekly vakt rotation and do not go on this board or into a release. The one rule to agree: when a vakt case needs more than the vakt can do in the week — a code change, a fix that should be released — the person on vakt creates an issue, and it enters Backlog like any other. Does that match how vakt works today, and does vakt need a reserved share of capacity in the cycle, or is one person per week enough? And does the incident rule in (6) match how vakt handles an outage today?
7. **Horizon or milestone only.** Horizon is a quick sort before commitment, milestone is the commitment. Is the two-step worth a field, or is "milestone set / not set" enough and horizon dropped?

## References

- [ADR-0004: One issue and label model for the monorepo](0004-issue-and-label-model.md). Shared project board: [#4088](https://github.com/Altinn/altinn-auth/issues/4088).
- Shared project board: [Team Autorisasjon](https://github.com/orgs/Altinn/projects/196).
- Roadmap items for Autorisasjon: [digdir/roadmap](https://github.com/digdir/roadmap/issues), for example [#609](https://github.com/digdir/roadmap/issues/609) (removal of Altinn 2 roles, due 2027-01-01) and [#610](https://github.com/digdir/roadmap/issues/610) (move to DIS).
- Agent skills for the weekly report and closure candidates: (https://github.com/Altinn/altinn-auth/pull/4615).