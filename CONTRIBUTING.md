# Contributing to Altinn Authorization

This guide applies to everyone who changes this repository, whether you type every line yourself or work with an AI coding assistant. The bar for a contribution is the same either way, and so is the review.

## Before you start

- Every change starts from an issue. Reference it in the branch name and the PR title.
- Read the [README](README.md) and the docs of the vertical you are changing. Agent guidance lives in `AGENTS.md` at the root and in each vertical.
- Integration tests need a running container runtime. A green local build is not proof; CI is the gate.

## Branches, commits and pull requests

- Branch from `main` as `type/<issue>_<short-slug>`, for example `fix/4044_remove_all_client_delegations`.
- The PR title is a [Conventional Commit](https://www.conventionalcommits.org/) with the issue number: `fix(#4044): remove every delegation when a client is removed`. `main` is squash-only, so the PR title becomes the commit message that release-please reads. `feat` and `fix` on a package under `src/pkgs` bump its version.
- Keep PRs small: aim for under **400 changed lines**, not counting generated code, test data and lock files. Split larger work, and agree the split with a reviewer before you start.
- One approving review from a person and green required checks are needed to merge. Open a draft PR early if you want feedback.
- Documentation, `AGENTS.md` and ADRs are updated in the same PR as the behaviour they describe. Write an ADR in `docs/adr/` (cross-cutting) or the vertical's `docs/adr/` when a change alters an authorization flow or policy evaluation, a public API contract, a data model, a dependency between verticals, or a repository convention.
- Tests are production code: reviewed with the same care, and never deleted or weakened without a reason a reviewer has agreed to. See the [testing guide](docs/testing/README.md). Say in the PR what you could not test locally.

## Working with AI assistance

Using an AI assistant is a personal choice. Nobody is required to use one, and nobody is criticised for using or not using one. The rules below are not optional; they are what makes the choice safe for everyone. The reasoning behind them, with sources, is in [docs/ai/working-agreement-rationale.md](docs/ai/working-agreement-rationale.md).

1. **You own what you submit.** Whatever produced the code, you understand it and can explain every line in review, in your own words. "The tool wrote it" is not an answer.
2. **Disclose substantial assistance.** If an assistant generated or substantially rewrote code, tests or documentation, say so in the PR description: what it did and how you verified it. Autocomplete, grammar fixes and answering your questions need no disclosure. If your tool adds a `Co-authored-by` or `Assisted-by` trailer, leave it in. An ADR, security assessment or other decision record written with substantial assistance says so in the document and names the model.
3. **Write the narrative yourself.** PR descriptions, commit messages and replies to reviewers come from you. If you quote an assistant in a discussion, put it in a quote block and add your own comment.
4. **Review-ready means self-reviewed and green.** Before you ask for review, read the whole diff, build it, run the tests, and fix what the tool got wrong. The reviewer's time is the scarcest resource in the team; never spend it on output you have not read.
5. **Keep generated changes small.** A large change produced with an assistant is agreed with a reviewer before it is started, and split so each PR can be understood in one sitting.
6. **AI review is advisory.** Comments from a review bot are addressed or dismissed with a reason, by a person. A bot never counts as the required approval.
7. **Protect data.** Never put secrets, API keys, tokens, environment files, personal data, production logs or telemetry, incident details, or material that is taushetsbelagt or unntatt offentlighet into a prompt, and keep such files where an assistant cannot read them. Use only tools the team has approved. An agent gets the least access the task needs and never production credentials; a person starts CI on and merges an agent-opened PR. The data policy in `docs/ai/` says what may go where.
8. **Verify before you report.** A bug or vulnerability an assistant found is reproduced and understood by you before it becomes an issue, and the issue says it was found with assistance.
9. **Know the rules before you use a tool.** Read this section and the data policy, and complete the team's short onboarding. The team keeps a record of who has done it.
10. **Leave the map better than you found it.** When you discover something an assistant or a new colleague would get wrong, add it to the relevant `AGENTS.md` in the same PR.

## Reviewing

- Review the change, not the tool. Ask the author to explain anything you cannot follow; a good answer is part of the contribution.
- A small, focused PR gets a faster and better review. Ask for a split when a PR is too big to hold in your head.
- Approve only what you would be comfortable maintaining yourself.

## Security and licence

Report vulnerabilities as described in [SECURITY.md](SECURITY.md), never in a public issue. Contributions are licensed under the [MIT License](LICENSE); make sure any tool you use lets you license its output that way.
