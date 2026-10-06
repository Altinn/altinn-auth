# ADR-0001: Access Management is the policy information point for delegations, roles and access packages

- **Status:** Accepted
- **Date:** 2024-01-24, when the PDP started reading delegations through the Access Management API ([altinn-authorization#569](https://github.com/Altinn/altinn-authorization-archive/pull/569)). Recorded on 2026-10-06.
- **Deciders:** The API was asked for by @TheTechArch ([altinn-access-management#146](https://github.com/Altinn/altinn-access-management-archive/issues/146)) and the integration by @jonkjetiloye ([altinn-authorization#497](https://github.com/Altinn/altinn-authorization-archive/issues/497)). Implemented by @andreasisnes. Roles followed in 2026, by @jonkjetiloye.

**TL;DR**

- The PDP gets delegations, roles and access packages from Access Management's internal policy-information API. It does not read another service's database, and it does not write delegation data.
- Responses are cached in the PDP for five minutes.
- The PDP trusts Access Management's filtering, for example of which units a user holds a key role for.
- Leftovers from the old model still sit in the code, switched off. Do not build on them.

> Recorded after the fact for [#4079](https://github.com/Altinn/altinn-auth/issues/4079), from the sources under References and nothing else. Drafted with Claude (Opus 5.5). Human reader: not confirmed.

## Context

In 2022 the goal was set to split Authorization into separate components with clear responsibilities ([altinn-authorization#23](https://github.com/Altinn/altinn-authorization-archive/issues/23)). By September 2022 the delegation component had moved from the Authorization repository to Access Management ([altinn-access-management#80](https://github.com/Altinn/altinn-access-management-archive/issues/80)), but the PDP still read app delegations straight from Access Management's database.

[altinn-access-management#146](https://github.com/Altinn/altinn-access-management-archive/issues/146) (2022-10-14) asked Access Management to expose an API for the PDP to fetch all delegation changes for a user, a reportee and an app or resource. [altinn-authorization#497](https://github.com/Altinn/altinn-authorization-archive/issues/497) (2023-09-29) replaced the direct read with that API, which would also cover resources delegated from the Resource Registry.

Altinn 2 was shut down on 2026-06-19. The PDP still looked up roles there, and [#2749](https://github.com/Altinn/altinn-auth/issues/2749) set the task of replacing that lookup with Altinn 3, either directly against the database or through Access Management.

## Decision

1. **Delegations come from Access Management's API.** The PDP calls `policyinformation/getdelegationchanges` through `AccessManagementWrapper`, not a database ([altinn-authorization#569](https://github.com/Altinn/altinn-authorization-archive/pull/569)). The delegation API that Authorization itself offered was removed in [#380](https://github.com/Altinn/altinn-auth/pull/380) (2025-02-25).
2. **Responses are cached for five minutes.** The review of #569 asked to keep the five-minute cache the direct read had. The access-package lookup got the same cache after it put heavy load on the database for multi-requests ([#1836](https://github.com/Altinn/altinn-auth/issues/1836)).
3. **Roles and access packages come from Access Management too**, through `policyinformation/roles-and-accesspackages` ([#3114](https://github.com/Altinn/altinn-auth/pull/3114), 2026-05-12), behind the feature flag `AccessManagementAsPipForRoles`. Of the two options in #2749, the lookup goes through Access Management.
4. **The PDP trusts Access Management's answer.** A delegation returned with `coveredByPartyId` is taken to mean that the user holds a key role for that unit, without a separate lookup ([#3284](https://github.com/Altinn/altinn-auth/pull/3284)).

### Alternatives considered

- **Keep reading Access Management's database directly.** The implementation before #569; replaced by the API.
- **Look up roles directly in the Altinn 3 database.** Named in #2749 next to going through Access Management. The sources do not say why Access Management was chosen.

## Consequences

- Access Management is on the PDP's decision path. A slow or failing policy-information API affects every decision that depends on delegations, roles or packages, within what the cache absorbs. Reviewing #569, @jonkjetiloye pointed out that the PDP no longer controls when those database lookups happen.
- A change to what Access Management's policy-information API returns is a change to authorization semantics. Treat it that way in review, in both verticals.
- The delegation policies themselves are read from blob storage, at the path and version that Access Management returns with each delegation change.
- **Leftovers, switched off:** a `delegation` schema with an EF migration and `DelegationMetadataRepository`, behind `PostgreSQLSettings:EnableDBConnection=false`, and the Altinn 2 `RolesClient`, behind the feature flag above. [#3851](https://github.com/Altinn/altinn-auth/issues/3851) proposes removing them. Whether the flag is on in each environment is not visible in this repository.

## References

- Background: [altinn-authorization#23](https://github.com/Altinn/altinn-authorization-archive/issues/23), [altinn-access-management#80](https://github.com/Altinn/altinn-access-management-archive/issues/80).
- Delegations: [altinn-access-management#146](https://github.com/Altinn/altinn-access-management-archive/issues/146), [altinn-authorization#497](https://github.com/Altinn/altinn-authorization-archive/issues/497), [altinn-authorization#569](https://github.com/Altinn/altinn-authorization-archive/pull/569), [#380](https://github.com/Altinn/altinn-auth/pull/380), [#1836](https://github.com/Altinn/altinn-auth/issues/1836).
- Roles and key roles: [#2749](https://github.com/Altinn/altinn-auth/issues/2749), [#3114](https://github.com/Altinn/altinn-auth/pull/3114), [#3284](https://github.com/Altinn/altinn-auth/pull/3284). Cleanup: [#3851](https://github.com/Altinn/altinn-auth/issues/3851).
- Code: `AuthorizeUsingDelegations` and `GetAllCachedDelegationChanges` in `src/Altinn.Authorization/Controllers/DecisionController.cs`; `src/Altinn.Authorization/Services/Implementation/AccessManagementWrapper.cs`; on the other side, `PolicyInformationPointController` in Access Management's `Api.Internal`.
- The repositories `altinn-authorization` and `altinn-access-management` are archived as `altinn-authorization-archive` and `altinn-access-management-archive`. Their code moved here without history.
