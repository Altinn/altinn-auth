# Step 34 — Scope CI coverage threshold enforcement to the owning vertical

## Goal

Fix CI failures in three verticals (`app: Authorization`, `lib: Integration`,
`pkg: PEP`) caused by the global `docs/testing/coverage-thresholds.json` being
enforced against **every** assembly that shows up in the Cobertura XML — including
transitively referenced assemblies from *other* verticals whose own code is
barely exercised by the current vertical's tests.

## Root cause

`dotnet-coverage` emits a `<package>` row for every loaded assembly, not just
the ones whose source lives in the current vertical. So e.g. running the
`pkg: PEP` vertical produced a row for `Altinn.Authorization.ABAC` at 0% line
coverage, and the global threshold (`ABAC: 60`) tripped even though ABAC is
enforced — and passing — in its own `pkg: ABAC` vertical.

Concretely, the three verticals tripped on:

| Vertical | Offending assembly | Measured | Enforced floor |
|---|---|---|---|
| `pkg: PEP` | `Altinn.Authorization.ABAC` (ref) | 0% | 60% |
| `app: Authorization` | `Altinn.Authorization.PEP` (ref) | 7.68% | 75% |
| `lib: Integration` | referenced libs (same leakage pattern) | ~0% | — |

## Change

`docs/testing/run-coverage.ps1`:

- New `-OwnedRoot` parameter (defaults to `Get-Location`, which is already the
  vertical root in `tpl-vertical-ci.yml`'s `working-directory`).
- New `Test-IsOwnedByVertical` helper that inspects `<class filename="...">`
  entries in the Cobertura XML and returns `$true` only when at least one class
  file lives under `$OwnedRoot`.
- Both the fatal (`assemblies`) and warn-only (`warnings`) threshold checks now
  skip non-owned assemblies. Non-owned rows are still printed for visibility,
  suffixed with ` (ref)`.

The global `docs/testing/coverage-thresholds.json` is unchanged and remains the
single source of truth. The per-vertical `$localThresholds` override mechanism
in `tpl-vertical-ci.yml` is still supported — it's just no longer required as a
workaround for cross-vertical leakage.

## Verification

Ran `run-coverage.ps1` locally against each failing vertical with the unchanged
global thresholds file:

| Vertical | Result | Exit |
|---|---|---|
| `pkg: Altinn.Authorization.PEP` | `All enforced assemblies meet their coverage thresholds.` — PEP 78.99% line; ABAC shown as `(ref)` and skipped | 0 |
| `app: Altinn.Authorization` | `All enforced assemblies meet their coverage thresholds.` — Altinn.Authorization 69.19% line; PEP/ABAC/ProblemDetails/Urn/Swashbuckle shown as `(ref)` and skipped | 0 (threshold stage) |
| `lib: Altinn.Authorization.Integration` | `All enforced assemblies meet their coverage thresholds.` — Integration.Platform 35.19% line; Register.Contracts/ModelUtils/Urn/ProblemDetails shown as `(ref)` and skipped | 0 |

No coverage numbers changed — only the set of assemblies the script chooses to
enforce per vertical.

## Deferred / not in scope

- The `Altinn.Urn` assembly and other shared libs still show low coverage when
  they leak into a vertical's run. That's cosmetic now (`(ref)` marker) and
  will stop appearing once each of those libs owns its own enforced floor in
  its own vertical.
- `PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_PermitWithActionFilterMatch`
  was observed to fail locally when the full `ResourceRegistry_DecisionTests`
  class runs but pass when run in isolation — a fixture-ordering / shared-state
  issue. Out of scope for this step; not reproduced by CI in the PR that
  triggered this fix.
