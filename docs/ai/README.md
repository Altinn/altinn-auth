# Getting started with AI assistance in altinn-auth

> 🤖 Drafted with AI assistance (Claude Code). Reader unspecified.

**TL;DR**

- Using an AI assistant here is optional. If you do, the rules in [CONTRIBUTING.md](../../CONTRIBUTING.md#working-with-ai-assistance) apply to what you submit, whatever the tool.
- Every tool reads the same guidance: `AGENTS.md` at the root and in each vertical. You do not set up the repository per tool.
- Check the [data policy](data-policy.md) before you put anything into a tool. It is a draft until Digdir security accepts it; until then, rule 7 in CONTRIBUTING is the rule.
- Before you ask for review, run `just check <vertical>`, and `just check-full <vertical>` for what CI runs.
- Five read-only skills write team reports for Slack or Teams. See [`.agents/skills`](../../.agents/skills/README.md).

## 1. Read these first

| Read | What it gives you |
| --- | --- |
| [CONTRIBUTING.md, "Working with AI assistance"](../../CONTRIBUTING.md#working-with-ai-assistance) | The ten rules: you own what you submit, disclose substantial help, keep generated changes small, treat AI review as advice, protect data |
| [Data policy](data-policy.md) | What may go into a tool, which tools are approved, and what an agent may access |
| [`AGENTS.md`](../../AGENTS.md) and the vertical's own `AGENTS.md` | Commands, where things live, landmines and test gotchas |

The reasons behind the rules, with sources, are in [working-agreement-rationale.md](working-agreement-rationale.md).

## 2. How your tool finds the guidance

`AGENTS.md` is the single source ([ADR-0002](../adr/0002-tool-neutral-agent-contract.md)). Other files only point to it.

| Tool | Starts from |
| --- | --- |
| Claude Code | `CLAUDE.md`, which imports `AGENTS.md`. Skills through `.claude/skills/`, which point to `.agents/skills/`. |
| GitHub Copilot | `.github/copilot-instructions.md`, which summarises `AGENTS.md` and points to it. |
| Codex, Cursor, Gemini CLI and other tools that follow [agents.md](https://agents.md) | `AGENTS.md` directly. Skills in `.agents/skills/` for tools that support them. |

Tools differ in whether they load a vertical's `AGENTS.md` by themselves. When you work in a vertical, point your tool at that file.

## 3. Before you open a pull request

- Run `just check <vertical>` for build and unit tests, `just check-full <vertical>` for everything CI runs, and `just repo-check` for formatting and generated files. See [GETTING_STARTED](../testing/GETTING_STARTED.md).
- Write the description by the rules in `AGENTS.md`: a TL;DR, what you verified, and what the assistant did.
- Leave any `Co-authored-by` or `Assisted-by` trailer your tool adds.
- Copilot code review comments are advice. Answer or dismiss each one with a reason ([ADR-0003](../adr/0003-one-ai-review-bot.md)).

## 4. Reports for the team

| Skill | Shows |
| --- | --- |
| `ukesrapport` | What the team delivered this week, per component |
| `tavlestatus` | Work in progress on the Team Autorisasjon board, and where the board is out of date |
| `epicstatus` | Progress, blockers and pace per epic |
| `innkommende` | New issues, questions waiting for an answer, and bugs |
| `lukkekandidater` | Open issues that look done |

The skills only read from GitHub and never name people. How to run them is in [`.agents/skills/README.md`](../../.agents/skills/README.md).

## 5. When the assistant gets something wrong

Add what it got wrong to the relevant `AGENTS.md` in the same pull request, so the next person and the next tool avoid it (rule 10). A security suspicion goes privately through [SECURITY.md](../../SECURITY.md).

## Checked per tool

This page is the first part of the onboarding in [#4091](https://github.com/Altinn/altinn-auth/issues/4091). Section 2 should be confirmed by someone who uses each tool.

| Tool | Confirmed by |
| --- | --- |
| Claude Code | Not yet |
| GitHub Copilot | Not yet |
| Codex | Not yet |
| Cursor | Not yet |
