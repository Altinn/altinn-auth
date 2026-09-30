# ADR-0003: One AI review bot, Copilot code review, advisory only

- **Status:** Accepted
- **Date:** 2026-09-30
- **Deciders:** The altinn-auth maintainers, `@altinn/team-access-management`, through review of the PR that added this ADR

## Context

The repositories being consolidated into this one ([#4049](https://github.com/Altinn/altinn-auth/issues/4049)) use three different AI review setups. This repository has a ruleset, *Copilot review for default branch*, that requests Copilot code review on every pull request to `main`. altinn-register has a three-line `.coderabbit.yaml`, and altinn-access-management-frontend a nineteen-line one. altinn-authentication has none. Once those repositories arrive, two bots on the same pull request would comment on the same lines, and nobody would know which one the team answers to.

There is now evidence from this repository to decide on. Across the nine pull requests in epic [#4075](https://github.com/Altinn/altinn-auth/issues/4075), Copilot code review raised 32 inline findings. 31 led to a change in the pull request and one to a change in the issue's scope. None was dismissed as wrong. They were mostly internal contradictions, claims in documentation that did not match the code, and promises a change did not keep.

Two limits on that evidence. All nine pull requests were documentation and configuration, written with AI assistance by one author, so the hit rate may not carry over to product code. And a different class of defect came from elsewhere: a separate AI-assisted review that ran the code found that a test filter did not exclude what it was documented to exclude, and that a path with spaces broke a command; human reviewers found a wrong issue reference and line-ending damage. The bot is one reviewer among several, not a gate.

The [data policy](../ai/data-policy.md) matters here too. It approves a tool on requirements, among them an agreement at organisation level and known retention. Copilot is already covered by the organisation's GitHub agreement. A second vendor would need its own assessment before it could see our code.

## Decision

1. **Copilot code review is the one AI review bot for this repository.** The existing ruleset stays as it is.
2. **It is advisory.** A bot never counts as the required approval and never requests changes. A person resolves or dismisses each comment with a reason, as rule 6 of the working agreement already says.
3. **Its instructions live in the repository, derived from `AGENTS.md`**, per [ADR-0002](0002-tool-neutral-agent-contract.md). `.github/copilot-instructions.md` already exists and points to `AGENTS.md`. Path-scoped instructions are added only when a vertical needs a rule the bot keeps missing, and are then derived from that vertical's `AGENTS.md`, never the other way round.
4. **Repositories moved in drop their bot configuration.** The `.coderabbit.yaml` files in altinn-register and altinn-access-management-frontend are removed as part of their move, through the checklist in [#4080](https://github.com/Altinn/altinn-auth/issues/4080).

### Alternatives considered

- **CodeRabbit.** Richer configuration, including path instructions, and already in use in two of the repositories moving in. Rejected for now: it is a second vendor that would need approval under the data policy, and a second configuration to keep consistent with `AGENTS.md`, for a capability the evidence above does not show we are missing.
- **Both bots.** Duplicate comments on the same lines and two vendors. Rejected.
- **No bot.** Would have removed a reviewer that found real defects in almost every pull request above. Rejected.

## Consequences

- No ruleset change is needed. Switching bots later is a configuration change, because the instructions live in the repository rather than in the bot.
- Whether Copilot code review reads the nested `AGENTS.md` files for a vertical, or only `.github/copilot-instructions.md`, has not been verified. Watch the first reviews on a vertical-specific change; if the bot misses that vertical's rules, that is the trigger for path-scoped instructions under decision 3.
- The evidence should be revisited once the bot has reviewed product code, not only documentation. If its findings there are mostly noise, this decision should be superseded.

## References

- Issue [#4085](https://github.com/Altinn/altinn-auth/issues/4085), part of epic [#4075](https://github.com/Altinn/altinn-auth/issues/4075).
- Working agreement, rule 6: [CONTRIBUTING.md](../../CONTRIBUTING.md).
- Data policy, section 2: [data-policy.md](../ai/data-policy.md).
- Pull requests the evidence comes from: #4093, #4094, #4267, #4270, #4271, #4272, #4273, #4275, #4291.
