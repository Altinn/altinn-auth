---
name: ukesrapport
description: Lager ukesrapport over hva autorisasjonsteamet har levert, ferdig til å limes inn i Slack eller Teams. Bruk når noen ber om ukesrapport, ukesoppsummering, «hva har vi levert denne uka» eller leveranser for en periode. Rapporten handler om oppgaver, aldri om personer.
---

# Ukesrapport for autorisasjonsteamet

Lager en rapport på bokmål over hva teamet har levert i inneværende uke, fra mandag 00:00 (Europe/Oslo) til nå. Den har to deler: et kort sammendrag for ledelse og interessenter, og teknisk detalj per område for teamet.

## Regel nummer én: oppgaver, ikke personer

Rapporten skal aldri si hvem som gjorde noe. Ingen navn, GitHub-brukere, @-omtaler, team-handles, «takk til» eller «X fikset Y». Skriv om leveransen: «Endepunkt for antall forespørsler på underenheter er på plass», ikke «NN la til endepunkt». Dette følger regelen i [data-policyen](../../../docs/ai/data-policy.md) om at måling er aggregert og aldri per person. Tall per person, per forfatter eller per reviewer skal heller ikke med, selv om noen ber om det i samme forespørsel; si da at skillen ikke lager det.

Skriptet henter ikke ut forfattere og fjerner @-omtaler fra titler. Leser du mer selv (issue-tekst, PR-beskrivelse), gjelder regelen fortsatt.

## Steg

1. **Hent data.** Fra roten av repoet:

   ```bash
   node .agents/skills/ukesrapport/scripts/collect.mjs
   ```

   Krever Node 20+ og `gh` innlogget med lesetilgang til repoene. Annen periode: `--from 2026-10-05 --to 2026-10-09` (datoer i Oslo-tid, `--to` er inklusiv). Skriptet skriver JSON til temp-mappen og skriver ut stien og en kort oppsummering.

2. **Les JSON-filen** og vurder hver leveranse (`deliveries`):
   - En leveranse er én oppgave: et issue og PR-ene som løste det er slått sammen. `title` er issue-tittelen når det finnes et issue, ellers PR-tittelen.
   - **Område:** `areas` med navn fra `config.areaNames`. Er `areaCertain` false, bruk ditt beste skjønn ut fra tittelen; finner du ikke et klart område, legg leveransen under «Annet». Er `crossCutting` true (repo-brede endringer som formatering, CI eller import), legg den under «På tvers av områder».
   - **`likelyBacklogCleanup` true** betyr en sak over et halvt år gammel, lukket uten PR i perioden og uten nylig endring som løste den. Det er som regel rydding i backlog, ikke ny leveranse. Ta dem ikke med som punkter; tell dem på én linje under Tall. Ser tittelen ut som noe som faktisk ble levert, sjekk med `gh issue view <nr> -R <repo>`.
   - **Saker lukket uten PR i perioden ellers** er ofte levert tidligere og lukket nå etter test; de teller som leveranser. `earlierChanges` viser PR-ene som løste dem.
   - Er tittelen for teknisk eller uklar til å si hva det betyr, les issuet med `gh issue view` før du skriver. Ikke finn på effekter som ikke står der.

3. **Skriv rapporten** etter malen under, til en fil ved siden av JSON-filen, f.eks. `ukesrapport-2026-W42.md`.

4. **Gjør den klar til innliming.** Slack og Teams viser ikke markdown som limes inn som ren tekst (`**fet**` blir stående med stjerner), men de beholder fet skrift, lenker og lister fra formatert tekst:

   ```bash
   node .agents/skills/ukesrapport/scripts/render.mjs <sti-til-rapporten.md>
   ```

   På Windows legger skriptet rapporten på utklippstavlen som formatert tekst, så den limes inn med Ctrl+V. Ellers åpner det en HTML-fil i nettleseren som brukeren kopierer fra (Ctrl+A, Ctrl+C). Si i svaret hvilken av delene som skjedde, vis rapporten, og si én linje om hva du var usikker på (områder du gjettet, saker du utelot som backlog-rydding).

## Mal

Middels lengde: alle områder med aktivitet, 2–5 punkter per område. Slå sammen småting til ett punkt («tre forbedringer i testoppsettet»). Rekkefølgen på områdene er `config.areaOrder`; områder uten leveranser utelates.

```text
_Utkast fra ukesrapport-skillen (AI-assistert). Ikke gjennomgått ennå._

**Ukesrapport autorisasjon – uke 42 (12.–16. oktober 2026)**

**Kort fortalt**
3–5 setninger i forretningsspråk: hva som nå er mulig eller bedre for innbyggere, virksomheter, tjenesteeiere og sluttbrukersystemer, og den viktigste interne fremdriften. Ingen saksnumre her.

**Levert per område**

**Tilgangsstyring**
- Hva som er levert, i én setning med effekten først ([#4321](lenke))
- ...

**Tilgangsstyring – brukerflate**
- ...

**På tvers av områder**
- ...

**Annet**
- ...

**Fremdrift på epics**
- Teknisk gjeld: 33 av 47 ferdige, 3 leveranser denne uka ([#154](lenke))
- ...

**Releaser**
- Altinn.Authorization.PEP 5.0.0 ([lenke](lenke))
- ...

**Tall**
N oppgaver levert, M PR-er merget, K releaser. X eldre saker lukket ved rydding i backlog.
```

Detaljer:

- **Overskrift:** uke fra `period.isoWeek`, datoer fra `period.from` til `period.to`. Er `period.partial` true, skriv «hittil denne uka» etter datoene.
- **Punkter:** start med hva som ble mulig eller rettet, ikke med verbet fra commit-meldingen. Oversett engelske PR-titler. Lenk til issue når det finnes, ellers til PR-en. Bruk bare `**fet**`, `_kursiv_`, `` `kode` ``, `[tekst](url)` og `- ` for punkter; det er det `render.mjs` gjør om til formatert tekst.
- **Epics:** fra `parents` der `type` er `Epic`, eller `Feature` med flere leveranser i perioden. Skriv `subIssues.completed` av `subIssues.total`, og `deliveriesInPeriod` denne uka. Er `closedInPeriod` true, skriv at den er fullført. Maks fem, de med flest leveranser i perioden først.
- **Releaser:** alt i `releases`. Releaser på samme dag fra samme repo kan slås sammen på én linje. Inntil `config.releases.deployTracked` er true, avslutt seksjonen med: «Hva som er i produksjon spores ikke ennå.»
- **Tall:** antall leveranser du tok med, `stats.prsMerged`, antall releaser og `stats.likelyBacklogCleanup`. Bot-PR-er, avhengighetsoppdateringer og saker lukket som «not planned» eller duplikat er ikke med og nevnes ikke.

## Vedlikehold

- **Repoer og områder** står i [`config.json`](config.json): `repos` (med `defaultArea` og `pathAreas` for å utlede område fra filstier), `areaOrder`, `areaNames` og `exclude`.
- **Releaser og prod-deploy:** i dag leses bare GitHub Releases, og rapporten sier at prod ikke spores. Når releaseoppsettet viser hva som er i produksjon, oppdater `releases` i config og seksjonen *Releaser* over, og sett `deployTracked` til true.
- **Når et repo flyttes inn i altinn-auth** (register, frontend), fjern det fra `repos` og legg stien under `pathAreas` for altinn-auth.
- Claude Code finner skillen via pekeren i `.claude/skills/ukesrapport/SKILL.md`. Endre innholdet her, ikke der.
