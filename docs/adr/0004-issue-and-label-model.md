# ADR-0004: One issue and label model for the monorepo

- **Status:** Accepted
- **Date:** 2026-10-09
- **Deciders:** `@altinn/team-access-management` and `@altinn/team-access-info`, through review of the PR that adds this ADR

Drafted with AI assistance (Claude Code, model Claude Opus 5.5) from figures pulled from GitHub on 2026-10-09. Reader unspecified until a person confirms they have read it.

**TL;DR**

- Every issue has one issue type, which says what kind of work it is. It also has at least one `area/<component>` label, one for each part of the domain it touches.
- Capabilities that cut across components (consent, access packages, system user …) are `feature/*` labels. Other labels have a fixed prefix and a rule.
- Issues carry no team label. Ownership follows the component: CODEOWNERS says which team owns each component, so a reorganisation changes one file, not hundreds of issues.
- Creating an issue must stay quick. The only required field in the issue forms is the component, and "Don't know" is a valid answer. Triage and refinement fill in the rest. Forms and labels are in English; an issue may be written in English or Norwegian.
- Labels are defined in `.github/labels.yml` and changed only by pull request. Releases are milestones, and duplicate and won't-fix are close reasons.

## Context

This repository is becoming the single home for every authorization component: Access Management, Authorization, Resource Registry and Register are here, while Authentication, the Access Management frontend and Auditlog are on their way ([#4048](https://github.com/Altinn/altinn-auth/issues/4048)). Their issues come with them. On 2026-10-09 there were 646 open issues here and 411 more in the repositories still to move in, two of which are archived.

The labels do not support that. They say *when* an issue was planned and *which layer* it touches, but not *which part of the domain* it belongs to:

- 574 of 646 open issues have no `area/*` label. Teams write the area into the title instead (`Aktivitetslogg |`, `AccMgmt |`, `Authz |`), which no filter can use.
- Eleven labels act as releases or priorities and have been applied more than 1 000 times. No issue has a milestone.
- `kind/bug`, `kind/feature-request` and `kind/user-story` overlap the organisation's issue types; for bugs alone the label and the type disagree on 51 open issues. 204 open issues have no type.
- This repository has no issue forms of its own, so the organisation templates apply. They add `status/triage`, `kind/chore` and `Epic`, none of which exist here, and GitHub drops them without a warning.
- 61 labels follow four naming styles; 34 have no description and 16 have never been used.
- CODEOWNERS names no team for any code under `src/`, so neither reviews nor issues can be routed by component.
- The teams will be reorganised after the new year. Any model that stores the team on each issue has to be migrated again then.

## Decision

We will use one model, with one mechanism per question.

1. **Issue type** answers *what kind of work*: Epic, Feature, Task, Bug or Enhancement. A user story is written as a Feature, which has room for the story and its acceptance criteria; the organisation's User story type is not used here. `kind/bug`, `kind/feature-request` and `kind/user-story` are retired.
2. **`area/*`** answers *which component*. An issue has at least one: every component it touches. Large work across components is still better split into sub-issues per component. The values are `access-management`, `access-management-frontend`, `authorization`, `authentication`, `register`, `resource-registry`, `auditlog`, `packages`, `libs`, `tools`, `infra`, `devex` and `docs`. `area/infra` is the shared platform only (CI/CD, shared Terraform, Flux); infrastructure work for one component gets that component's area and `layer/infra`. `area/auditlog` is the service that stores audit events; producing an event belongs to the component that emits it. A pull request gets area labels automatically from the paths it changes, and may have more than one.
3. **`feature/*`** answers *which capability*, zero or more: `access-packages`, `roles`, `single-rights`, `instance-delegation`, `client-delegation`, `system-user`, `consent`, `access-requests`, `maskinporten`, `authorized-parties`, `activity-log`, `notifications` and `a2-decommission`. A new capability gets a label when it has more than a handful of issues.
4. **The remaining groups** each have a prefix and a rule:

   | Group | Values | Rule |
   | --- | --- | --- |
   | `layer/` | `backend`, `frontend`, `ux`, `infra`, `test` | 0–n |
   | `kind/` | `analysis`, `documentation`, `tech-debt`, `chore`, `question`, `security`, `incident`, `deploy-patch`, `testplan` | 0–1, only what the issue type does not say |
   | `status/` | `triage`, `draft`, `blocked` | 0–1. `triage`: not yet looked at. `draft`: not yet described well enough to plan. `blocked`: waiting for something; quick to set and visible on the board |
   | no prefix | `a11y`, `performance`, `breaking-change`, `legal`, `good first issue` | 0–n |
   | `consumer/` | `studio`, `dialogporten`, `correspondence`, `app-lib`, `notifications`, `altinn-portal` | 0–n, work requested by or blocking another product |

5. **Not labels.** Where GitHub has a built-in mechanism, that mechanism is used and no label repeats it:
   - A release is a milestone with a due date.
   - A duplicate is closed with the close reason *duplicate*, which links the original. Won't-fix is closed as *not planned*, with the reason in a comment.
   - When the issue that blocks another is known, it is linked with a "blocked by" relationship as well as `status/blocked`, so the dependency can be followed.
   - Priority, horizon (now, next, later) and progress are fields on the shared project board ([#4088](https://github.com/Altinn/altinn-auth/issues/4088)).
6. **Naming and language.** Label names are English, lowercase kebab-case with a group prefix, so they match the code and need no quoting in `gh`. Every label has a description. Issue forms, labels and label descriptions are in English: this is an open-source repository, and several end-user system vendors read English. The content of an issue may be written in English or Norwegian, whichever is quicker for the person writing it; every form says so. Two exceptions keep their established names: `good first issue`, which GitHub recognises, and the bot labels `dependencies`, `autorelease: pending` and `autorelease: tagged`.
7. **One source for labels.** The labels, with colour and description, live in `.github/labels.yml`, and a workflow synchronises them. A label is added, renamed or removed only by changing that file in a pull request.
8. **Ownership follows the component.** Issues carry no `team/*` label. Which team owns a component is stated once, in `.github/CODEOWNERS`, by the component's path. Review requests come from it, and the board's Team field is derived from it by the same workflow that sets `area/*`. When an issue's areas belong to one team, the field is set automatically. When they span teams, triage sets it. When the teams change, CODEOWNERS changes, and no issue needs relabelling. The starting point, taken from who deploys what in the weekly deploy checklists, is for the reviewers to confirm:

   | `area/` | Owning team today |
   | --- | --- |
   | `access-management`, `access-management-frontend`, `authorization`, `packages` | `@altinn/team-access-management` (tilgangsstyring) |
   | `authentication`, `register`, `resource-registry`, `auditlog` | `@altinn/team-access-info` (tilgangsinfo) |
   | `libs`, `tools`, `infra`, `devex`, `docs` | both teams |

9. **A low threshold, enforced by the repository.** This repository gets its own issue forms in `.github/ISSUE_TEMPLATE/`. Once a repository has its own templates, GitHub no longer offers the organisation's default ones there, so the outdated organisation templates stop applying without anyone changing them. Each form sets the issue type and a status label. Its only required field is the component, and "Don't know" is a valid answer. An incomplete issue is better than an issue never written: `status/triage` makes sure someone completes it. Members of the teams can still open a blank issue. A one-screen guide for people, [Issues and labels](../../CONTRIBUTING.md#issues-and-labels), says how to create and label an issue; this ADR keeps the reasons. `AGENTS.md` only points AI agents to this ADR and the guide.

### Alternatives considered

- **Add `area/*` labels and leave the rest.** Cheapest, but keeps release labels without dates, two ways of saying "bug", and four naming styles. Rejected.
- **Exactly one `area/*` per issue.** Makes ownership unambiguous, but forces an arbitrary choice on issues that touch, for example, both the Access Management backend and its frontend. Most such issues stay within one team, so the ambiguity it prevents is rare. Rejected.
- **A `team/*` label on every issue.** Makes the owning team visible at a glance, but has to be migrated on every reorganisation, and the next one is already planned. Rejected in favour of deriving the team from the component.
- **Board fields instead of `area/*` labels.** Fields are invisible in repository search and on pull requests, cannot be set by a path-based labeler, and need a `project`-scoped token for every automation. Rejected for area; chosen for priority, horizon and team, which are planning data.
- **Only a "blocked by" relationship, no `status/blocked`.** Precise, but needs the blocking issue to be found first and does not cover a blocker that is not an issue. Rejected as the only way; used alongside the label.
- **Norwegian forms, or forms in both languages.** Norwegian is the teams' working language, but the repository is open source and some system vendors read only English. Forms in both languages would double the template chooser and the maintenance. Rejected: the forms are in English, and the content may be in either language.
- **Strict forms with many required fields.** More complete issues, but the teams' experience is that a high threshold means fewer issues and things forgotten. Rejected.
- **Keep the area in the title prefix.** Already in use on 118 open issues, but not filterable and spelled differently by each author. Rejected.
- **Keep issues in each component's old repository.** Splits the backlog again, and cross-repository sub-issues need write access to both repositories, which has already blocked linking in practice. Rejected.

## Consequences

- Every open issue needs a type and an `area/*` once. The backfill is a best-effort script, confirmed by each team for its own components, so expect gaps and some manual correction during the transition ([#4360](https://github.com/Altinn/altinn-auth/issues/4360), [#4361](https://github.com/Altinn/altinn-auth/issues/4361)).
- An issue with several areas shows up under each of them, so a count per area can add up to more than the number of issues.
- Renaming `Backend` and `Frontend` to `layer/*`, and removing `team/*`, breaks saved searches and board views that filter on the old names. They must be updated in the same change.
- Deleting a release label also removes it from closed issues. Whether that history is kept is decided in [#4362](https://github.com/Altinn/altinn-auth/issues/4362).
- A vertical that is added or moved in brings its `area/*` label, its CODEOWNERS line and its labeler rule in the same pull request. Issues transferred from its old repository are relabelled to this model on transfer ([#4363](https://github.com/Altinn/altinn-auth/issues/4363)).
- The organisation issue templates stop applying here when this repository's issue forms land ([#4357](https://github.com/Altinn/altinn-auth/issues/4357)). This ADR does not change them. Updating them for other repositories is a separate request to the owners of `Altinn/.github`.
- `Blocked` is renamed to `status/blocked`. `status/duplicate` and `status/wontfix` are removed; GitHub's close reasons are already in use, with 21 issues closed as duplicate and 72 as not planned.
- A reorganisation of the teams is a change to CODEOWNERS, not to this ADR. A change to the label groups is a new ADR that supersedes this one. Adding a single `feature/*` or `consumer/*` value is not; it is a change to `.github/labels.yml`.

## References

- Epic [#4354](https://github.com/Altinn/altinn-auth/issues/4354) and its sub-issues; this ADR is [#4355](https://github.com/Altinn/altinn-auth/issues/4355). Issue forms: [#4357](https://github.com/Altinn/altinn-auth/issues/4357).
- Repository consolidation: [#4048](https://github.com/Altinn/altinn-auth/issues/4048). Shared project board: [#4088](https://github.com/Altinn/altinn-auth/issues/4088).
- Organisation issue templates: [Altinn/.github](https://github.com/Altinn/.github/tree/main/.github/ISSUE_TEMPLATE).
- Deploy checklists used for the starting ownership: `.github/templates/tilgangsstyring-pnd.md` and `.github/templates/tilgangsinfo-pnd.md`.
