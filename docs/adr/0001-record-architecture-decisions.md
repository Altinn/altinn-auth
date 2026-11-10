# ADR-0001: Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-09-18
- **Deciders:** The altinn-auth maintainers, `@altinn/team-access-management`

## Context

This repository holds the Policy Decision Point, the Policy Enforcement Point and Access Management for Altinn 3, and is becoming the monorepo for the whole authorization domain: register, resource registry, authentication and the Access Management frontend are being moved in with their history ([#4049](https://github.com/Altinn/altinn-auth/issues/4049), with the moves in [#4056](https://github.com/Altinn/altinn-auth/issues/4056) to [#4059](https://github.com/Altinn/altinn-auth/issues/4059)).

It carries a lot of implicit knowledge: why a code path that looks removable is load-bearing, which behaviour production depends on, why a dismissed scanner finding is a false positive, why a delegation check is ordered the way it is. That knowledge lives in people's heads and in closed PR discussions, and is re-derived at cost by new contributors and by AI coding agents, who need the *why* and not only the *what*.

altinn-authentication already keeps ADRs in `docs/adr/`, and its agent guidance tells agents to read the relevant ADR before changing an authentication flow. When it moves in, those ADRs come with it and must not collide with anything here.

## Decision

We will record significant architectural and behavioural decisions as Architecture Decision Records using the lightweight format in [template.md](template.md).

- **Cross-cutting decisions** (repository conventions, build and CI, dependencies between verticals, anything that binds more than one vertical) live in `docs/adr/` at the repository root.
- **Vertical-specific decisions** (an authorization flow, policy evaluation, a public API contract, a data model) live in that vertical's own folder: `src/apps/<Vertical>/docs/adr/`, or the equivalent under `src/pkgs`, `src/libs` and `src/tools`.
- **Numbering is per folder**, sequential and zero-padded from `0001`. A repository moved into the monorepo keeps its ADR numbers unchanged; only its folder changes.
- ADRs are **immutable**. A changed decision gets a new ADR that supersedes the old one.
- A decision-worthy change adds or supersedes an ADR **in the same PR** as the change.

## Consequences

- Contributors and agents get a durable, greppable record of *why*, kept in the folder closest to the code it concerns.
- A small ongoing cost per decision-worthy PR. The rule for when an ADR is required will be written into `CONTRIBUTING.md` ([#4082](https://github.com/Altinn/altinn-auth/issues/4082), [#4079](https://github.com/Altinn/altinn-auth/issues/4079)).
- Important existing decisions in the Authorization and Access Management verticals will be backfilled as ADRs with their original dates ([#4079](https://github.com/Altinn/altinn-auth/issues/4079)).
- Links between root and vertical ADRs are relative paths; the link check in [#4081](https://github.com/Altinn/altinn-auth/issues/4081) keeps them honest.

## References

- Issue [#4076](https://github.com/Altinn/altinn-auth/issues/4076), part of epic [#4075](https://github.com/Altinn/altinn-auth/issues/4075).
- Repository consolidation: decision in [#4049](https://github.com/Altinn/altinn-auth/issues/4049), moves in [#4056](https://github.com/Altinn/altinn-auth/issues/4056) to [#4059](https://github.com/Altinn/altinn-auth/issues/4059).
- Reference implementation: [altinn-authentication `docs/adr/`](https://github.com/Altinn/altinn-authentication/tree/main/docs/adr).
