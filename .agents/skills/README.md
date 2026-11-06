# Rapporter som Agent Skills

> 🤖 Utkast laget med AI-assistanse (Claude Code). Leser ikke angitt før en person bekrefter at den er lest.

**TL;DR**

- Fem rapporter om teamets arbeid. Du starter dem fra en AI-agent (Claude Code, Codex m.fl.) i roten av repoet, og de lager tekst som kan limes rett inn i Slack eller Teams.
- Alle bare leser fra GitHub. De endrer aldri issues, PR-er eller boardet.
- Ingen av dem nevner personer, bare oppgaver og aggregerte tall.
- Du trenger Node 20+ og `gh` innlogget med scopene `repo`, `read:org` og `project`.
- Ingenting kjører automatisk ennå. Det er tema for [#4089](https://github.com/Altinn/altinn-auth/issues/4089).

## Rapportene

| Skill | Viser | Til | Når | Tid |
| --- | --- | --- | --- | --- |
| [`ukesrapport`](ukesrapport/SKILL.md) | Hva teamet har levert: lukkede issues, mergede PR-er, releaser og epic-fremdrift, per område | Ledelse og team | Ukentlig, fredag | ~25 s |
| [`tavlestatus`](tavlestatus/SKILL.md) | Flyt (hva som er i arbeid, hvor lenge, PR-er som venter) og hygiene (der boardet ikke stemmer) | Teamet | Ukentlig, før standup eller planlegging | ~35 s |
| [`epicstatus`](epicstatus/SKILL.md) | Fremdrift, blokkeringer, risiko og tempo per aktiv epic eller feature, og hva som står stille | Produkteier | Annenhver uke | ~50 s |
| [`innkommende`](innkommende/SKILL.md) | Nye saker, spørsmål fra andre som venter på svar fra oss, svartid og bugs | Produkteier | Ukentlig | ~65 s |
| [`lukkekandidater`](lukkekandidater/SKILL.md) | Åpne issues som ser ferdige ut, som teamet bør vurdere å lukke | Teamet | Ved behov, f.eks. månedlig rydding | ~65 s |

Tidene gjelder innhenting av data for de fire repoene. Etterpå trenger agenten litt tid på å skrive rapporten.

## Slik kjører du en rapport

Start agenten i roten av repoet, så den finner skillene:

- **Claude Code:** skriv `/ukesrapport`, eller be om det med vanlige ord, f.eks. «lag ukesrapport» eller «hva venter på svar fra oss?». Claude Code leser skillene fra `.claude/skills/`, der det bare ligger pekere til mappene her.
- **Codex og GitHub Copilot:** be om rapporten med vanlige ord. Begge leser skillene fra `.agents/skills/`.

Agenten kjører innhentingsskriptet, leser resultatet, skriver rapporten etter malen i skillen og legger den klar til innliming. Du kan be om justeringer, for eksempel en annen periode (`ukesrapport` tar `--from` og `--to`), et kortere sammendrag eller bare ett område.

### Lime inn i Slack eller Teams

Slack og Teams viser ikke markdown som limes inn som ren tekst. Rapporten gjøres derfor om til formatert tekst med [`ukesrapport/scripts/render.mjs`](ukesrapport/scripts/render.mjs):

- **Windows:** rapporten legges på utklippstavlen. Lim inn med Ctrl+V, og fet skrift, lenker og lister blir med.
- **macOS og Linux:** en HTML-fil åpnes i nettleseren. Kopier med Ctrl+A og Ctrl+C, og lim inn.

Les gjennom før du poster. Rapporten starter med en linje om at den er et AI-assistert utkast; den fjerner eller endrer du når du har lest den.

### Uten agent

Innhentingsskriptene kan kjøres direkte, for eksempel `node .agents/skills/tavlestatus/scripts/collect.mjs`. De skriver en JSON-fil til temp-mappen og en kort oppsummering i terminalen. Selve rapportteksten skrives av agenten etter malen i `SKILL.md`, så uten agent får du bare dataene.

## Krav

| | `ukesrapport` | `lukkekandidater` | `tavlestatus` | `epicstatus` | `innkommende` |
| --- | --- | --- | --- | --- | --- |
| Node 20+ og `gh` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `project` (board-status) | | valgfri | ✓ | ✓ | ✓ |
| `read:org` (teammedlemskap) | | | | | ✓ |

Mangler du et scope:

```bash
gh auth refresh -s project,read:org
```

## Felles regler

- **Bare lesing.** Skillene lukker, kommenterer, merker og flytter ingenting, heller ikke om noen ber om det.
- **Oppgaver, ikke personer.** Ingen navn, GitHub-brukere, @-omtaler eller tall per person. Skriptene skriver ikke ut forfattere eller assignees, og fjerner @-omtaler og brukernavn i branch-lignende PR-titler. Se [data-policyen](../../docs/ai/data-policy.md).
- **Utkast først.** Rapporten er et forslag. Den som poster den, har lest den.

## Oppbygging og vedlikehold

Hver skill er en mappe med:

- `SKILL.md`: framgangsmåte, regler og mal, i [Agent Skills-formatet](https://agentskills.io/specification)
- `config.json`: repoer, board, områder, terskler og vinduer
- `scripts/collect.mjs`: innhenting med `gh`, uten avhengigheter

En ny skill trenger tre ting: en mappe her, en peker i `.claude/skills/<navn>/SKILL.md` med samme frontmatter (se en av de eksisterende), og en rad i tabellen over. Mappene er dekket av `CODEOWNERS`. Plasseringen følger [ADR-0002](../../docs/adr/0002-tool-neutral-agent-contract.md): innholdet ligger ett sted, uten symlenker.

Når noe endres i teamets oppsett, er det som regel nok å endre `config.json`:

- **Nye repoer**, eller repoer som flyttes inn i altinn-auth: `repos` i hver skill.
- **Nye statuser på boardet:** `statuses` i `tavlestatus`, `epicstatus` og `innkommende`.
- **Omorganisering av team:** `teams` i `innkommende`.
- **Releaser og prod-deploy:** se *Vedlikehold* i [`ukesrapport`](ukesrapport/SKILL.md).
