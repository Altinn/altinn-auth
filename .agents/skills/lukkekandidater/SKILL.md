---
name: lukkekandidater
description: Finner åpne issues som ser ut til å være utført, og lager en rapport for Slack eller Teams over saker teamet bør vurdere å lukke. Bruk når noen spør etter issues som kan lukkes, åpne saker som er ferdige, rydding i backlog eller lukkekandidater. Bare rapport; lukker og kommenterer ingenting.
---

# Lukkekandidater: åpne issues som ser utført ut

Går gjennom alle åpne issues i teamets repoer og finner dem som ser ferdige ut, men ikke er lukket. Resultatet er en rapport på bokmål til Slack eller Teams. Teamet avgjør selv hva som lukkes.

## Regler

- **Bare rapport.** Ikke lukk, kommenter, merk eller endre issues, heller ikke om noen ber om det i samme forespørsel. Si i stedet at skillen bare rapporterer.
- **Oppgaver, ikke personer.** Ingen navn, GitHub-brukere, @-omtaler eller hvem som bør lukke hva. Ikke siter kommentarer ordrett; beskriv signalet («siste kommentar sier at den er løst»). Samme regel som i [ukesrapport](../ukesrapport/SKILL.md) og [data-policyen](../../../docs/ai/data-policy.md).
- **Forslag, ikke fasit.** Rapporten sier tydelig at listen må gås gjennom før noe lukkes.

## Steg

1. **Hent data.** Fra roten av repoet:

   ```bash
   node .agents/skills/lukkekandidater/scripts/collect.mjs
   ```

   Tar omtrent ett minutt for de fire repoene. Krever Node 20+ og `gh` innlogget med `project`-scope for prosjektstatus (`gh auth refresh -s project`); uten den hoppes det signalet over, og skriptet sier fra. `--repo Altinn/altinn-auth` begrenser til ett repo.

2. **Les JSON-filen.** Hver kandidat har `signals` (hvorfor den ser ferdig ut), `counters` (hvorfor kanskje ikke), `score` og `strength`:

   | Signal | Vekt | Betyr |
   | --- | --- | --- |
   | `merged-closing-pr` | sterk | En PR som skulle lukke saken er merget, ofte til en annen branch eller fra et annet repo |
   | `subissues-complete` | sterk | Alle sub-issues er lukket som ferdige |
   | `project-done` | sterk | Status «Done» eller «Closed» i et GitHub Project |
   | `checklist-complete` | middels | Alle punkter i sjekklisten i beskrivelsen er krysset av |
   | `comment-says-done` | middels/svak | Siste kommentar sier at den er løst (middels), eller spør om det (svak) |
   | `merged-referencing-pr` | svak | Mergede PR-er nevner saken, uten å lukke den |
   | `open-pr`, `reopened` | mot | Koblet PR er fortsatt åpen, eller saken er gjenåpnet etter siste merge |
   | `comment-says-not-done` | mot | Siste kommentar sier at noe gjenstår eller fortsatt feiler (`notDoneWords`) |

   `strength: "strong"` betyr minst ett sterkt signal og ingen motsignaler. `"possible"` betyr minst ett middels eller sterkt signal, men ikke sikkert. Saker med bare svake signaler er utelatt og bare telt.

3. **Vurder kandidatene.**
   - **Sterke** tas med. Er det eneste signalet at «1 av 1» sub-issue er ferdig, skriv det, slik at leseren ser at grunnlaget er tynt.
   - **Mulige:** les `excerpt` på kommentarsignaler. Forkast kandidaten om utdraget tydelig ikke handler om at saken er ferdig (det siterer en knappetekst, beskriver en betingelse, eller sier at en annen ting er fikset). Sier det «for det meste» eller «noe av dette», ta den med som delvis ferdig. Er du i tvil, les saken med `gh issue view <nr> -R <repo> --comments`.
   - Motsignaler nevnes alltid, f.eks. «gjenåpnet etter merge».

4. **Skriv rapporten** etter malen under, til en fil i temp-mappen, f.eks. `lukkekandidater-2026-10-16.md`, og gjør den klar til innliming med skriptet fra ukesrapport:

   ```bash
   node .agents/skills/ukesrapport/scripts/render.mjs <sti-til-rapporten.md>
   ```

   På Windows legges den på utklippstavlen som formatert tekst (Ctrl+V i Slack eller Teams); ellers åpnes en HTML-fil å kopiere fra. Vis rapporten i svaret og si hvilke kandidater du forkastet og hvorfor.

## Mal

```text
_Utkast fra lukkekandidater-skillen (AI-assistert). Ikke gjennomgått ennå._

**Lukkekandidater – åpne issues som ser ferdige ut (16. oktober 2026)**

**Kort fortalt**
2–3 setninger: hvor mange åpne issues som er gjennomgått og i hvilke repoer, hvor mange som ser ferdige ut, og hvor mange som bør sjekkes. Si at dette er forslag og at ingenting er lukket.

**Ser ferdige ut**

**Tilgangsstyring**
- [#2891](lenke) Sammenkobling av e-postbruker og A2-brukere – alle 5 sub-issues er ferdige
- ...

**Bør sjekkes**
- [#4521](lenke) Fjerne SI-bruker pålogging – siste kommentar sier den er for det meste løst
- [#3106](lenke) Kverke DelegationChange Function App – lukkende PR merget og status Done, men gjenåpnet etter merge
- ...

**Tall**
N åpne issues gjennomgått i K repoer. X ser ferdige ut, Y bør sjekkes, Z hadde bare svake signaler og er ikke med.
```

Detaljer:

- **Ser ferdige ut** grupperes per område etter første område i `areas`, i rekkefølgen `config.areaOrder` og med navn fra `config.areaNames`. Saker uten område samles under «Uten område» til slutt.
- **Bør sjekkes** er én liste sortert etter `score`, uten gruppering. Over 15 saker: vis de 15 med høyest score og skriv «+N til». Tilby hele listen i chatten.
- **Hvert punkt:** lenke, tittelen (kortet ned og ryddet, uten prefikser som «AccMgmt |»), og grunnen i noen få ord. Bruk bare `**fet**`, `_kursiv_`, `[tekst](url)` og `- `, som `render.mjs` gjør om til formatert tekst.

## Vedlikehold

- **Repoer, områder, ferdig-statuser, ferdig-ord og gjenstår-ord** står i [`config.json`](config.json). Statuser sammenlignes uten emoji og store bokstaver, så «✅ Done» treffer `done`.
- **Gir et ord mange falske treff**, fjern det fra `doneWords`. Ord som står rett etter «ikke», «når», «skal», «will», «when» og lignende teller ikke.
- Claude Code finner skillen via pekeren i `.claude/skills/lukkekandidater/SKILL.md`. Endre innholdet her, ikke der.
