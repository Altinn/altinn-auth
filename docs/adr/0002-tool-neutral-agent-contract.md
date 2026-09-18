# ADR-0002: Tool-neutral agent contract: `AGENTS.md` as the single source

- **Status:** Accepted
- **Date:** 2026-09-18
- **Deciders:** The altinn-auth maintainers, `@altinn/team-access-management`, through review of the PR that added this ADR

## Context

AI coding agents are already part of how this domain is built. In 2026 so far, 64 of 840 commits in altinn-auth carry an AI co-author trailer, from 11 of the 17 human authors; altinn-authentication is at 55 of 177. Adoption is uneven across people and repositories, and the team ranges from daily users to people who do not use these tools at all.

Every tool reads its own instruction file: `CLAUDE.md` (Claude Code), `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` (GitHub Copilot), `.cursor/rules` (Cursor), `GEMINI.md` (Gemini CLI), and so on. Left alone, a repository grows several of these with diverging content, and the content ends up written in one tool's dialect. [AGENTS.md](https://agents.md/) is an open, tool-agnostic convention for the same purpose. It is read natively by Codex, Cursor, GitHub Copilot coding agent, Gemini CLI, Jules and others, and Claude Code reads it through a one-line import. Procedural knowledge that is only needed for a specific task has its own open convention, [Agent Skills](https://agentskills.io/): a folder with a `SKILL.md` that agents load on demand.

The consolidation decided in [#4049](https://github.com/Altinn/altinn-auth/issues/4049) will bring altinn-register, altinn-resource-registry, altinn-authentication and the Access Management frontend into this repository as verticals ([#4056](https://github.com/Altinn/altinn-auth/issues/4056) to [#4059](https://github.com/Altinn/altinn-auth/issues/4059)). Whatever convention exists here when they arrive is the one they inherit. Today:

- altinn-authentication has a mature [`AGENTS.md`](https://github.com/Altinn/altinn-authentication/blob/main/AGENTS.md) of 52 lines (commands, workflow rules, landmines, test gotchas) and a [`CLAUDE.md`](https://github.com/Altinn/altinn-authentication/blob/main/CLAUDE.md) that imports it and adds three lines of pointers.
- altinn-auth has no root agent guidance. One vertical, `src/tools/Altinn.AccessMgmt.FFB`, has a 43-line `CLAUDE.md` with full content.
- Three different AI review configurations exist across the repositories: a Copilot review ruleset here, and two differently configured `.coderabbit.yaml` files elsewhere.
- Most of the team develops on Windows, where Git checks out symbolic links as plain text files unless `core.symlinks` is enabled, which the default Git for Windows installation leaves off.

Forces at play: guidance must work for every tool, including next year's; it must survive moving a repository into a vertical without a central rewrite; the landmines in PDP evaluation and delegation logic are vertical-specific and must be owned by the people who know them; a developer working in one vertical should not have to load another vertical's guidance; and nothing in the guidance may lock the team to a vendor.

## Decision

1. **`AGENTS.md` is the single authoritative source of agent guidance.** Every other instruction file either imports it or summarises it and links to it; none carries guidance of its own. It follows a fixed section order so humans and agents find things in the same place: *What this is*, *Commands*, *Critical workflow rules*, *Where things live*, *Landmines*, *Test gotchas*, *Known tech debt*. It is short and high-signal: the root file stays under 100 lines, and detail is linked, not inlined.

2. **Tool-specific files are pointers only.**
   - A tool that imports files gets a pointer file containing the import line and at most five further lines that name where the docs and ADRs are. For Claude Code that file is `CLAUDE.md` with `@AGENTS.md`.
   - A tool that neither imports nor reads `AGENTS.md` gets a summary of at most 20 lines and a link. Today that is `.github/copilot-instructions.md`.
   - A tool that reads `AGENTS.md` natively gets no file of its own, and its tool-specific file is not checked in.
   - Path-scoped instructions for a review bot are derived from the relevant `AGENTS.md`, never the other way round ([#4085](https://github.com/Altinn/altinn-auth/issues/4085)).

3. **One file per vertical, plus a root file.** The root `AGENTS.md` covers what is common: repository layout, the vertical convention, repo-wide commands, CI facts, workflow rules, and a table that points to every vertical's file. Each app vertical has `src/apps/<Vertical>/AGENTS.md` with its pointer file next to it, holding that vertical's landmines, test gotchas and ADR pointers. Verticals under `src/pkgs`, `src/libs` and `src/tools` get one where it adds value. Tools load the nearest file in addition to the root file, so a developer in Register never loads Access Management's landmines. A repository moved in under #4056 to #4059 brings its own `AGENTS.md` and gets one row in the root table; nothing else changes centrally.

4. **Ownership.** `AGENTS.md` files are owned by their vertical's owners through `.github/CODEOWNERS`. Today CODEOWNERS covers only itself; the entries are added together with the files in [#4077](https://github.com/Altinn/altinn-auth/issues/4077) and [#4078](https://github.com/Altinn/altinn-auth/issues/4078), and [#4081](https://github.com/Altinn/altinn-auth/issues/4081) checks that every `AGENTS.md` is covered. Whoever finds a landmine updates the file in the same PR.

5. **What does not belong in `AGENTS.md`:** tool-specific syntax or features, secrets or environment-specific values, long explanations (those go in `docs/` or an ADR and are linked), procedures needed only for one task (those go in a skill, see 6), and anything that changes more often than monthly.

6. **Task-specific procedures are skills.** How to run `repoctl`, how to seed test data, how to cut a package release: each is a folder with a `SKILL.md` in the Agent Skills format, loaded by the agent when it needs it and linked from `AGENTS.md`. Where the skill folders live is settled with the first skill in #4077, so that the same folder serves every tool that supports the format.

7. **Decisions are recorded, not repeated.** The *why* behind a landmine lives in an ADR in the vertical's `docs/adr/`, per [ADR-0001](0001-record-architecture-decisions.md). `AGENTS.md` links to it with "read ADR-XXXX before changing Y".

### Alternatives considered

- **One complete file per tool.** Rejected: content drifts, and every landmine has to be written three times.
- **A single root file for the whole monorepo.** Rejected: it would exceed any tool's useful context, and a moved-in repository would have to merge its guidance into a shared file.
- **Symbolic links instead of pointer files.** Attractive because the operating system, not the agent, resolves the indirection. Rejected for now because of Windows: with `core.symlinks` off, `CLAUDE.md` checks out as a text file containing the string `AGENTS.md`, which imports nothing, and the failure is silent. Revisit when every developer machine has `core.symlinks` on, or for a tool that resolves links server-side.
- **Generate the tool files from `AGENTS.md` in CI.** Not needed while the pointers are a few lines each. Revisit if a tool without import or native support becomes important.

## Consequences

- A developer or an agent can clone the repository, point any tool at it and get the same commands, rules and landmines, with no verbal handover.
- The convention costs one short file per vertical, and CODEOWNERS review on changes to it. The CI guard in [#4081](https://github.com/Altinn/altinn-auth/issues/4081) fails a PR that adds an app vertical without `AGENTS.md`, breaks a link in one, or leaves one outside CODEOWNERS.
- The root file ([#4077](https://github.com/Altinn/altinn-auth/issues/4077)) and the first two vertical files ([#4078](https://github.com/Altinn/altinn-auth/issues/4078)) are follow-ups. Until #4077 lands, the existing `src/tools/Altinn.AccessMgmt.FFB/CLAUDE.md` is the known exception to decision 2; #4077 turns it into an `AGENTS.md` with a pointer next to it. The moves in #4056 to #4059 carry their files in through the checklist in [#4080](https://github.com/Altinn/altinn-auth/issues/4080), and the altinn-authentication pointer file is trimmed to the five-line limit on the way in.
- Switching or adding a tool is a pointer change, not a content change. Switching review bot is a configuration change ([#4085](https://github.com/Altinn/altinn-auth/issues/4085)).
- Nested pointer files are needed because Claude Code loads guidance per directory. That is the one tool-specific artefact per vertical, and it is a few lines.
- Using an agent remains optional. The contract raises the floor for those who do, and changes nothing for those who don't: the same build, tests and review apply.

## References

- Issue [#4076](https://github.com/Altinn/altinn-auth/issues/4076), part of epic [#4075](https://github.com/Altinn/altinn-auth/issues/4075).
- The AGENTS.md convention: <https://agents.md/>. The Agent Skills format: <https://agentskills.io/>.
- Reference implementation: altinn-authentication [`AGENTS.md`](https://github.com/Altinn/altinn-authentication/blob/main/AGENTS.md) and [`CLAUDE.md`](https://github.com/Altinn/altinn-authentication/blob/main/CLAUDE.md).
- Repository consolidation: decision in [#4049](https://github.com/Altinn/altinn-auth/issues/4049), moves in [#4056](https://github.com/Altinn/altinn-auth/issues/4056) to [#4059](https://github.com/Altinn/altinn-auth/issues/4059).
