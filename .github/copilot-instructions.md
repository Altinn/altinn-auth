# Copilot instructions

The full guidance for this repository is in [`AGENTS.md`](../AGENTS.md). Read it first. This page is a summary, not a second source.

- Branch per change, never commit to `main`. Branch name `type/<issue>_<slug>`.
- The PR title is a Conventional Commit with the issue number, for example `fix(#4044): ...`. It becomes the squash commit that the release automation reads.
- Build with `dotnet build Altinn.Authorization.sln`, test with `dotnet test`, and use `dotnet test -- --filter-trait "Category=Unit"` for the unit lane.
- A green local test run is not proof: integration tests skip when no container runtime is running. CI is the gate.
- Every test must be marked `[UnitTest]` or `[IntegrationTest]`, or the run fails.
- `src/apps/Altinn.Register` is a placeholder and `src/apps/Altinn.ResourceRegistry` is an intentional build island. Do not "tidy" either.
- Documentation, `AGENTS.md` and ADRs change in the same PR as the behaviour they describe.
- [`CONTRIBUTING.md`](../CONTRIBUTING.md) is the working agreement, including the rules for AI assistance.
