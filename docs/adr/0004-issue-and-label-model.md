# ADR-0004: One issue and label model for the monorepo

- **Status:** Proposed
- **Date:** 2026-10-09
- **Deciders:** `@altinn/team-access-management` and `@altinn/team-access-info`, through review of the PR that adds this ADR

Drafted with AI assistance (Claude Code, model Claude Opus 5.5) from figures pulled from GitHub on 2026-10-09.

**TL;DR**

- Every issue has exactly one issue type, which says what kind of work it is, and exactly one `area/<component>` label, which says which part of the domain owns it.
- Capabilities that cut across components (consent, access packages, system user …) are `feature/*` labels. Other labels have a fixed prefix and a rule.
- Where GitHub has a built-in mechanism, use it instead of a label. Releases are milestones, and blocking is a "blocked by" relationship. Duplicate and won't-fix are close reasons. Priority, horizon, team and progress are fields on the shared project board.
- Labels are defined in `.github/labels.yml`, changed only by pull request, and applied by this repository's own issue forms and workflows, not by the organisation templates.
- Each `area/*` has one owning team. The same table drives CODEOWNERS, the `team/*` label and the board.

## Context

This repository is becoming the single home for every authorization component: Access Management, Authorization, Resource Registry and Register are here, while Authentication and the Access Management frontend are on their way ([#4048](https://github.com/Altinn/altinn-auth/issues/4048)). Their issues come with them. On 2026-10-09 there were 646 open issues here and 384 more in the repositories still to move in, two of which are archived.

The labels do not support that. They say *when* an issue was planned and *which layer* it touches, but not *which part of the domain* it belongs to:

- 574 of 646 open issues have no `area/*` label. Teams write the area into the title instead (`Aktivitetslogg |`, `AccMgmt |`, `Authz |`), which no filter can use.
- Eleven labels act as releases or priorities and have been applied more than 1 000 times. No issue has a milestone.
- `kind/bug`, `kind/feature-request` and `kind/user-story` overlap the organisation's issue types; for bugs alone the label and the type disagree on 51 open issues. 204 open issues have no type.
- This repository has no issue forms of its own, so the organisation templates apply. They add `status/triage`, `kind/chore` and `Epic`, none of which exist here, and GitHub drops them without a warning.
- 61 labels follow four naming styles; 34 have no description and 16 have never been used.
- CODEOWNERS names no team for any code under `src/`, so neither reviews nor issues can be routed by component.

## Decision

We will use one model, with one mechanism per question.

1. **Issue type** answers *what kind of work*: Epic, Feature, Task, Bug, Enhancement or User story. Exactly one per issue. `kind/bug`, `kind/feature-request` and `kind/user-story` are retired.
2. **`area/*`** answers *which component*. Exactly one per issue. The values are the verticals: `access-management`, `access-management-frontend`, `authorization`, `authentication`, `register`, `resource-registry`, `packages`, `libs`, `tools`, `infra`, `devex` and `docs`. A pull request gets the same labels automatically from the paths it changes, and may have more than one.
3. **`feature/*`** answers *which capability*, zero or more: `access-packages`, `roles`, `single-rights`, `instance-delegation`, `client-delegation`, `system-user`, `consent`, `access-requests`, `maskinporten`, `authorized-parties`, `activity-log`, `notifications` and `a2-decommission`. A new capability gets a label when it has more than a handful of issues.
4. **The remaining groups** each have a prefix and a rule:

   | Group | Values | Rule |
   | --- | --- | --- |
   | `layer/` | `backend`, `frontend`, `ux`, `test` | 0–n |
   | `kind/` | `analysis`, `documentation`, `tech-debt`, `chore`, `question`, `security`, `incident`, `deploy-patch`, `testplan` | 0–1, only what the issue type does not say |
   | `status/` | `triage`, `draft` | 0–1. An untriaged report, or an idea not yet described well enough to plan. Removed when the issue is accepted onto the board |
   | no prefix | `a11y`, `performance`, `breaking-change`, `legal`, `good first issue` | 0–n |
   | `consumer/` | `studio`, `dialogporten`, `correspondence`, `app-lib`, `notifications`, `altinn-portal` | 0–n, work requested by or blocking another product |
   | `team/` | `tilgangsstyring`, `tilgangsinfo` | 0–1, set by automation from `area/*` |

5. **Not labels.** Where GitHub has a built-in mechanism, that mechanism is used and no label repeats it:
   - A release is a milestone with a due date.
   - A blocked issue has a "blocked by" relationship to the issue that blocks it. If the blocker is not a GitHub issue, such as a decision outside the team, the board Status is *Blocked*, and a comment says what the issue is waiting for.
   - A duplicate is closed with the close reason *duplicate*, which links the original. Won't-fix is closed as *not planned*, with the reason in a comment.
   - Priority, horizon (now, next, later), team and progress are fields on the shared project board ([#4088](https://github.com/Altinn/altinn-auth/issues/4088)).
6. **Naming.** Label names are English, lowercase kebab-case with a group prefix, so they match the code and need no quoting in `gh`. Descriptions are Norwegian and mandatory. Two exceptions keep their established names: `good first issue`, which GitHub recognises, and the bot labels `dependencies`, `autorelease: pending` and `autorelease: tagged`.
7. **One source.** The labels, with colour and description, live in `.github/labels.yml`, and a workflow synchronises them. A label is added, renamed or removed only by changing that file in a pull request.
8. **Ownership.** Each `area/*` has one owning team. The table below is the single source for CODEOWNERS, for the `team/*` label and for the board's Team field. It is proposed from who deploys what in the weekly deploy checklists in `.github/templates/`; reviewers confirm or correct it before this ADR is accepted.

   | `area/` | Owning team | Basis |
   | --- | --- | --- |
   | `access-management` | `@altinn/team-access-management` (tilgangsstyring) | deploys Access Management and Delegation Events |
   | `access-management-frontend` | `@altinn/team-access-management` | deploys the Access Management frontend |
   | `authorization` | `@altinn/team-access-management` | deploys Authorization |
   | `packages` | `@altinn/team-access-management` | PEP and ABAC are the client side of Authorization; *to confirm* |
   | `authentication` | `@altinn/team-access-info` (tilgangsinfo) | deploys Authentication |
   | `register` | `@altinn/team-access-info` | deploys Register |
   | `resource-registry` | `@altinn/team-access-info` | deploys Resource Registry |
   | `libs`, `tools`, `infra`, `devex`, `docs` | both teams | shared; *to confirm* |

9. **Enforced by the repository, not by instructions.** This repository gets its own issue forms in `.github/ISSUE_TEMPLATE/`. When a repository has its own templates, GitHub stops offering the organisation's default ones there, so the outdated organisation templates stop applying without anyone having to change them. Each form sets the issue type, `status/triage` and a required component, and a workflow turns the component into `area/*` and `team/*`. Rules that a form or workflow cannot enforce are written down for people in `CONTRIBUTING.md`. `AGENTS.md` only points AI agents to this ADR; it carries no second copy of the rules.

### Alternatives considered

- **Add `area/*` labels and leave the rest.** Cheapest, but keeps release labels without dates, two ways of saying "bug", and four naming styles. Rejected.
- **Board fields instead of `area/*` labels.** Fields are invisible in repository search and on pull requests, cannot be set by a path-based labeler, and need a `project`-scoped token for every automation. Rejected for area; chosen for priority, horizon and team, which are planning data.
- **Keep the area in the title prefix.** Already in use on 118 open issues, but not filterable and spelled differently by each author. Rejected.
- **Keep issues in each component's old repository.** Splits the backlog again, and cross-repository sub-issues need write access to both repositories, which has already blocked linking in practice. Rejected.

## Consequences

- Every open issue needs a type and an `area/*` once. The backfill is scripted and confirmed by each team for its own components ([#4360](https://github.com/Altinn/altinn-auth/issues/4360), [#4361](https://github.com/Altinn/altinn-auth/issues/4361)).
- Renaming `Backend` and `Frontend` to `layer/*` breaks saved searches and board views that filter on the old names. They must be updated in the same change.
- Deleting a release label also removes it from closed issues. Whether that history is kept is decided in [#4362](https://github.com/Altinn/altinn-auth/issues/4362).
- A vertical that is added or moved in brings its `area/*` label, its CODEOWNERS line, its labeler rule and its row in the ownership table in the same pull request. Issues transferred from its old repository are relabelled to this model on transfer ([#4363](https://github.com/Altinn/altinn-auth/issues/4363)).
- The organisation issue templates stop applying here when this repository's issue forms land ([#4357](https://github.com/Altinn/altinn-auth/issues/4357)). This ADR does not change them. Updating them for other repositories is a separate request to the owners of `Altinn/.github`.
- `Blocked` (13 open issues, of which only one has a "blocked by" relationship), `status/duplicate` and `status/wontfix` are removed. Each open `Blocked` issue gets a relationship or the board Status *Blocked* before the label goes. GitHub's close reasons are already in use: 21 issues closed as duplicate and 72 as not planned.
- A change to the groups or the ownership table is a new ADR that supersedes this one. Adding a single `feature/*` or `consumer/*` value is not; it is a change to `.github/labels.yml`.

## References

- Epic [#4354](https://github.com/Altinn/altinn-auth/issues/4354) and its sub-issues; this ADR is [#4355](https://github.com/Altinn/altinn-auth/issues/4355).
- Repository consolidation: [#4048](https://github.com/Altinn/altinn-auth/issues/4048). Shared project board: [#4088](https://github.com/Altinn/altinn-auth/issues/4088).
- Organisation issue templates: [Altinn/.github](https://github.com/Altinn/.github/tree/main/.github/ISSUE_TEMPLATE).
- Deploy checklists used for the ownership table: `.github/templates/tilgangsstyring-pnd.md` and `.github/templates/tilgangsinfo-pnd.md`.
