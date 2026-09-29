# Data policy for AI tools

> **Status: draft.** This page is written by the team and is not in force until security in Digdir has reviewed and accepted it. Until then, rule 7 of [CONTRIBUTING.md](../../CONTRIBUTING.md) is the rule, and a tool nobody on the team already uses is cleared with the team lead first. Open questions for that review are listed at the end.

**TL;DR**

- **Never**, under any circumstance: secrets and tokens, real personal data, and anything gradert, taushetsbelagt or unntatt offentlighet.
- **Not without a written assessment first:** production logs and telemetry, incident data, unpublished security findings, and data from a service owner.
- Source code in our public repositories and synthetic test data are fine, in an approved tool.
- **Only approved tools**, whatever the material. The approved list is in section 2.
- A tool is approved on requirements, not on brand: no training on our input, known retention, processing in the EEA, an agreement at organisation level.
- An agent gets the least access the task needs, never production credentials, and never a token it does not need.
- If something went in by accident, say so the same day. The list of who to tell is at the bottom.

## 1. What may go where

The classes follow the tiers Digdir and DFØ use for cloud services, applied to the material this team actually touches. **Everything below assumes an approved tool from section 2.** An unapproved tool is not a question of class; it is simply not used for work.

| Class | Examples from our work | Rule |
| --- | --- | --- |
| **Open** | Source code in our public repositories, public documentation, published ADRs, synthetic test data (Tenor, TT02 test users), public API contracts | Use freely |
| **Internal** | Unpublished design notes, internal architecture sketches, non-sensitive configuration, issue and PR text that is not public | Use, and keep it out of anything that leaves the approved tool |
| **Sensitive** | Anything about a named person, data from a service owner, unpublished security findings, incident timelines, access logs, production telemetry | Only after a written assessment for that specific use, recorded with the team lead |
| **Prohibited** | Secrets and API keys, tokens, `.env` files, connection strings, certificates, real personal data, gradert information, anything taushetsbelagt or unntatt offentlighet | Never, in any tool, under any agreement |

Two things that catch people out:

- **An assistant reads what you can read.** It is not enough to avoid pasting a secret: a tool with access to your working directory can read `.env`, local dumps and downloaded logs. Keep that material outside the checkout, or outside the folder the tool is pointed at. Digdir's own guidance makes this point specifically about code assistants, because they cannot reliably exclude individual files.
- **Test data is only green when it is synthetic.** A database dump taken from a test environment that was seeded from production is not synthetic.

## 2. What makes a tool approved

A tool is judged on what it does with our input, not on who sells it. All of these must hold:

1. **No training on our input.** Stated in the agreement or the product's terms, not merely a setting.
2. **Known retention.** We know whether prompts and responses are stored, and for how long. Zero retention is preferred.
3. **Processing within the EEA**, verified and configured, not assumed from the vendor's headquarters.
4. **An agreement at organisation level**, not a personal or free-tier account.
5. **A data processing agreement** where personal data can be processed at all.
6. **A named owner on our side** who re-checks the above when the product changes.

A cloud framework agreement covers the cloud provider, not necessarily the application vendor. Check the vendor you are actually sending text to.

### Approved tools

| Tool | Approved for | Agreement and retention | Owner | Reviewed |
| --- | --- | --- | --- | --- |
| _(to be filled in with security)_ | | | | |

This table is the operative list. A tool that is not in it is not approved, however good it looks.

## 3. MCP servers and connected data

An MCP server extends an assistant's reach, so it is governed by the same classes as section 1.

- **Allowed:** servers that reach only what the developer may already reach and that carry no sensitive data, such as a fine-grained GitHub token scoped to the repositories you work in, or a local development database whose contents are synthetic. A local database is not automatically synthetic: one seeded from a production dump is sensitive, and a server pointed at it is not allowed.
- **Not allowed:** anything pointed at production, at a database with real personal data, or at an internal system that holds sensitive material.
- **New servers are added to the list below before use.** A server installed from a registry is still a third party running code on your machine.

| MCP server | Reaches | Approved by | Date |
| --- | --- | --- | --- |
| _(to be filled in)_ | | | |

## 4. Agents and access

- **An agent inherits the identity it runs under.** Give it a fine-grained token limited to the repositories and permissions the task needs. Never an admin token, never a token that can reach production.
- **No production credentials in an agent's environment**, including through a local `.env` or a logged-in CLI session.
- **A pull request opened under an agent's own identity runs CI only after a person approves it, and is merged by a person.** The hosting platform enforces this for its own coding agent. Any other agent identity needs its own decision, recorded here.
- **Prefer an isolated environment** for agents that run commands, so a mistake cannot reach your credentials or the rest of your machine. Other public-sector teams in Norway run their agents in an OS-level sandbox that blocks SSH keys, cloud credentials and `.env` files; that is the direction to move in, and is tracked separately.

## 5. Logging, and why it is a privacy question too

Tools and agents log prompts. Those logs can contain what an employee was working on, minute by minute, which the Data Protection Authority has warned may fall under the rules on monitoring employees.

- A tool is not approved until we know who can read its logs and for how long they are kept.
- Employees are told what is logged and why, before the tool is used.
- Any measurement we do ourselves is aggregated and never per person. That is already the rule for [#4092](https://github.com/Altinn/altinn-auth/issues/4092).

## 6. Writing things down

The documentation duty is proportional. Prompts for ordinary code are not case documents and are not archived.

Where AI output substantially shaped a decision record, a security assessment or an analysis that someone will rely on later, the document says so and names the model, so the result can be understood afterwards. That is the same rule as rule 2 in the working agreement.

## 7. If something went in by accident

It happens, and the useful response is speed, not silence.

1. Stop using that conversation and do not continue in it.
2. Tell the right people the same day:
   - **Always** the team lead.
   - **A secret, token or credential:** rotate it first, then tell a code owner. Rotation is the fix; reporting is not a substitute for it.
   - **An unpublished security finding or anything else security-sensitive:** a code owner, following [SECURITY.md](../../SECURITY.md), never a public issue.
   - **Personal data:** the data protection contact as well.
3. Write down what was sent, to which tool, and when. That record is what any later assessment needs.

Nobody is disciplined for reporting this quickly. The policy exists to make the mistake recoverable.

## 8. What this policy does not cover

- **Using an AI coding assistant is not a high-risk use under the AI Act.** There is no software-development category in Annex III, and a general-purpose assistant is not high-risk by itself, so the deployer duties for high-risk systems do not apply. What does apply is the duty to support AI literacy, which is why onboarding is required ([#4091](https://github.com/Altinn/altinn-auth/issues/4091)).
- **This is not a policy for AI inside the product.** If AI were ever placed in the authorization decision path itself, that is a different assessment entirely, and a high-risk one.
- **Whether a data protection impact assessment is needed** for a given tool depends on the processing and its risk. That assessment belongs to the approval of each tool in section 2, not to this page.

## Open questions for the security review

1. Who owns the approved-tools list, and how often is it re-checked?
2. Is a data protection impact assessment required for the tools we already use, and who carries it out?
3. Is the "internal" class correct for issue and PR text that is not yet public, or should that be sensitive?
4. Should this page be in Norwegian? It is English to match the other governance documents in this repository, but it is the one most likely to be read outside the team.
5. Who is the data protection contact the team should reach in section 7?

## Sources

The reasoning and the full citations are in [working-agreement-rationale.md](working-agreement-rationale.md), section 3. The sources that shape this page in particular are Digdir's guidance on generative AI, the classification tiers from DFØ's marketplace guidance, the Data Protection Authority's report on Copilot at NTNU, NSM's principles for outsourced services, and the AI Act's Article 4.
