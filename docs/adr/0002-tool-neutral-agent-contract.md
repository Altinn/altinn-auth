# ADR-0002: Tool-neutral agent contract: `AGENTS.md` as the single source

- **Status:** Accepted
- **Date:** 2026-09-18
- **Deciders:** Team Autorisasjon, through review of the PR that added this ADR

## Context

AI coding agents are already part of how this domain is built. In 2026 so far, 64 of 840 commits in altinn-auth carry an AI co-author trailer, from 11 of the 17 human authors; altinn-authentication is at 55 of 177. Adoption is uneven across people and repositories, and the team ranges from daily users to people who do not use these tools at all.

Every tool reads its own instruction file: `CLAUDE.md` (Claude Code), `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` (GitHub Copilot), `.cursor/rules` (Cursor), `GEMINI.md` (Gemini CLI), and so on. Left alone, a repository grows several of these with diverging content, and the content ends up written in one tool's dialect. [AGENTS.md](https://agents.md/) is an open, tool-agnostic convention for the same purpose. It is read natively by Codex, Cursor, GitHub Copilot coding agent, Gemini CLI, Jules and others, and Claude Code reads it through a one-line import.

The consolidation tracked in [#4048](https://github.com/Altinn/altinn-auth/issues/4048) will bring altinn-register, altinn-resource-registry, altinn-authentication and the Access Management frontend into this repository as verticals. Whatever convention exists here when they arrive is the one they inherit. Today:

- altinn-authentication has a mature [`AGENTS.md`](https://github.com/Altinn/altinn-authentication/blob/main/AGENTS.md) (commands, workflow rules, landmines, test gotchas) and a [`CLAUDE.md`](https://github.com/Altinn/altinn-authentication/blob/main/CLAUDE.md) that only imports it.
- altinn-auth has no root agent guidance. One vertical, `src/tools/Altinn.AccessMgmt.FFB`, has its own `CLAUDE.md`.
- Three different AI review configurations exist across the repositories: a Copilot review ruleset here, and two differently configured `.coderabbit.yaml` files elsewhere.

Forces at play: guidance must work for every tool, including next year's; it must survive moving a repository into a vertical without a central rewrite; the landmines in PDP evaluation and delegation logic are vertical-specific and must be owned by the people who know them; and nothing in the guidance may lock the team to a vendor.

## Decision

1. **`AGENTS.md` is the single source of agent guidance.** It is the only file with content. It follows a fixed section order so humans and agents find things in the same place: *What this is*, *Commands*, *Critical workflow rules*, *Where things live*, *Landmines*, *Test gotchas*, *Known tech debt*. It is short and high-signal: the root file stays under 150 lines, and detail is linked, not inlined.

2. **Tool-specific files are pointers only.**
   - `CLAUDE.md` contains `@AGENTS.md` and at most two further lines.
   - `.github/copilot-instructions.md` contains a summary of at most 20 lines and a link to `AGENTS.md`, because Copilot does not import files.
   - Tools that read `AGENTS.md` natively (Codex, Cursor, Copilot coding agent, Gemini CLI via its `context.fileName` setting) get no file of their own. `.cursorrules`, `GEMINI.md` and similar files are not checked in.
   - Path-scoped instructions for a review bot are derived from the relevant `AGENTS.md`, never the other way round ([#4085](https://github.com/Altinn/altinn-auth/issues/4085)).

3. **One file per vertical, plus a root file.** The root `AGENTS.md` covers what is common: repository layout, the vertical convention, repo-wide commands, CI facts, workflow rules, and a table that points to every vertical's file. Each app vertical has `src/apps/<Vertical>/AGENTS.md` with a `CLAUDE.md` pointer next to it, holding that vertical's landmines, test gotchas and ADR pointers. Verticals under `src/pkgs`, `src/libs` and `src/tools` get one where it adds value. A repository moved in under #4048 brings its own `AGENTS.md` and gets one row in the root table; nothing else changes centrally.

4. **Ownership.** `AGENTS.md` files are covered by `.github/CODEOWNERS` for their vertical. Whoever finds a landmine updates the file in the same PR.

5. **What does not belong in `AGENTS.md`:** tool-specific syntax or features, secrets or environment-specific values, long explanations (those go in `docs/` or an ADR and are linked), and anything that changes more often than monthly.

6. **Decisions are recorded, not repeated.** The *why* behind a landmine lives in an ADR in the vertical's `docs/adr/`, per [ADR-0001](0001-record-architecture-decisions.md). `AGENTS.md` links to it with "read ADR-XXXX before changing Y".

### Alternatives considered

- **One complete file per tool.** Rejected: content drifts, and every landmine has to be written three times.
- **A single root file for the whole monorepo.** Rejected: it would exceed any tool's useful context, and a moved-in repository would have to merge its guidance into a shared file.
- **Generate the tool files from `AGENTS.md` in CI.** Not needed while the pointers are one line each. Revisit if a tool without import or native support becomes important.

## Consequences

- A developer or an agent can clone the repository, point any tool at it and get the same commands, rules and landmines, with no verbal handover.
- The convention costs one short file per vertical, and CODEOWNERS review on changes to it. The CI guard in [#4081](https://github.com/Altinn/altinn-auth/issues/4081) fails a PR that adds an app vertical without `AGENTS.md` or breaks a link in one.
- The root file ([#4077](https://github.com/Altinn/altinn-auth/issues/4077)) and the first two vertical files ([#4078](https://github.com/Altinn/altinn-auth/issues/4078)) are follow-ups. The moves in #4048 carry their files in through the checklist in [#4080](https://github.com/Altinn/altinn-auth/issues/4080).
- Switching or adding a tool is a pointer change, not a content change. Switching review bot is a configuration change ([#4085](https://github.com/Altinn/altinn-auth/issues/4085)).
- Nested `CLAUDE.md` pointers are needed because Claude Code loads guidance per directory. That is the one tool-specific artefact per vertical, and it is two lines.
- Using an agent remains optional. The contract raises the floor for those who do, and changes nothing for those who don't: the same build, tests and review apply.

## References

- Issue [#4076](https://github.com/Altinn/altinn-auth/issues/4076), part of epic [#4075](https://github.com/Altinn/altinn-auth/issues/4075).
- The AGENTS.md convention: <https://agents.md/>
- Reference implementation: altinn-authentication [`AGENTS.md`](https://github.com/Altinn/altinn-authentication/blob/main/AGENTS.md) and [`CLAUDE.md`](https://github.com/Altinn/altinn-authentication/blob/main/CLAUDE.md).
- Repository consolidation: [#4048](https://github.com/Altinn/altinn-auth/issues/4048).
