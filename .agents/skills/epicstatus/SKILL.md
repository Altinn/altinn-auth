---
name: epicstatus
description: Lager status for teamets epics og features til produkteier, ferdig til å limes inn i Slack eller Teams. Viser fremdrift, hva som ble ferdig siste to uker, hva som er blokkert, risiko og tempo per aktiv epic. Bruk når noen ber om epicstatus, status på epics eller features, fremdrift, roadmap-status eller PO-rapport. Bare rapport; endrer ingenting.
---

# Epicstatus for produkteier

Lager en rapport på bokmål over de store leveransene, epics og features, i teamets fire repoer. Den svarer på det produkteier spør om annenhver uke: hva beveger seg, hva står fast, hva er risikoen, og omtrent når blir det ferdig?

## Regler

- **Bare rapport.** Ikke lukk, flytt, merk eller kommenter, heller ikke om noen ber om det i samme forespørsel.
- **Oppgaver, ikke personer.** Ingen navn, GitHub-brukere, @-omtaler eller hvem som jobber med hva. Se [data-policyen](../../../docs/ai/data-policy.md).
- **Prognoser er grove anslag.** Skriv «ca. N uker med tempoet de siste fire ukene», aldri en dato. Har `forecastWeeks` verdien null, ikke lag en prognose. Den mangler når det er for få ferdige saker til å si noe.

## Steg

1. **Hent data.** Fra roten av repoet:

   ```bash
   node .agents/skills/epicstatus/scripts/collect.mjs
   ```

   Tar under ett minutt. Krever Node 20+ og `gh` med `project`-scope (`gh auth refresh -s project`) for status og Horizon fra boardet.

2. **Les JSON-filen.**
   - `active`: aktive epics og features, sortert med Horizon «Now» først, deretter flest ferdige siste periode. Aktiv betyr at den er på «Now», er i arbeid på boardet, har sub-issues i arbeid, eller har fått en sub-issue ferdig de siste `activeDays` dagene. En feature under en epic i lista vises under epicen (`features`), ikke for seg.
     - `leaves`: fremdrift talt på oppgavenivå, to nivåer ned. `total`, `completed`, `remaining`, `inWork`.
     - `completedInWindow`: oppgaver ferdige de siste `windowDays` dagene.
     - `weekly`: ferdige per uke de siste `tempoWeeks` ukene, eldste først. `perWeek` og `forecastWeeks` er utledet av den.
     - `blocked`: åpne oppgaver med «blocked by» eller `status/blocked`, med hva som blokkerer. `otherRepo: true` betyr et annet team eller repo.
     - `openBugs`, `openWithoutTypeOrArea`: risiko og rot.
     - `truncated`: mer enn 100 sub-issues, så tallene er ufullstendige.
   - `completed`: epics og features lukket som ferdige siste `windowDays` dager.
   - `quiet`: epics og features med sub-issues, men uten fremdrift siste `activeDays` dager, sortert med flest gjenstående først.

3. **Skriv rapporten** etter malen under, til en fil i temp-mappen, f.eks. `epicstatus-2026-10-19.md`, og gjør den klar til innliming:

   ```bash
   node .agents/skills/ukesrapport/scripts/render.mjs <sti-til-rapporten.md>
   ```

   På Windows havner den på utklippstavlen som formatert tekst (Ctrl+V i Slack eller Teams); ellers åpnes en HTML-fil å kopiere fra. Vis rapporten i svaret.

## Mal

```text
_Utkast fra epicstatus-skillen (AI-assistert). Ikke gjennomgått ennå._

**Epicstatus – Team Autorisasjon (19. oktober 2026)**

**Kort fortalt**
3–4 setninger for produkteier: hvilke leveranser som beveger seg, hva som ble fullført, hva som står fast og hvorfor, og den største risikoen.

**Fullført siste 14 dager**
- [#3561](lenke) Fullmaktsoversikt for enkelttjenester (4 av 4)
- ...

**Aktive**

**[Teknisk gjeld](lenke)** – 63 av 82 ferdige (77 %), 2 i arbeid
- Siste 14 dager: 2 ferdige, blant annet [#4385](lenke) bygg uten advarsler
- Blokkert: [#3522](lenke) venter på [#3786](lenke)
- Risiko: 1 åpen bug, 2 oppgaver uten type eller område
- Tempo: ca. 1 per uke, ca. 19 uker igjen med dette tempoet (grovt anslag)

**[Aktivitetslogg – backend](lenke)** – 5 av 6 ferdige (83 %)
- ...

**Står stille** (ingen fremdrift siste 30 dager)
- [#4510](lenke) Systembruker med og uten forhåndsdefinerte rettigheter – 0 av 17
- ...

**Tall**
N åpne epics og features med sub-issues. A aktive, S står stille, F fullført siste 14 dager.
```

Detaljer:

- **Aktive:** vis maks 8 med full blokk. Resten får én linje: tittel, «X av Y», og antall ferdige siste periode. Utelat linjer i en blokk som ikke har innhold, for eksempel «Blokkert» når ingenting er blokkert.
- **Fremdrift på 100 %**, men epicen er fortsatt åpen: skriv «alle oppgaver ferdige, kan lukkes?» i stedet for tempo.
- **Features under en epic:** nevn dem kort i epicens blokk, f.eks. «Features: Aktivitetslogg backend 5/6, utrulling 3/5».
- **Blokkeringer i andre repoer** er de viktigste for produkteier, fordi de krever kontakt med andre team. Nevn repoet.
- **Står stille:** maks 8, med flest gjenstående oppgaver først, og «+N til».
- **Titler:** kort ned og fjern prefikser som «[Epic]», «AccMgmt |», «Ny brukerflate |» når de ikke tilfører noe.
- Bruk bare `**fet**`, `_kursiv_`, `[tekst](url)` og `- `, som `render.mjs` gjør om til formatert tekst.

## Vedlikehold

- **Repoer, typer, board, statusnavn, vinduer og terskel for prognose** står i [`config.json`](config.json).
- **Når Target date og Size tas i bruk på boardet**, kan prognosen sammenlignes med måldato og vektes etter størrelse. Det er ikke laget ennå.
- Claude Code finner skillen via pekeren i `.claude/skills/epicstatus/SKILL.md`. Endre innholdet her, ikke der.
