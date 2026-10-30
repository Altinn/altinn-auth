---
name: tavlestatus
description: Lager ukentlig status for teamets GitHub-board (Team Autorisasjon) til Slack eller Teams, med flyt (hva som er i arbeid, hvor lenge, PR-er som venter på review) og hygiene (saker der boardet ikke stemmer). Bruk når noen ber om tavlestatus, boardstatus, WIP, hva som er i arbeid eller rydding på boardet. Bare rapport; endrer ingenting.
---

# Tavlestatus: flyt og hygiene på boardet

Lager en rapport på bokmål for [Team Autorisasjon](https://github.com/orgs/Altinn/projects/196). Den har to deler:

- **Flyt:** hva som er i arbeid, hva som har stått lenge, og hva som venter på review. Den er til teamet før standup og planlegging.
- **Hygiene:** konkrete saker der boardet ikke stemmer med virkeligheten, slik at produkteier og team kan stole på det.

## Regler

- **Bare rapport.** Ikke flytt kort, sett felt, lukk eller kommenter, heller ikke om noen ber om det i samme forespørsel. Si at skillen bare rapporterer.
- **Oppgaver, ikke personer.** Ingen navn, GitHub-brukere, @-omtaler, fordeling av saker per person eller «hvem som bør ta hva». «Uten assignee» er greit; «X har 6 saker» er det ikke. PR-titler som ser ut som branchnavn (`Feature/26/09/...`) skrives om til hva PR-en gjør, siden branchnavn ofte inneholder brukernavn. Se [data-policyen](../../../docs/ai/data-policy.md).
- **Ikke lag tall ut av tynne data.** Er `project.statusHistoryDays` lavere enn terskelen for In progress, si at «dager i status» ikke sier mye ennå.

## Steg

1. **Hent data.** Fra roten av repoet:

   ```bash
   node .agents/skills/tavlestatus/scripts/collect.mjs
   ```

   Tar rundt et halvt minutt. Krever Node 20+ og `gh` med `project`-scope (`gh auth refresh -s project`). Skriptet skriver JSON til temp-mappen og en kort oppsummering.

2. **Les JSON-filen.**
   - `flow.counts`: åpne saker per statusgruppe (`config.statuses` mapper Status-navn til grupper).
   - `flow.inWork`: alle saker i In progress og In review, med `daysInStatus`, koblede PR-er og `flags`:
     - `over-threshold`: lenger i status enn `thresholdDays`
     - `in-review-without-open-pr`: i review, men ingen åpen PR
     - `closing-pr-merged`: PR-en som skulle lukke saken er merget, men saken er åpen, så den bør trolig til Done
     - `not-horizon-now`: i arbeid, men ikke på Horizon «Now» (bare når Horizon brukes)
     - `no-assignee`: i arbeid uten assignee
   - `flow.prsWaiting`: åpne, ikke-draft PR-er i teamets repoer, uten godkjenning, mellom `prWaitingDays` og `stalePrDays` gamle. `reviews` er antall reviews så langt. `flow.prsApprovedNotMerged` er godkjente PR-er som ikke er merget.
   - `flow.doneInWindow`: saker på boardet lukket som ferdige de siste `doneWindowDays` dagene.
   - `hygiene`: én liste per sjekk, se tabellen under.
   - `fields`: board-felt uten valg (`no-options`) eller brukt på under `fieldUsagePercent` % av åpne saker (`rarely-used`).
   - `project.workflows`: boardets innebygde automatikk og om den er på.

3. **Skriv rapporten** etter malen under, til en fil i temp-mappen, f.eks. `tavlestatus-2026-10-19.md`, og gjør den klar til innliming:

   ```bash
   node .agents/skills/ukesrapport/scripts/render.mjs <sti-til-rapporten.md>
   ```

   På Windows havner den på utklippstavlen som formatert tekst (Ctrl+V i Slack eller Teams); ellers åpnes en HTML-fil å kopiere fra. Vis rapporten i svaret.

| Sjekk | Betyr | Typisk tiltak |
| --- | --- | --- |
| `closed-not-done` | Lukket, men ikke i Done | Flytt til Done, og sjekk at automatikken «Item closed» setter Done |
| `open-but-done` | Åpen, men i Done | Lukk saken eller flytt den tilbake |
| `open-without-status` | Åpen uten status | Sett status |
| `missing-type` | Mangler issue-type | Sett Bug, Feature, Task eller Epic |
| `missing-area` | Mangler `area/*` | Sett område |
| `old-triage` | `status/triage` eldre enn `triageDays` | Triager |
| `open-under-closed-parent` | Åpen sub-issue under lukket forelder | Lukk, eller flytt til en åpen forelder |
| `not-on-board` | Åpen i et av teamets repoer, men ikke på boardet | Legg til, eller avklar at den hører til et annet board |
| `stale-prs` | PR åpen lenger enn `stalePrDays` | Lukk, eller få den i mål |

## Mal

```text
_Utkast fra tavlestatus-skillen (AI-assistert). Ikke gjennomgått ennå._

**Tavlestatus – Team Autorisasjon (19. oktober 2026)**

**Kort fortalt**
2–3 setninger: hvor mye som er i arbeid, hva som haster mest å se på i flyten, og den viktigste ryddejobben.

**Flyt**
- I arbeid: 32 (25 In progress, 7 In review). Backlog 644, Ready 0. Ferdig siste 7 dager: 32.
- Bør trolig til Done (lukkende PR er merget): [#4511](lenke), [#4518](lenke), ...
- I review uten åpen PR: [#3522](lenke), ...
- Lenge i status: [#123](lenke) Tittel – 21 dager i In progress
- I arbeid uten assignee: [#4084](lenke), ...

**PR-er som venter**
- Ingen review ennå: [#4414](lenke) Tittel (5 dager), ...
- Under review, ikke godkjent: [#4268](lenke) Tittel (20 dager), ...
- Godkjent, ikke merget: [#4401](lenke) Tittel

**Hygiene**
- 1 lukket sak står ikke som Done: [#4321](lenke)
- 227 åpne saker mangler issue-type
- 28 mangler område: [#155](lenke), ...
- 45 åpne sub-issues under lukket forelder, flest under [#719](lenke) (8)
- 213 åpne saker ligger ikke på boardet (altinn-auth 127, frontend 66, register 14, utils 6)
- 14 PR-er har vært åpne i over 30 dager, den eldste i 348 dager

**Oppsett av boardet**
- Priority har ingen valg og kan ikke brukes.
- Horizon er satt på 4 % av åpne saker, Size, Sprint og Target date på under 1 %.
```

Detaljer:

- **Flyt** først, så **PR-er som venter**, så **Hygiene**, og til slutt **Oppsett av boardet**. Utelat punkter og seksjoner uten treff.
- **Lister:** vis maks 8 lenker per punkt og skriv «+N til». Store tall (som manglende type) får bare antall og eventuelt fordeling per repo eller område, ikke en lang lenkeliste.
- **WIP-grense:** er `flow.wip.limit` satt, skriv «32 av maks 20». Er den ikke satt, ikke finn på en.
- **Ferdig siste 7 dager** er et tall, ikke en liste; leveransene står i ukesrapporten.
- **Oppsett av boardet** tas med bare når det er `fields` med flagg. Er en automatikk i `project.workflows` slått av, nevn det.
- Bruk bare `**fet**`, `_kursiv_`, `[tekst](url)` og `- `, som `render.mjs` gjør om til formatert tekst.

## Vedlikehold

- **Board, repoer, statusnavn, terskler og WIP-grense** står i [`config.json`](config.json). Får boardet nye statuser, legg dem inn under riktig gruppe i `statuses`.
- **Når Priority og Target date tas i bruk**, kan flyt-delen sortere på prioritet og vise saker som er over måldato. Det er ikke laget ennå.
- Claude Code finner skillen via pekeren i `.claude/skills/tavlestatus/SKILL.md`. Endre innholdet her, ikke der.
