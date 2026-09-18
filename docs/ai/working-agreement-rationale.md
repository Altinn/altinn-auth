# Why the working agreement says what it says

The rules in [CONTRIBUTING.md](../../CONTRIBUTING.md) under *Working with AI assistance* are deliberately short. This document is the long version: where each rule comes from, what other projects do, what the evidence says, and what we chose not to adopt. It is background, not policy. If this page and CONTRIBUTING.md disagree, CONTRIBUTING.md wins.

Everything here was checked against the cited source on 2026-09-18. Numbers are quoted with their definitions; where a widely repeated number could not be verified on a primary page, it is left out.

## 1. What other projects do

We read the AI contribution policies of 22 open-source projects and foundations, from full bans to encouraging use. A survey of the top 2,000 GitHub repositories found 281 such policies: 83% permit AI-assisted code, 15% forbid it, 67% require substantial human involvement, and 49% require disclosure (Hora, Robbes and Zacchiroli, September 2026). Half of the dedicated policy files have already been revised, mostly to tighten.

| Project | Stance | Disclosure mechanism |
| --- | --- | --- |
| Linux kernel | Allowed with conditions | `Assisted-by: LLM [tools]` trailer; agents may not sign off |
| curl | Allowed; strict on bug reports | Must state AI use in reports; fabricated reports banned |
| LLVM | Allowed with conditions | `Assisted-by:` suggested; human-written PR text; no autonomous agents |
| Kubernetes | Allowed with conditions | Statement in PR description; trailers explicitly forbidden |
| Django | Allowed with conditions | Tool, version and use disclosed; AI review bots not allowed on PRs |
| Node.js | Allowed with conditions | Disclose use and verification; brand names kept out of commits |
| Fedora | Allowed, encouraging | `Assisted-by: <tool>` when output used largely unchanged |
| Mozilla Firefox | Allowed with conditions | No disclosure; accountability instead |
| Rust (compiler and library teams) | Narrow: assist, not create | `llm-assisted` label, pre-arranged with reviewer |
| Home Assistant, Zed, ripgrep, Astral | Allowed; autonomous agents banned | Quoted AI output marked; own words required |
| Ghostty | Allowed with conditions | All use disclosed; must explain without AI |
| Apache Software Foundation | Allowed (licence framing) | `Generated-by:` token recommended |
| Linux Foundation, KubeVirt, Kyverno | Allowed with conditions | `Assisted-by:`, `Co-authored-by:` or `Generated-by:` |
| dotnet/runtime | Allowed, heavy internal use | Agent appends a visible AI-generated note to its own posts |
| Gentoo, NetBSD | Banned (2024) | n/a |
| QEMU | Banned (2025); tiered replacement proposed September 2026 | Proposed `AI-used-for:` trailer |

Source links are in section 6.

### What recurs

Across these policies, three rules appear almost everywhere, regardless of stance:

1. The human is accountable and must be able to explain the change in their own words.
2. Self-review, a passing build and passing tests come before asking for review.
3. AI review is advisory. A person approves, and bot comments are resolved or dismissed by a person.

Two more appear in about half of them: disclosure of substantial AI generation (49% of the 281 surveyed policies, 14 of the 22 read here; autocomplete and grammar help are usually exempt), and PR text, commit messages and replies written by the author rather than generated (28% of the surveyed policies forbid AI in communication; 11 of the 22 here, among them Kubernetes, LLVM, Node.js and Rust). Two more recur in the projects that use AI most: keep generated changes small, and give agents no secrets or production access.

### What we adopted, and what we left out

We adopted the five recurring rules, the size rule and the data rule, plus two of our own: verify AI-found bugs before filing them, and update `AGENTS.md` when you find a landmine.

We left out rules that solve a problem we do not have. Public denouncement lists, permanent bans and vouch systems exist because open-source projects receive drive-by contributions from strangers. Caps on open PRs, "AI PRs only on accepted issues" and Rust's circuit breaker manage volume from thousands of contributors. Rules keeping AI away from *good first issue* tickets protect mentoring of newcomers. Rules against vendor names in commit trailers reflect the kernel's and Node's views on advertising in git history. None of these fit a team of seventeen people who see each other every day.

We chose disclosure in the PR description rather than a mandatory commit trailer. Kubernetes forbids trailers because its CLA needs a human signatory. We have no CLA, so we keep whatever trailer the tool adds, because it makes measurement from git history possible, but the PR description is what reviewers read, so that is where the statement lives.

## 2. What the evidence says

The evidence on AI-assisted development is mixed, and the working agreement is built on the parts that are consistent. Three things are consistent across independent sources: developers overestimate the speed-up they get; review load and PR size grow faster than throughput; and unreviewed model output has a high rate of security defects that has not improved in two years.

### Keep PRs small

Reviewers find defects in roughly 200 to 400 lines per session, and detection falls off above 400 lines or above 500 lines per hour (SmartBear, Cisco study). Google's guidance treats 100 lines as a reasonable change and 1,000 as usually too large.

That limit existed before AI. What AI changed is the pressure on it. Teams with high AI adoption produced 98% more merged PRs, but PR size grew 154% and review time 91%, with no measurable gain at company level (Faros, July 2025, 10,000+ developers). A year later the same telemetry showed median review time up 441% and PRs merged without any review up 31% under high adoption (Faros, April 2026, 22,000 developers). Median PR size across 400 organisations grew from 44 to 72 lines in twelve months (DX, June 2026). DORA 2024 associated a 25% increase in AI adoption with a 7.2% drop in delivery stability, and DORA 2025 still found stability negatively related to adoption even as throughput turned positive.

The 400-line guideline in CONTRIBUTING.md is the top of the SmartBear range, chosen so that a PR can be reviewed properly in one sitting.

### You own what you submit

Every study that measured both perception and outcome found the same gap. Experienced developers in their own repositories expected AI to make them 24% faster, believed afterwards it had made them 20% faster, and were measured 19% slower (METR, July 2025, randomised, 16 developers, 246 tasks). METR's own summary of that study is that people overestimated AI's effect on their time by 40 percentage points on average (METR, May 2026). The February 2026 follow-up (57 developers, 143 repositories, 800+ tasks) is weak evidence in both directions: the ten returning participants were an estimated 18% faster and the 47 new ones 4% faster, neither significant and both with confidence intervals spanning zero, and METR itself calls the result an unreliable signal because 30% to 50% of participants withheld tasks they did not want to do without AI. METR then abandoned the design.

Comprehension is the mechanism. Junior engineers who used AI to learn a new library scored 50% on a quiz about the code they had just written, against 67% for those who wrote it by hand, with no significant time saved (Anthropic, randomised, January 2026). Comprehension survived when participants asked the assistant to explain rather than to do. Users with an AI assistant wrote less secure code and were more confident it was secure (Perry et al., Stanford, 2023). Higher confidence in generative AI correlated with less critical thinking among 319 knowledge workers (Microsoft Research, 2025).

"Explain it in your own words" is the cheapest known countermeasure, and the Anthropic result suggests it works.

### Generated tests get the same review as production code

Agent-authored commits touch test files at twice the human rate and add mocks in 36% of commits against 26% for humans, across 1.2 million commits in 2025 (Hora and Robbes, January 2026). Mocked tests are easier to generate and weaker at catching real integration faults. LLM-generated unit tests across 20,505 suites consistently carried the *assertion roulette* and *magic number* smells (Ouédraogo et al., 2024, revised 2026). More test activity coexisted with 54% more bugs per developer in high-adoption teams (Faros, 2026).

Nobody has measured escape rates with and without reviewed generated tests. The honest claim is narrower: generated tests have documented weaknesses that a green CI run will not reveal.

### Human review remains the gate

Across 100+ models and four languages, 45% of generated code samples failed security tests, and the pass rate was unchanged two years later at 55% (Veracode, 2025 and March 2026). Java fared worst; cross-site scripting and log injection failed in over 85% of relevant samples. Over 90% of issues in generated Java were code smells, and every model tested produced hard-coded credentials (Sonar, August 2025).

The counter-evidence belongs on the same page. In GitHub's controlled study, code written with Copilot was 53% more likely to pass all unit tests, and blind reviewers were 5% more likely to approve it (202 submissions, November 2024). Developers who later evolved AI-co-written code showed no maintainability penalty (CodeScene, 151 participants, 2025). The gate is justified by the defect rate of *unreviewed* output, not by a claim that AI code is uniformly worse. Industry practice agrees: the median company auto-merges 0.7% of PRs without human review (Jellyfish, August 2026, 99 million PRs).

### Disclose assistance

No study shows that disclosure changes outcomes on its own. The reason to disclose is instrumental. By self-report, 52% of code is now AI-authored (DX, June 2026), acceptance of AI suggestions rose from 20% to 60% in two years (Faros, 2026), and agents opened over a million PRs on GitHub in five months (Octoverse, 2025). Every study above that compared AI-assisted and unassisted code depended on knowing which was which. Without disclosure, the measurement in section 4 is impossible.

### Measure outcomes in aggregate

This is the best-supported rule. Perceived gains and measured gains diverged in the same direction in METR 2025, DORA 2025 (over 80% believe productivity rose while stability fell), Faros 2025 (individual gains, flat company metrics), Uplevel 2024 (41% more bugs, no change in cycle time) and Microsoft 2026 (24% more merged PRs, with the authors warning that a merged PR is not the same as value). Self-report is not a measurement. Aggregate, not per person, because the point is to learn, not to rank.

### AI use is a personal choice

The gains are heterogeneous. Contractors on a greenfield task were 55% faster with Copilot (GitHub, 2022), experts in familiar codebases were slower (METR, 2025), and adoption at Microsoft spread through social networks with retention tied to coding activity rather than mandates (Microsoft, 2026). 46% of developers distrust the accuracy of AI output against 33% who trust it, while 84% use or plan to use it (Stack Overflow, 2025, 49,000 respondents). Mandating use in either direction distorts. The caveat is that the developers who gain the most speed, juniors, are also the ones who lose the most learning, so optional does not mean unsupervised.

### Where the evidence is mixed

Speed: 55% faster in 2022, 19% slower in 2025, a non-significant 4% to 18% faster in the weak 2026 follow-up, and DORA flipping from negative to positive throughput between 2024 and 2025. Quality: GitHub's study and the CodeScene experiment are neutral to slightly positive; GitClear, Uplevel, Sonar, Veracode and Faros are negative. Vendor incentives run both ways: GitHub sells Copilot, the others sell measurement or scanning. Every large telemetry study is correlational and every randomised study is small. That is itself the argument for measuring our own outcomes.

## 3. The Norwegian and regulatory frame

Digdir is a state body, and the working agreement has to sit inside what Digdir itself, the government and the coming AI Act expect. This section maps those expectations to the rules. One verification note: digdir.no and regjeringen.no block automated readers. Digdir's guidance text was read through search-index snippets, and the rundskriv's section 1.3 was checked against the primary document by a reviewer of the PR that added this page. Confirm Digdir's exact wording in a browser before quoting it outside this document.

**Digdir's own guidance.** *Veiledning for ansvarlig bruk og utvikling av kunstig intelligens*, sub-page *Bruk av generativ KI* (open beta, last updated November 2024), is the clearest Norwegian public-sector source found that addresses developers using code assistants directly; NAV's guidance and tooling, below, are the most concrete practice. It says that personal data and information that is taushetsbelagt, gradert, unntatt offentlighet, security-sensitive or IP-protected must not be given to a generative tool; that for code tools this explicitly includes API keys, security tokens, environment variables and test data; and that because assistants cannot reliably exclude individual files, such material must be kept where the tool does not see it. The user is the sender and answers for the content. It recommends stating in the pull request that AI-assisted code generation was used, so the reviewer knows. It asks for training and routines for developers, and it cites the same Stanford finding as section 2. That is the source of rules 1, 2 and 7.

**Digitaliseringsrundskrivet D-2/25** (May 2025) is binding for state bodies, but its AI section 1.3 is a recommendation, not a requirement: state bodies *should* assess where AI helps, *should* have a plan, and *should* make sure employees know the organisation's routines for AI. Section 1.8 points to the duty under GDPR article 35 to carry out a data protection impact assessment when web-based tools such as ChatGPT are used; that duty comes from the GDPR, not from the rundskriv. This agreement, rule 9 on onboarding and the data policy in #4083 follow the recommendation; the DPIA belongs to #4083.

**The EU AI Act.** Article 4 on AI literacy has applied to deployers since February 2025 and is enforced in the EU from August 2026; the July 2026 Omnibus softened it to a duty to support the development of AI literacy. The Commission's Q&A says staff using Copilot- or ChatGPT-type tools are in scope, that measures are tailored to role and risk, and that organisations may keep an internal record of training. The Act is not yet Norwegian law: a new høring is planned for autumn 2026 and a proposition for spring 2027. A general-purpose coding assistant is not a high-risk system; Annex III has no software-development category, and the deployer duties in Article 26 apply only to high-risk systems. Article 50 covers public-facing text and synthetic media, not source code. One sentence for this team in particular: if AI were ever placed inside the authorization decision path itself, Annex III point 5(a) on access to essential public services could apply. That is outside the scope of a coding-assistant agreement, and worth remembering.

**Datatilsynet.** The regulatory-sandbox report on NTNU's use of Copilot (November 2024) holds that the organisation, not the vendor, is responsible for what a tool can reach, that users need sufficient knowledge and training, and that a DPIA is generally required where personal data is processed. It also warns that vendor and agent logs of employees' prompts can fall under the rules on monitoring employees, so logging needs a stated purpose and staff must be informed. That is why the measurement in #4092 is aggregate and never per person.

**DFØ and Markedsplassen for skytjenester** (July 2026) give the classification tiers the data policy will use: open and internal information may be processed in approved commercial cloud AI services, sensitive information needs a review by the data protection officer, gradert information is prohibited. A data processing agreement comes before any personal data, processing is within the EEA by default, the vendor must not train on inputs, and prompt retention must be known. A cloud framework agreement binds the cloud provider, not necessarily the application vendor.

**NSM.** The *Grunnprinsipper for IKT-sikkerhet* principles for outsourced and cloud services apply unchanged: keep oversight and control, risk-assess before the decision, set explicit requirements to the provider. *Risiko 2026* expects AI-driven cyber operations against Norwegian organisations this year. No NSM publication specific to code assistants was found. Rule 7's least-privilege line for agents follows from this.

**NAV** is the most concrete Norwegian developer practice in public. Its *Veileder for generativ kunstig intelligens* (June 2025) says no personal data about NAV users goes into generative AI, developers keep full responsibility for generated code, generated content is labelled, and code development in itself is generally not processing of personal data. NAV's tooling runs agents in an OS-level sandbox that blocks SSH keys, cloud credentials and `.env` files, allows MCP servers from a registry only, and does not let an agent-opened PR trigger CI until a person starts it. NAV's developer survey (April 2026, 163 respondents) found 93% using AI coding tools, training as the most requested improvement, and 59% worried about loss of deep understanding. Rules 1, 7 and 9 and the onboarding in #4091 mirror this.

**Skatteetaten** (policy November 2023, communication guidelines February 2025) names responsible persons for every AI use, asks for extra control where generative AI is used, requires training in security, privacy and data governance, and states that offentleglova and arkivplikt apply to AI use.

**Nasjonalarkivet** (interview December 2025; *arkivforskrifta* in force January 2026) reads the documentation duty proportionally: routine prompts are not case documents, but where AI output underpins an important decision, the instructions, inputs and model should be kept with the record. That is the origin of the sentence in rule 2 about ADRs and security assessments.

**Comparable Norwegian rule sets** say the same things in fewer words. NTNU's guideline (May 2026) is classification-driven with four tiers. Helsedirektoratet's *KI-vettregler* (September 2025) are nine rules: know the approved tools, never share sensitive information on open platforms, verify, take responsibility for what you share, be open about AI use. The University of Oslo's overview of six agencies' guidelines found the same four common denominators: no sensitive data in prompts, critical verification, the human remains responsible, transparency about use.

**Inside Digdir and Altinn** there is practice but no policy. The Altinn organisation has 58 `AGENTS.md` files (altinn-studio has a full hierarchy), `copilot-instructions.md` pointers in altinn-notifications and altinn-profile, and Digdir runs a public testbed for agentic tooling. No published Digdir decision approving specific tools, and no Digdir rules for its own staff's use of generative AI, were found. Closing that gap with Digdir security is the job of #4083.

What does not apply, so nobody has to ask: high-risk obligations under the AI Act (logging, registration, fundamental-rights assessment) do not apply to using a coding assistant, and Article 50 labelling does not apply to code.

## 4. Decisions in the agreement, and their alternatives

| Decision | Alternative considered | Why we chose as we did |
| --- | --- | --- |
| Disclosure as a statement in the PR description, trailers kept if the tool adds them | Mandatory `Assisted-by:` trailer (kernel, Fedora) | Reviewers read PR descriptions, not trailers. Trailers still feed measurement. A PR template field follows in #4084. |
| 400 changed lines as the guideline | 100 (Google), no number (most policies) | Top of the SmartBear range; a number people can act on. Excludes generated code, test data and lock files. |
| Autocomplete and grammar exempt from disclosure | Disclose everything (Ghostty) | Disclosure of trivial use produces noise and is not followed. Fedora, KubeVirt and the QEMU proposal draw the same line. |
| AI review bots advisory, one bot for the repo | Ban bots on PRs (Django) | The Copilot review ruleset already exists here. Advisory-only removes the failure mode Django worried about. Decision on which bot is #4085. |
| Optional per person, rules mandatory for all | Mandate use, or ban use | Heterogeneous gains, low trust, and the harassment clause in Rust's policy all point the same way. |
| PR text and review replies are the author's: drafting with a tool is allowed, every claim is checked, the verification described is the verification done | An absolute ban on generated PR text (Kubernetes, jj, the QEMU proposal) | The pain maintainers describe is text nobody has read, not text a tool helped draft. LLVM's line, copy-editing yes and authorship no, fits a team that talks every day. |
| Verification proportional to the change | Full build and test run before every review | A docs-only change gets a link check; authorization logic gets negative tests and permission boundaries. What matters is that the checks that fit the change were run and the gaps are stated. |
| Generated code excluded from the size guideline means mechanically generated files only | Exclude everything a tool produced | Code an assistant wrote is exactly the code that needs review capacity, so it counts in full. |
| Onboarding before use, recorded by the team | No onboarding requirement | AI Act Article 4, the rundskriv's "know the routines", Datatilsynet's training point and NAV's survey all ask for it. Content is #4091. |
| Decision records name the model when substantially AI-assisted | Disclose only in PRs | Nasjonalarkivet's proportional documentation duty: provenance matters where output underpins a decision. |

## 5. What this document does not settle

- Which tools are approved and what data may go where: #4083.
- The PR template field for disclosure: #4084.
- The review bot: #4085.
- How the numbers in section 4 are actually collected: #4092.
- Whether 400 lines is the right number for this codebase: revisit after #4092 has six months of data.

## 6. Sources

Policies:

- Linux kernel, *Coding assistants*: <https://docs.kernel.org/process/coding-assistants.html> and *Generated content*: <https://docs.kernel.org/process/generated-content.html>
- curl, *Contributing* (AI section): <https://curl.se/dev/contribute.html>; *Death by a thousand slops* (Stenberg, 2025-07-14): <https://daniel.haxx.se/blog/2025/07/14/death-by-a-thousand-slops/>
- LLVM, *AI Tool Use Policy*: <https://llvm.org/docs/AIToolPolicy.html>
- Kubernetes, *Pull request guide, AI guidance*: <https://www.kubernetes.dev/docs/guide/pull-requests/#ai-guidance>; review-bot policy: <https://github.com/kubernetes/community/blob/main/github-management/ai-code-review-tools.md>
- Django, *Submitting patches*: <https://docs.djangoproject.com/en/dev/internals/contributing/writing-code/submitting-patches/>
- Node.js, *AI guidelines*: <https://github.com/nodejs/node/blob/main/doc/contributing/ai-guidelines.md>
- Fedora Council, *Policy on AI-assisted contributions* (approved 2025-10-22): <https://pagure.io/Fedora-Council/tickets/issue/542>
- Mozilla Firefox, *AI-assisted coding*: <https://firefox-source-docs.mozilla.org/contributing/ai-coding.html>
- Rust, *LLM usage policy*: <https://forge.rust-lang.org/policies/llm-usage.html>
- Home Assistant, *AI policy* (2026-07-20): <https://developers.home-assistant.io/blog/2026/07/20/ai-policy/>
- Zed, *CONTRIBUTING.md* (AI policy): <https://github.com/zed-industries/zed/blob/main/CONTRIBUTING.md>; Astral template: <https://github.com/astral-sh/.github/blob/main/AI_POLICY.md>
- Ghostty, *AI_POLICY.md*: <https://github.com/ghostty-org/ghostty/blob/main/AI_POLICY.md>
- Apache Software Foundation, *Generative tooling guidance*: <https://www.apache.org/legal/generative-tooling.html>
- Linux Foundation, *Generative AI policy*: <https://www.linuxfoundation.org/legal/generative-ai>; KubeVirt: <https://github.com/kubevirt/community/blob/main/ai-contribution-policy.md>; Kyverno: <https://github.com/kyverno/community/blob/main/AI_USAGE_POLICY.md>
- dotnet/runtime, *copilot-instructions.md*: <https://github.com/dotnet/runtime/blob/main/.github/copilot-instructions.md>; Toub, *Ten months with CCA in dotnet/runtime* (2026-03-23): <https://devblogs.microsoft.com/dotnet/ten-months-with-cca-in-dotnet-runtime/>
- Gentoo Council, *AI policy* (2024-04-14): <https://wiki.gentoo.org/wiki/Project:Council/AI_policy>
- NetBSD, *Commit guidelines*: <https://www.netbsd.org/developers/commit-guidelines.html>
- QEMU, *Code provenance*: <https://www.qemu.org/docs/master/devel/code-provenance.html>
- OpenSSF, *Security-focused guide for AI code assistant instructions* (2025-08-01): <https://best.openssf.org/Security-Focused-Guide-for-AI-Code-Assistant-Instructions>
- GitHub, *Responsible use of Copilot code review*: <https://docs.github.com/en/copilot/responsible-use/code-review>
- Hora, Robbes, Zacchiroli, *"We Permit the Use of AI, but..."* (2026-09-07): <https://arxiv.org/html/2609.07542>; curated list: <https://github.com/melissawm/open-source-ai-contribution-policies>

Evidence:

- DORA, *Accelerate State of DevOps 2024* (2024-10-23): <https://dora.dev/research/2024/dora-report/>
- DORA, *State of AI-assisted Software Development 2025* (2025-09-23): <https://cloud.google.com/blog/products/ai-machine-learning/announcing-the-2025-dora-report>
- METR, *Measuring the Impact of Early-2025 AI on Experienced Open-Source Developer Productivity* (2025-07-10): <https://metr.org/blog/2025-07-10-early-2025-ai-experienced-os-dev-study/>
- METR, *We are changing our developer productivity experiment design* (2026-02-24): <https://metr.org/blog/2026-02-24-uplift-update/>
- METR, *Measuring the Self-Reported Impact of Early-2026 AI on Technical Worker Productivity* (2026-05-11): <https://metr.org/blog/2026-05-11-ai-usage-survey/>
- GitHub, *Quantifying GitHub Copilot's impact on developer productivity* (2022-09-07): <https://github.blog/news-insights/research/research-quantifying-github-copilots-impact-on-developer-productivity-and-happiness/>
- GitHub, *Does GitHub Copilot improve code quality?* (2024-11-18): <https://github.blog/news-insights/research/does-github-copilot-improve-code-quality-heres-what-the-data-says/>
- GitHub, *Octoverse 2025* (2025-10-28): <https://github.blog/news-insights/octoverse/octoverse-a-new-developer-joins-github-every-second-as-ai-leads-typescript-to-1/>
- Stack Overflow, *2025 Developer Survey, AI* (2025-07-29): <https://survey.stackoverflow.co/2025/ai>
- Veracode, *2025 GenAI Code Security Report* (2025-07-30): <https://www.veracode.com/blog/genai-code-security-report/>; *Spring 2026 update* (2026-03-24): <https://www.veracode.com/blog/spring-2026-genai-code-security/>
- Perry, Srivastava, Kumar, Boneh, *Do Users Write More Insecure Code with AI Assistants?* (CCS 2023): <https://arxiv.org/abs/2211.03622>
- Sonar, *The Coding Personalities of Leading LLMs* (2025-08-13): <https://www.sonarsource.com/company/press-releases/the-coding-personalities-of-leading-llms/>
- SmartBear, *Best Practices for Code Review* (Cisco study): <https://smartbear.com/learn/code-review/best-practices-for-peer-code-review/>
- Google, *Small CLs*: <https://google.github.io/eng-practices/review/developer/small-cls.html>
- Shen and Tamkin, Anthropic, *How AI assistance impacts the formation of coding skills* (2026-01-29): <https://www.anthropic.com/research/AI-assistance-coding-skills>
- Hora and Robbes, *Are Coding Agents Generating Over-Mocked Tests?* (2026-01-30): <https://arxiv.org/abs/2602.00409>
- Ouédraogo et al., *On the Diffusion of Test Smells in LLM-Generated Unit Tests* (2024, revised 2026-08-01): <https://arxiv.org/abs/2410.10628>
- CodeScene, *Echoes of AI: Downstream Effects of AI Assistants on Software Maintainability* (2025, revised 2026): <https://arxiv.org/abs/2507.00788>
- Lee et al., Microsoft Research, *The Impact of Generative AI on Critical Thinking* (CHI 2025): <https://www.microsoft.com/en-us/research/publication/the-impact-of-generative-ai-on-critical-thinking-self-reported-reductions-in-cognitive-effort-and-confidence-effects-from-a-survey-of-knowledge-workers/>
- GitClear, *The Maintainability Gap: AI Code Quality in 2026* (2026-01): <https://www.gitclear.com/the_ai_code_quality_maintainability_gap>
- Faros AI, *The AI Productivity Paradox* (2025-07-23): <https://www.faros.ai/blog/ai-software-engineering>; *The Acceleration Whiplash* (2026-04-12): <https://www.faros.ai/blog/ai-acceleration-whiplash-takeaways>
- Jellyfish, *AI engineering trends* (updated 2026-08): <https://jellyfish.co/ai-engineering-trends/>
- DX, *AI-authored code has nearly doubled, but so has PR size* (2026-06-17): <https://newsletter.getdx.com/p/ai-authored-code-has-nearly-doubled>
- Murphy-Hill, Butler, Savelieva, *Adoption and Impact of Command-Line AI Coding Agents at Microsoft* (2026-07-01): <https://arxiv.org/abs/2607.01418>
- Uplevel, *Can Generative AI Improve Developer Productivity?* (2024-09): <https://uplevelteam.com/blog/ai-for-developer-productivity>

Norwegian and regulatory (digdir.no pages were read through search-index snippets; confirm wording in a browser before quoting):

- Digdir, *Bruk av generativ KI* (open beta, updated 2024-11): <https://www.digdir.no/kunstig-intelligens/bruk-av-generativ-ki/4670>; *Anskaffelse av generativ KI*: <https://www.digdir.no/kunstig-intelligens/anskaffelse-av-generativ-ki/7546>
- Digdir, Prosjektveiviseren, *Kunstig intelligens* (2026-06-25): <https://prosjektveiviseren.digdir.no/god-praksis-og-tilpasning/kunstig-intelligens/272>; KI Norge, *KI-loven: informasjon* (2026-06-10): <https://ki.norge.no/veiledning/ki-loven-informasjon>
- Regjeringen, *Digitaliseringsrundskrivet* D-2/25 (2025-05): <https://www.regjeringen.no/no/dokumenter/digitaliseringsrundskrivet/id3103320/>; coverage: <https://www.altinget.no/artikkel/ny-instruks-fra-digitaliseringsminsteren-alle-statlige-virksomheter-boer-ta-i-bruk-kunstig-intelligens>
- Datatilsynet, *NTNU: Copilot through the lens of data protection* (2024-11-26): <https://www.datatilsynet.no/en/regulations-and-tools/reports-on-specific-subjects/reports/ntnu-copilot-through-the-lens-of-data-protection/>; *KI og personvern på arbeidsplassen* (2025-03-19): <https://www.datatilsynet.no/aktuelt/aktuelle-nyheter-2025/videoforedrag-ny-sideki-og-personvern-pa-arbeidsplassen/>
- DFØ, Markedsplassen, *Krav til sikkerhet og informasjonsbehandling ved bruk av kunstig intelligens* (2026-07-02): <https://markedsplassen.anskaffelser.no/krav-til-sikkerhet-og-informasjonsbehandling-ved-bruk-av-kunstig-intelligens>; *Veiledning i anskaffelse og bruk av kunstig intelligens på MPS rammeavtaler* v0.9 (2026-07-07): <https://markedsplassen.anskaffelser.no/kunnskap-og-veiledning/kunstig-intelligens/veiledning-i-anskaffelse-og-bruk-av-kunstig-intelligens-pa-mps-rammeavtaler-v09>
- NSM, *Grunnprinsipper for IKT-sikkerhet*, *Bruk av tjenesteutsetting og skytjenester* (updated 2024-04-30): <https://nsm.no/regelverk-og-hjelp/rad-og-anbefalinger/grunnprinsipper-for-ikt-sikkerhet-2-0/introduksjon-1/bruk-av-tjenesteutsetting-og-skytjenester>; *Risiko 2026* (2026-02-06): <https://nsm.no/regelverk-og-hjelp/rapporter/risiko-2026>
- EU AI Act, Article 4: <https://artificialintelligenceact.eu/article/4/>; Article 26: <https://artificialintelligenceact.eu/article/26/>; Article 50: <https://artificialintelligenceact.eu/article/50/>; Annex III: <https://artificialintelligenceact.eu/annex/3/>; European Commission, *AI literacy – Questions & Answers* (updated 2026-07-27): <https://digital-strategy.ec.europa.eu/en/faqs/ai-literacy-questions-answers>; *AI Omnibus enters into force* (2026-07-27): <https://digital-strategy.ec.europa.eu/en/news/ai-omnibus-enters-force>
- Norwegian AI Act status: digi.no (2026-08-02): <https://www.digi.no/artikler/nye-eu-regler-for-ki-norge-og-eos-henger-enna-langt-etter/575266>; Nkom, *KI-loven i et nøtteskall* (2026-06-10): <https://nkom.no/ki/i-et-notteskall>
- NAV, *Veileder for generativ kunstig intelligens* (2025-06): <https://data.nav.no/fortelling/ki/>; developer tooling: <https://github.com/navikt/cplt>, <https://ki-utvikling.nav.no/>; *Utviklerundersøkelsen 2026* (2026-04-15): <https://ki-utvikling.nav.no/nyheter/utviklerundersokelsen-2026>
- Skatteetaten, *Policy for utvikling og bruk av KI i Skatteetaten* v1.1 (2023-11-20): <https://www.skatteetaten.no/globalassets/om-skatteetaten/om-oss/ki/policy-ki-i-skatteetaten-1-1.pdf>; *Retningslinjer for bruk av generativ kunstig intelligens i kommunikasjon* (2025-02-27): <https://www.skatteetaten.no/en/stilogtone/god-praksis/kunstig-intelligens/>
- Nasjonalarkivet, interview in Khrono (2025-12-18): <https://www.khrono.no/ogsa-prompter-fra-ki-verktoy-skal-arkiveres-trenger-bevisstgjoring/1021249>; *Forskrift om dokumentasjon og arkiv* (in force 2026-01-01): <https://lovdata.no/dokument/SF/forskrift/2025-12-17-2647/>
- NTNU, *Retningslinje for bruk av IKT-verktøy med generativ KI* (2026-05-22): <https://i.ntnu.no/wiki/-/wiki/Norsk/Bruk+av+IKT-verkt%C3%B8y+med+generativ+kunstig+intelligens+ved+NTNU+-+retningslinje>; Helsedirektoratet, *KI-vettregler* (2025-09-15): <https://www.helsedirektoratet.no/digitalisering-og-e-helse/kunstig-intelligens/ki-vettregler>; UiO DigiVel, overview of agency guidelines (2024-05-24): <https://www.jus.uio.no/ior/forskning/prosjekter/digivel/blogg/veiledninger-og-retningslinjer-for-bruk-av-generat.html>
- Digdir, *digdir-ai-agents* testbed: <https://github.com/digdir/digdir-ai-agents>
