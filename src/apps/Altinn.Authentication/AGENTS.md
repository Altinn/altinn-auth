# AGENTS.md — Altinn.Authentication

Operational guide for AI agents working in this vertical, imported from altinn-authentication with its full history (#4058). Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md). Humans: see [`README.md`](README.md) and [`docs/`](docs/README.md). Keep this file **short and high-signal** — it is loaded into limited context.

## What this is

Altinn Platform Authentication — an ASP.NET Core (.NET 10) service that authenticates users/orgs/systems and issues Altinn JWTs, and acts as a small OIDC authorization server for browser sign-in. Read [`docs/architecture.md`](docs/architecture.md) first.

## Commands

```bash
# Build
dotnet build src/apps/Altinn.Authentication/Altinn.Authentication.slnx

# Test (a container runtime MUST be running — Testcontainers PostgreSQL).
# Podman works as a drop-in for Docker; if it fails to connect, try `podman machine start`.
dotnet test src/apps/Altinn.Authentication/test/Altinn.Platform.Authentication.Tests/Altinn.Platform.Authentication.Tests.csproj

# Compile-only check of one project (fast)
dotnet build src/apps/Altinn.Authentication/src/Altinn.Platform.Authentication/Altinn.Platform.Authentication.csproj -clp:ErrorsOnly --nologo
```

## Critical workflow rules

- **A green local `dotnet build` is NOT sufficient.** The integration tests need a container runtime (Docker *or* Podman — see [development.md](docs/development.md#container-runtime)), and compilation passing says nothing about them. The authoritative gate is the CI **Build and Test** job. Before concluding you can't run them locally, check `podman machine list` / `docker info`; a stopped Podman machine looks exactly like "no container runtime installed". If you genuinely can't run them, push and watch CI before claiming done.
- **Branch per change; never commit to `main`.** Commit/push only when asked.
- Update the relevant [`docs/`](docs/README.md) page or ADR **in the same PR** as a behaviour change.
- Respond to CodeRabbit/CodeQL review comments; don't silently ignore them.

## Where things live

- `src/apps/Altinn.Authentication/src/Altinn.Platform.Authentication` — host, controllers, app services, config. `src/apps/Altinn.Authentication/src/Altinn.Platform.Authentication.Core` — models/interfaces/pure helpers (no I/O). `src/apps/Altinn.Authentication/src/Altinn.Platform.Authentication.Integration` — outbound clients (Register/`PartiesClient`, Access Management, Profile). `src/apps/Altinn.Authentication/src/Altinn.Platform.Authentication.Persistance` — Postgres + SQL migrations. `src/pkgs/JWTCookieAuthentication` — shared JWT-cookie library.
- Auth flows are documented in [`docs/flows/`](docs/flows/). The **why** behind the design is in [`docs/adr/`](docs/adr/) — **read the relevant ADR before changing an auth flow.**

## Landmines — do NOT "fix" these without understanding

- **`AuthenticateUser` no longer branches on `EnableOidc`/`ForceOidc`/`AuthorizationServerEnabled`** — those legacy browser-sign-in branches were collapsed in Altinn/altinn-authentication#2071 ([ADR-0002](docs/adr/0002-authorization-server-is-the-live-auth-path.md)); the authorization-server flow is the only live browser path. Don't reintroduce branching on these in `AuthenticateUser`. The settings still exist (checked-in value `false` — **not** prod), with differing real usage:
  - `ForceOidc` — still read in `OidcServerService` (session logic).
  - `AuthorizationServerEnabled` — still read in `LogoutController`, which still has a legacy branch (dead only if prod sets it `true`; a collapse candidate like Altinn/altinn-authentication#2071).
  - `EnableOidc` — now read **nowhere**; a dead property / safe-removal candidate.
- **`IsSafeSameOrSubdomainHttps`** (`AuthenticationController`) is an intentional open-redirect guard. The CodeQL "URL redirection" alerts on the `goTo`/upstream redirects are **dismissed false positives** — don't rewrite the redirects to appease a scanner.
- **`IsValidIssuer`** is *misnamed* (returns `true` for an invalid issuer) but its control flow is **correct**. Fix the name/substring-match if asked, but do not "invert" the logic — that would break it.
- **`SblAuthCookie*` names + the legacy-cookie delete logic are intentionally kept** to drain stale Altinn 2 cookies, even though the rest of the SBL integration is gone ([ADR-0004](docs/adr/0004-sbl-bridge-altinn2-decommission.md)).
- Token-validation in the exchange path disables issuer/audience checks — flagged for review in [#2074](https://github.com/Altinn/altinn-authentication/issues/2074); **verify intent against tests before changing.**

- **This vertical is a build island**, like `src/pkgs/JWTCookieAuthentication` that it depends on: its own `Directory.Build.props`/`.targets`, `Directory.Packages.props` with central package management off (versions are inline in the csproj files), `Altinn3.ruleset`, and `stylecop.json`. It inherits the repository's `.editorconfig` settings. Do not merge it into the shared build files.
- **Existing compiler warnings are a baseline, not errors** (`WarningsNotAsErrors` in `Directory.Build.props`), because CI builds with `-warnaserror`. Do not add new warnings and do not extend the list; [#4597](https://github.com/Altinn/altinn-auth/issues/4597) removes it. The list applies to every project in the vertical and exempts its codes from `-warnaserror`, so a project that reaches zero warnings opts out by resetting `WarningsNotAsErrors` to the NuGet audit codes in its csproj (as `Altinn.Platform.Authentication.Core` does). CI's `-warnaserror` then fails it on any new warning; it needs no `TreatWarningsAsErrors`.

## Test gotchas

- Build Register contract types (`Altinn.Register.Contracts`) via JSON deserialization in tests. A `PartyUser` with `userId` set **must** also set `userIds` (e.g. `"userIds": [ <id> ]`) or its constructor throws at runtime.
- Mock outbound clients with a mocked `HttpMessageHandler` (see `AccessManagementClientTests`); unit-test pure logic directly (see `AuthenticationHelperTests`).

## Known tech debt

A prioritised top-30 lives in [#2074](https://github.com/Altinn/altinn-authentication/issues/2074). Open follow-ups: [#2072](https://github.com/Altinn/altinn-authentication/issues/2072).
