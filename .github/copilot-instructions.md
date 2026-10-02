# Copilot instructions

The full guidance for this repository is in [`AGENTS.md`](../AGENTS.md). Read it first. This page is a summary, not a second source.

- Branch per change, never commit to `main`. Branch name `type/<issue>_<slug>`.
- The PR title is a Conventional Commit with the issue number, for example `fix(#4044): ...`. It becomes the squash commit that the release automation reads.
- Build with `dotnet build Altinn.Authorization.sln` and test with `dotnet test`. The unit lane is `--filter-trait "Category=Unit"` after `--`, run per vertical solution.
- A green local test run is not proof: integration tests skip when no container runtime is running. CI is the gate.
- Every test must be marked `[UnitTest]` or `[IntegrationTest]`, or the run fails. This applies to the xUnit v3 verticals. `src/apps/Altinn.ResourceRegistry` is an xUnit v2 island: no categories, the trait filter does not exclude its tests, and they start a real database.
- `src/apps/Altinn.Register` is a placeholder and `src/apps/Altinn.ResourceRegistry` is an intentional build island. Do not "tidy" either.
- Update affected documentation and agent guidance in the same PR. Require an ADR only for significant architectural decisions or lasting trade-offs; follow the [ADR threshold](../CONTRIBUTING.md#when-an-adr-is-required). Routine API/model extensions and bug fixes do not automatically require one.
- [`CONTRIBUTING.md`](../CONTRIBUTING.md) is the working agreement, including the rules for AI assistance.
