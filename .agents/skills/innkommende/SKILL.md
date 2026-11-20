---
name: innkommende
description: Lager ukentlig rapport til produkteier om innkommende saker, spørsmål fra andre som venter på svar fra teamet, svartid og bugs, ferdig til å limes inn i Slack eller Teams. Bruk når noen spør om nye issues, triage, hva som venter på svar, svartid, bugoversikt eller feilsituasjonen. Bare rapport; endrer ingenting.
---

# Innkommende, svartid og bugs

Lager en rapport på bokmål for produkteier om det som *kommer inn* til teamet. Den dekker nye saker, spørsmål fra andre team, tjenesteeiere og systemleverandører som venter på svar, og bug-situasjonen. Rapporten gjelder de fire repoene, og repoene er åpne, så svartid utad er synlig.

## Regler

- **Bare rapport.** Ikke svar på, merk, lukk eller tildel saker, heller ikke om noen ber om det i samme forespørsel.
- **Oppgaver, ikke personer.** «Teamet» er medlemmene av GitHub-teamene i config. Alle andre er enten «annet Altinn-team» (medlem av organisasjonen) eller «ekstern». Skriptet bruker brukernavn bare til denne inndelingen og skriver dem aldri ut. Rapporten nevner ingen navn, verken fra teamet eller utenfor, og ingen svartid per person. Se [data-policyen](../../../docs/ai/data-policy.md).
- **Ikke siter kommentarer.** Beskriv hva saken gjelder ut fra tittelen. Les saken med `gh issue view` om du trenger mer, men gjengi aldri hvem som spurte.

## Steg

1. **Hent data.** Fra roten av repoet:

   ```bash
   node .agents/skills/innkommende/scripts/collect.mjs
   ```

   Tar rundt ett minutt. Krever Node 20+ og `gh` med `read:org` (teammedlemskap) og `project` (board-status).

2. **Les JSON-filen.**
   - `incoming`: saker opprettet siste `windowDays` dager. Inneholder fordeling på opphav (`team`, `altinn`, `external`), område og type, hvor mange som fortsatt er åpne, hvor mange som mangler område eller type, og hvor mange som står i `status/triage`.
   - `waitingOn`: åpne saker der noen utenfor teamet venter på svar. Sortert med nyeste først, fordi ferske spørsmål er lettest å svare på.
     - `comment-waiting`: siste kommentar er fra noen utenfor teamet, og teamet har ikke svart siden. Saker opprettes nesten bare av teamet, så det er her andre team og eksterne viser seg.
     - `no-response`: saken er opprettet av noen utenfor teamet, og ingen fra teamet har kommentert.
     - `from`: `altinn` eller `external`. `days`: dager siden de skrev.
   - `waitingOld`: tilsvarende saker eldre enn `waitingMaxAgeDays`. De telles, men listes ikke.
   - `trend`: median tid til første svar fra teamet, i dager, for saker fra andre siste `trendWeeks` uker. Første svar er en kommentar fra teamet eller at saken lukkes.
   - `bugs`: åpne bugs (issue-type Bug eller `kind/bug`), nye og lukkede siste `windowDays` dager, fordeling på område og alder, hvor mange som er i arbeid eller har åpen PR, og `oldestUntouched`, de eldste uten åpen PR som ikke er i arbeid.

3. **Skriv rapporten** etter malen under, til en fil i temp-mappen, f.eks. `innkommende-2026-10-19.md`, og gjør den klar til innliming:

   ```bash
   node .agents/skills/ukesrapport/scripts/render.mjs <sti-til-rapporten.md>
   ```

   På Windows havner den på utklippstavlen som formatert tekst (Ctrl+V i Slack eller Teams); ellers åpnes en HTML-fil å kopiere fra. Vis rapporten i svaret.

## Mal

```text
_Utkast fra innkommende-skillen (AI-assistert). Ikke gjennomgått ennå._

**Innkommende og bugs – Team Autorisasjon (12.–19. oktober 2026)**

**Kort fortalt**
2–4 setninger: hvor mye som kom inn, hva som venter på svar fra teamet, og om bug-tallet går opp eller ned.

**Venter på svar fra oss**
- [#4522](lenke) Søk og filtrering av systembrukere på organisasjon – ekstern, 68 dager uten svar
- [#2738](lenke) Maskinportenadministrasjon i ny brukerflate – annet Altinn-team, 48 dager
- ...
I tillegg N eldre saker der ingen har svart på over 180 dager.

**Nye saker siste 7 dager**
- 61 nye, alle fra teamet; 54 fortsatt åpne
- Flest i tilgangsstyring (19) og autentisering (12)
- 32 står i triage, og 20 mangler område eller type

**Svartid**
Median tid til første svar på saker fra andre siste 4 uker: X dager (N saker, M ubesvart).

**Bugs**
- 102 åpne: 13 nye og 10 lukket siste uke
- Flest i tilgangsstyring (37), autentisering (20), brukerflate (17) og ressursregister (17)
- Alder: 21 under 30 dager, 39 mellom 3 og 12 måneder, 30 over ett år
- 10 er i arbeid eller har åpen PR
- Eldste uten noen som jobber med dem: [#4207](lenke) Tittel (950 dager), ...
```

Detaljer:

- **Venter på svar fra oss** kommer først, fordi det er det produkteier kan gjøre noe med denne uka. Vis maks 10 og skriv «+N til». Er lista tom, skriv «Ingen spørsmål fra andre venter på oss.»
- **Svartid:** er `trend.issuesFromOthers` 0, skriv «Ingen nye saker fra andre de siste 4 ukene; spørsmål kommer som kommentarer, se over». Ikke vis en median.
- **Bugs:** «Eldste uten noen som jobber med dem» viser maks 5. Svært gamle bugs er ofte kandidater til `lukkekandidater` eller til å lukkes som not planned; si det i én setning når de fleste er over ett år.
- **Opphav** skrives som «fra teamet», «annet Altinn-team» eller «ekstern», aldri med navn eller organisasjon.
- Bruk bare `**fet**`, `_kursiv_`, `[tekst](url)` og `- `, som `render.mjs` gjør om til formatert tekst.

## Vedlikehold

- **Repoer, GitHub-team, board, vinduer og terskler** står i [`config.json`](config.json). Endres teamene etter omorganiseringen, oppdater `teams`.
- **`kind/bug`** telles med som bug inntil labelen er migrert til issue-typen Bug (#4360). Fjern den fra `bugLabels` etterpå.
- Claude Code finner skillen via pekeren i `.claude/skills/innkommende/SKILL.md`. Endre innholdet her, ikke der.
