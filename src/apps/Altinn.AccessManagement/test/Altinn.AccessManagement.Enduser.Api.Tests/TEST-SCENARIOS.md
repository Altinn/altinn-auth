# Enduser API Test Scenarios

This document describes the functionality offered through the Enduser APIs, the people and organizations used in test scenarios, their relationships, and how test coverage maps to real-world delegation workflows.

## What the Enduser API Does

The Enduser API provides end users (citizens and business owners) with self-service access management in Altinn. It exposes two controllers:

### ConnectionsController — Managing Who Has Access to What

The ConnectionsController lets authenticated users manage **connections** (rightholder relationships) between parties, and control what access those connections carry. It covers four layers of access:

**Connections (Rightholder Management)**
A connection is a rightholder relationship between two parties — for example, an organization giving a person the ability to act on its behalf. Connections can be created, listed, and removed. Removing a connection with active packages or delegations requires explicit cascade.

**Access Packages**
Access packages are pre-defined bundles of permissions (e.g. "Salary with sensitive personal data", "Customs", "Accounting"). They can be assigned to rightholder connections to grant broad categories of access. The delegation check endpoint lets users discover which packages they are authorized to assign.

**Roles**
Roles (like Managing Director/DAGL, Chair of the Board/LEDE, Rightholder) define the organizational relationship between parties. The API allows viewing roles and checking which roles can be delegated. Role delegation to end users is not yet implemented (the RemoveRole endpoint returns 404).

**Resource Delegations**
Resource-level delegations grant specific rights (read, write, instantiate, confirm, etc.) on an Altinn resource (app or service) from one party to another. The API supports checking which rights are delegatable, adding/updating/removing resource delegations, and viewing the resulting direct and indirect rights. Indirect rights arise through key-role inheritance — for example, when Mille Hundefrisor is a rightholder of Dumbo Adventures, Thea (as Managing Director of Mille) inherits those rights indirectly.

**Instance Delegations**
Instance-level delegations are more granular than resource delegations — they grant rights on a specific instance (e.g. a particular tax filing form submission, a specific food safety inspection report). Instance IDs follow URN format (`urn:altinn:instance-id:partyId/guid`). The full lifecycle is supported: delegation check, add, update, remove, list instances, view instance rights, and list users with access to an instance.

### AuthorizedPartiesController — Discovering What You Have Access To

The AuthorizedPartiesController lets an authenticated user discover all parties they can act on behalf of, along with what access they have. The response can optionally include:
- **Roles** — Altinn 2 and key roles (DAGL, LEDE, PRIV, REVI, etc.)
- **Access Packages** — pre-defined permission bundles
- **Resources** — individual Altinn resources the user has access to
- **Instances** — specific instance-level delegations

The `includeResources` flag returns resource identifiers from both resource-level and instance-level delegations. The `includeInstances` flag returns the detailed instance delegation entries (with ResourceId, InstanceId, InstanceRef). When both flags are set, instance delegations appear only under `AuthorizedInstances` (not duplicated in `AuthorizedResources`).

### Authorization Model

Every endpoint enforces two levels of authorization:
1. **Scope-based** — the OAuth token must carry the correct scope for the operation direction and type:
   - Read operations require either `toothers.read` or `fromothers.read` matching the query direction
   - Write operations require the corresponding `.write` scope
   - Bidirectional endpoints accept either direction's scope
   - Instance delegation endpoints additionally require the `InstanceDelegation` policy
2. **PDP-based** — the Platform Decision Point evaluates XACML policies to determine if the user has the authority to perform the specific action on the specific resource/party combination

### Key Concepts

- **To-others vs From-others**: Every query has a direction. "To-others" means the party is giving access (e.g. Dumbo giving access to Mille). "From-others" means the party is receiving access (e.g. Mille viewing what Dumbo gave them). The `party` parameter identifies which perspective.
- **Key-role inheritance (nøkkelrollearv)**: When an organization is a rightholder, persons with key roles (DAGL, LEDE) at that organization inherit the rights indirectly. For example, if Mille Hundefrisor is a rightholder of Dumbo Adventures, Thea (as MD of Mille) gets indirect access.
- **XACML delegation policies**: When rights are delegated, an XACML policy is written to blob storage. Resource delegations use partyId (integer) as the subject identifier, while instance delegations use the party UUID.

---

## People

| Name | Description | Key Roles |
|------|-------------|-----------|
| **Malin Emilie** | Ambitious young entrepreneur who co-founded Dumbo Adventures. Handles all the administrative and financial paperwork. The go-to person for testing "delegator" scenarios. | Managing Director of Dumbo Adventures |
| **Thea BFF** | Malin's best friend since childhood. Runs her own dog grooming business but also helps out at Dumbo Adventures. Multi-tasker extraordinaire. Important for testing key-role inheritance — as MD of Mille, she inherits rights when Mille is a rightholder of Dumbo. | Managing Director of Mille Hundefrisor, Rightholder at Dumbo Adventures |
| **Josephine Yvonnesdottir** | Freelance graphic designer who does contract work for Kaos Magic Design and Arts. Has been given access to specific tax filing and food safety instances on behalf of Kaos. The primary "instance delegation recipient" in tests. | Rightholder at Kaos Magic Design and Arts |
| **Jinx Arcane** | Creative director and founder of Kaos Magic Design and Arts. Known for bold visual designs and chaotic brainstorming sessions. Used for testing delegation from the organization's Managing Director perspective. | Managing Director of Kaos Magic Design and Arts |
| **Alex The Artist** | A uniquely skilled designer — there is nothing she can't do. Whether it's 3D modeling, typography, welding stage props, or coding interactive installations, Alex delivers. She's also Jinx's business partner and the more level-headed half of Kaos, keeping the books balanced while Jinx dreams big. Used for testing person-to-person delegation (Alex to Milena). | Chair of the Board at Kaos Magic Design and Arts |
| **Milena Solstad** | Thea's most trusted employee at the dog grooming salon. Handles the business side when Thea is off helping Malin. Used for testing key-role inheritance (as LEDE of Mille) and as a recipient in Alex's person-to-person delegation. | Chair of the Board at Mille Hundefrisor |
| **Bodil Farmor** | A retired grandmother who occasionally helps with Dumbo Adventures events. Used in person lookup tests (AddRightholder with PersonInput). | (no org role) |
| **Lars Bakke** | A seasoned bakery owner who keeps things running smoothly at Baker Johnsen. | Managing Director of Baker Johnsen |

## Organizations

| Name | Type | Description |
|------|------|-------------|
| **Dumbo Adventures AS** | AS | A small adventure tourism company run by Malin Emilie. Offers guided hiking, kayaking, and northern lights trips. The primary "from" party for testing resource and instance delegations. |
| **Mille Hundefrisor** | ENK | Thea's sole proprietorship dog grooming salon, famous in the neighborhood for poodle haircuts. Important for testing key-role inheritance — when Mille is a rightholder of Dumbo, Thea and Milena inherit access indirectly. |
| **Kaos Magic Design and Arts** | AS | A creative agency founded by Jinx Arcane, specializing in event design, visual effects, and immersive art installations. The primary party for testing instance delegations (has seeded instance delegations to Josephine). |
| **Baker Johnsen** | AS | A traditional Norwegian bakery. Used in RemoveAssignment tests as a clean relationship target with no packages or delegations. |
| **Svendsen Automobil** | AS | An auto repair shop. Used to test removing non-existent connections (verifying idempotent delete behavior). |

## Relationships (Default Seed)

```
Dumbo Adventures AS
  |-- Malin Emilie .............. Managing Director (DAGL)
  |-- Thea BFF ................. Rightholder + SalarySpecialCategory package
  |-- Kaos Magic Design and Arts  Auditor (org-to-org)

Mille Hundefrisor
  |-- Thea BFF ................. Managing Director (DAGL)
  |-- Milena Solstad ........... Chair of the Board (LEDE)

Kaos Magic Design and Arts
  |-- Jinx Arcane .............. Managing Director (DAGL)
  |-- Alex The Artist .......... Chair of the Board (LEDE)
  |-- Josephine Yvonnesdottir .. Rightholder + 2 instance delegations (see below)

Private Persons (self-to-self)
  Malin Emilie, Thea, Josephine, Milena, Jinx Arcane, Alex The Artist
```

### Key-Role Inheritance Paths

These paths explain why certain people get indirect access in tests:

- **Thea** has indirect access to Dumbo's resources when Mille is a rightholder of Dumbo, because Thea is MD of Mille
- **Milena** has indirect access to Dumbo's resources when Mille is a rightholder of Dumbo, because Milena is Chair of the Board of Mille
- **Alex** has indirect access to Dumbo's resources because Kaos is Auditor of Dumbo, and Alex is Chair of the Board of Kaos

### Relationships Added by Individual Tests

| Test Class | Relationship | Purpose |
|------------|-------------|---------|
| GetResourceRights | Dumbo -> Mille (Rightholder) | Resource rights with delegation policies; tests key-role inheritance to Thea and Milena |
| GetResources | Dumbo -> Mille (Rightholder) | Resource listing for a connection |
| AddResourceRights | Dumbo -> Mille (Rightholder) | Malin delegates resource rights from Dumbo to Mille |
| UpdateResourceRights | Dumbo -> Mille (Rightholder) | Malin updates resource rights from Dumbo to Mille |
| RemoveResource | Dumbo -> Mille (Rightholder) | Remove resource delegation from both to-others and from-others perspectives |
| AddInstanceRights | Dumbo -> Kaos (Rightholder) | Malin delegates instance rights from Dumbo to Kaos |
| RemoveInstance | Dumbo -> Kaos (Rightholder) | Add then remove instance delegation, bidirectional |
| UpdateInstanceRights | Jinx -> Thea (Rightholder), Alex -> Milena (Rightholder) | Person-to-person instance delegation: add, update (increase + reduce rights) |
| RemoveAssignment | Dumbo -> Baker Johnsen (Rightholder) | Clean connection for basic remove test (no dependencies) |

## Resources

| Name | RefId | Description |
|------|-------|-------------|
| **Sirius Skattemelding** | `app_skd_sirius-skattemelding-v1` | Tax return filing service from the Norwegian Tax Administration (Skatteetaten). The main resource used across instance and resource delegation tests. Has 9 delegatable right keys covering instantiate, read, write, and confirm actions across Task_1, Task_2, Task_3, and EndEvent_1 workflow stages. |
| **Mattilsynet Bakery Service** | `app_mat_mattilsynet-baker-konditorvare` | Food safety inspection reporting service from the Norwegian Food Safety Authority (Mattilsynet). Used as a secondary resource in instance tests. Has fewer delegatable actions (instantiate, read, write, delete, complete, events/read). |
| **Nav Sykepenger Dialog** | `nav_sykepenger_dialog` | Dialogue service for sickness benefits from NAV. A non-app resource (no org/app split). Used in resource delegation check tests. Has 3 rights: read, access, subscribe. |
| **Nav Sykepenger Sykmelding** | `nav_sykepenger_sykmelding` | Sick leave notification service from NAV. Used in RemoveResource tests for the add-then-remove lifecycle. |
| **Skattemelding** | `app_skd_skattemelding` | Generic tax return resource used in resource listing tests (GetResources). |
| **MVA-melding** | `app_skd_mva-melding` | VAT reporting resource used alongside Skattemelding in resource listing tests (GetResources). |

## Access Packages

| Name | URN | Used In |
|------|-----|---------|
| **SalarySpecialCategory** | `urn:altinn:accesspackage:lonn-personopplysninger-saerlig-kategori` | Pre-assigned to Dumbo->Thea. Tested in GetPackages and RemoveAssignment (cascade vs non-cascade). |
| **AccountingAndEconomicReporting** | `urn:altinn:accesspackage:regnskap-okonomi-rapport` | Added by AddAssignmentPackage (by packageId), removed by RemovePackages. |
| **Customs (Toll)** | `urn:altinn:accesspackage:toll` | Added by AddAssignmentPackage (by URN string), removed by RemovePackages (from-others direction). |

## Instance Delegations (Default Seed)

Josephine has been given access to two specific instances on behalf of Kaos. These delegations include XACML policies on disk that define the exact rights:

| From | To | Resource | Instance ID | Delegation Policy | Rights Granted |
|------|----|----------|-------------|-------------------|----------------|
| Kaos | Josephine | Sirius Skattemelding | `urn:altinn:instance-id:50315678/b1a2c3d4-...` | `sirius-skattemelding-v1/50315678/p5049963/delegationpolicy.xml` | read, write (base) + read, write (Task_1) |
| Kaos | Josephine | Mattilsynet Bakery | `urn:altinn:instance-id:50315678/a2b3c4d5-...` | `mattilsynet-baker-konditorvare/50315678/p5049963/delegationpolicy.xml` | read only |

## Test Coverage: 24/24 Endpoints (154 tests)

### ConnectionsController — Connections

| Endpoint | Test File | Tests | Scenarios |
|----------|-----------|-------|-----------|
| `GET /connections` | GetConnections | 6 | Bidirectional scope enforcement; from/to-others direction; SystemUser as to-party |
| `POST /connections` | AddRightholder | 5 | Add org via to-param; add person via PersonInput body; scope enforcement |
| `DELETE /connections` | RemoveAssignment | 6 | Basic remove with round-trip; non-cascade fails with packages; cascade succeeds; idempotent delete; scope enforcement |
| `GET /connections/users` | GetAvailableUsers | 2 | List available users for Dumbo; scope enforcement |

### ConnectionsController — Access Packages

| Endpoint | Test File | Tests | Scenarios |
|----------|-----------|-------|-----------|
| `GET /connections/accesspackages` | GetPackages | 6 | Malin to-others sees SalarySpecialCategory; Thea from-others sees same; empty connection returns empty; wrong scope direction; write scope on read |
| `POST /connections/accesspackages` | AddAssignmentPackage | 6 | Add by packageId with round-trip; add by URN; invalid package; scope enforcement |
| `DELETE /connections/accesspackages` | RemovePackages | 4 | Remove by packageId (to-others) with round-trip; remove by URN (from-others Josephine); scope enforcement |
| `GET /connections/accesspackages/delegationcheck` | CheckPackage | 6 | By packageIds; by URN strings; no filter (all delegatable); scope enforcement |

### ConnectionsController — Roles

| Endpoint | Test File | Tests | Scenarios |
|----------|-----------|-------|-----------|
| `GET /connections/roles` | GetRoles | 7 | Malin sees DAGL for herself; Malin sees Rightholder for Thea; Thea from-others; Jinx sees Josephine's Rightholder at Kaos; wrong scope direction; write scope on read |
| `DELETE /connections/roles` | RemoveRole | 2 | Not implemented (never returns success); scope enforcement still active |
| `GET /connections/roles/delegationcheck` | DelegationCheckRoles | 4 | Malin checks delegatable roles; scope enforcement |

### ConnectionsController — Resources

| Endpoint | Test File | Tests | Scenarios |
|----------|-----------|-------|-----------|
| `GET /connections/resources` | GetResources | 7 | Malin to-others sees Skattemelding + MVA; Thea from-others sees same; empty direction; wrong scope direction; write scope on read |
| `GET /connections/resources/rights` | GetResourceRights | 8 | Malin sees 9 direct rights Dumbo->Mille; Thea from-others sees same; Thea gets 9 indirect rights via key-role (DAGL of Mille); Milena gets 9 indirect rights (LEDE of Mille); scope enforcement |
| `POST /connections/resources/rights` | AddResourceRights | 5 | Delegation check + delegate all right keys with XACML policy verification; invalid resource; no connection; scope enforcement |
| `PUT /connections/resources/rights` | UpdateResourceRights | 5 | Update with XACML policy verification; empty right keys; scope enforcement |
| `DELETE /connections/resources` | RemoveResource | 5 | Malin removes (to-others); Thea removes (from-others); invalid resource; scope enforcement |
| `GET /connections/resources/delegationcheck` | CheckResource | 4 | Malin full access (roles + packages); Thea package-only access; partial access (some denied); scope enforcement |

### ConnectionsController — Instances

| Endpoint | Test File | Tests | Scenarios |
|----------|-----------|-------|-----------|
| `GET /connections/resources/instances` | GetInstances | 9 | Jinx to-others sees 2 instances; Josephine from-others sees same; empty reverse direction; resource filter returns only matching; 401 no token; scope enforcement |
| `GET /connections/resources/instances/rights` | GetInstanceRights | 8 | Jinx sees direct rights for Sirius (to-others); Josephine from-others sees same; Mattilsynet (fewer rights); invalid URN -> 400; 401 no token; scope enforcement |
| `POST /connections/resources/instances/rights` | AddInstanceRights | 8 | Delegation check + delegate with XACML policy verification (UUID subject) + round-trip via GetInstanceRights; empty right keys -> 400; invalid URN -> 400; invalid resource -> 400; 401 no token; scope enforcement |
| `PUT /connections/resources/instances/rights` | UpdateInstanceRights | 6 | Jinx->Thea add 1 then update to 3 with round-trip; Alex->Milena add 2 then reduce to 1 (verify removed); 401 no token; scope enforcement |
| `DELETE /connections/resources/instances` | RemoveInstance | 4 | Malin removes (to-others) with round-trip (verify gone); Jinx removes (from-others); scope enforcement |
| `GET /connections/resources/instances/delegationcheck` | CheckInstance | 5 | Malin checks Sirius (9+ right keys); Malin checks Mattilsynet (fewer); scope enforcement |
| `GET /connections/resources/instances/users` | GetInstanceUsers | 6 | Jinx sees Josephine for Sirius instance; same for Mattilsynet; non-existent instance returns empty; scope enforcement |

### AuthorizedPartiesController

| Endpoint | Test File | Tests | Scenarios |
|----------|-----------|-------|-----------|
| `GET /authorizedparties` | AuthorizedPartiesControllerTest | 16 | Malin with roles (DAGL); Thea with roles (PRIV, DAGL for Mille, Rightholder for Dumbo); Malin with packages (DAGL packages); Thea with packages (SalarySpecialCategory); Alex with roles+packages (LEDE + REVI via key-role); Josephine with instances (Kaos has 2 instances); Josephine without instances (empty); Josephine with resources (shows resource IDs); Josephine with resources+instances (instances not duplicated in resources); multiple flags; system scope; wrong scope; no token |

## Key Test Patterns

- **Scope enforcement**: Every endpoint tests that wrong scopes (from-others vs to-others, read vs write) return 403 Forbidden
- **401 Unauthorized**: Instance endpoints verify that missing auth tokens return 401
- **400 Bad Request**: Invalid instance URN format, empty right keys, non-existent resources
- **Bidirectional queries**: Tests verify both the "giving" perspective (to-others) and "receiving" perspective (from-others) where the endpoint supports it
- **Round-trip verification**: Add/Update tests verify the result by reading back via GET endpoints
- **XACML policy inspection**: Write tests parse the generated XACML delegation policy to verify subject (UUID for instances, partyId for resources), resource attributes (org, app), and action rules (one per right key)
- **Key-role inheritance**: GetResourceRights tests verify that Thea (DAGL of Mille) and Milena (LEDE of Mille) get indirect rights with KeyRole reason when Mille is a rightholder
- **Cascade behavior**: RemoveAssignment tests both non-cascade (fails with packages) and cascade (succeeds) deletion
- **Idempotent deletes**: Removing non-existent connections/instances returns 204 NoContent
- **Resource vs instance distinction**: AuthorizedParties tests verify that `includeResources` and `includeInstances` flags route delegation data to the correct response fields

## Mock Infrastructure

| Mock | Purpose |
|------|---------|
| `PermitPdpMock` | Always returns Permit for PDP decisions. Registered globally in ApiFixture so all authorization checks pass. |
| `PolicyRetrievalPointMock` | Loads XACML resource policies and delegation policies from test data files on disk (under `AccessMgmt.Tests/Data/`). |
| `PolicyRetrievalPointWithWrittenPoliciesMock` | Extends PolicyRetrievalPointMock to also check in-memory `PolicyFactoryMock.WrittenPolicies` before the file system. Used in tests that write delegation policies (Add/Update) and then read them back (GetInstanceRights) within the same test. |
| `PolicyFactoryMock` | Captures all written delegation policies in a `WrittenPolicies` ConcurrentDictionary keyed by policy path. Tests inspect this to verify XACML rule structure. |
| `Altinn2RightsClientMock` | Prevents real HTTP calls to Altinn 2 SBL Bridge (ClearReporteeRights). Required for any test that creates/removes assignments or packages. |
| `ResourceRegistryClientMock` | Provides resource registry policy lookups from test data. Required for endpoints that resolve resource identifiers. |
| `UserProfileLookupServiceMock` | Handles person lookup by SSN + last name for the PersonInput flow (AddRightholder, AddAssignmentPackage). |
| `AltinnRolesClientMock` | Provides Altinn 2 role definitions. Required for AuthorizedParties tests that include roles. |
| `ProfileClientMock` | Provides user profile data. Required for AuthorizedParties tests. |
| `DelegationChangeEventQueueMock` | Captures delegation change events. Used in RemoveResource tests. |
