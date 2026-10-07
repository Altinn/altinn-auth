# Aktivitetslogg — teknisk beskrivelse

## Løsningsoversikt

Aktivitetsloggen er en hendelseslogg over alle tilgangsendringer i Access Management — tildelinger, delegeringer og forespørsler, inkludert pakke-, ressurs- og instansnivå. Den svarer på «hvem gjorde hva med tilgangene mine, når» for sluttbrukere, support og interne verktøy.

### Hvordan hendelsene skrives

23 AFTER-triggere på de 10 kildetabellene skriver hendelser i samme transaksjon som endringen selv — ingen applikasjonskode kan glemme å logge, og backend-jobber, import og synk fanges også. Attribusjonen gjenbruker audit-mekanismen som allerede står i produksjon: INSERT leser radens egne audit-kolonner, UPDATE leser sesjonsvariabler satt av EF ved lagring, DELETE leser audit-konteksten fra sesjonens temp-tabell. Navnesnapshots slås opp fra live-tabellene med fallback til historikkskjemaet når foreldre-raden allerede er slettet (kaskader). Feiler triggeren, rulles hele endringen tilbake — samme garanti som audit. All trigger-DDL versjoneres gjennom EF-migreringene via den samme SQL-generatoren som lager audit-triggerne.

### Lagring og partisjonering

Én range-partisjonert tabell (`dbo.activitylog`) på hendelsestidspunktet, med denormaliserte navnesnapshots (fra/til/via/utført av, rolle, pakke/ressurs) — spørringer trenger ingen joins, og navnene leses slik de var da hendelsen skjedde. Operasjons-id og parent-id lagres på radene for fremtidig gruppering av hendelser.

Partisjonsoppsettet har tre deler:

- **Årlige partisjoner 2000–2025** bærer backfillet historikk. Disse dataene skrives én gang av backfillen og endres aldri, og historiske spørringer er sjeldne — månedlig granularitet her ville gitt 300+ partisjoner uten gevinst; årlig gir 26.
- **Månedlige partisjoner fra 2026-01** bærer levende data. De fleste spørringer treffer nyere tidsintervaller, så partition pruning holder dem på noen få små partisjoner, indeksene per partisjon forblir små, og fremtidig arkivering kan detache én måned om gangen.
- **En default-partisjon** fanger alt utenfor de opprettede intervallene, slik at en forretningstransaksjon aldri kan feile på manglende partisjon.

Migreringen oppretter alt dette på forhånd, inkludert månedlige partisjoner 24 måneder frem. Løpende vedlikehold er funksjonen `dbo.activitylog_ensure_month_partitions(months_ahead)` (SECURITY DEFINER, kjørbar av app-rollen), som oppretter manglende måneder frem til horisonten. FFB-jobben `ActivityLogPartitions` kaller den: den kan kjøres manuelt fra backfill-siden eller settes opp som gjentakende via FFB-jobbplanleggeren, og den *oppretter* bare partisjoner. Retention er ubegrenset inntil videre, så det finnes bevisst ingen opprydnings-/detach-jobb — den kommer sammen med en retention-beslutning. Skulle vedlikeholdet bli glemt, lander skrivinger bare i default-partisjonen (ingenting feiler), men de månedene må da flyttes ut av den manuelt før partisjonene deres kan opprettes — derfor bør jobben stå på en plan i god tid før 24-månedersbufferen løper ut.

### Hendelsestypekatalogen

35 hendelsestyper (kombinasjoner av type/subtype/hendelse/status) med navn og beskrivelse på bokmål, nynorsk og engelsk, definert i kodekonstanter og seedet inn i en hjelpetabell (`dbo.activitytype`). Loggen beholder sine fire rå dimensjonskolonner; katalogen er et rent oppslagslag — typedefinisjoner kan endres uten å røre en tabell med ~86 millioner rader.

### Historikk og utrulling

En backfill-jobb genererer hendelser fra eksisterende data, med en read-only analysemodus først (PROD ≈ 86,6 millioner hendelser, omtrent 50–80 GB — håndterbart). Et manuelt install-/rollback-skript installerer hele skjemaet i testmiljøer uten EF-migreringsbokføring, slik at triggere, analyse og backfill kan prøves i stor skala før den ekte migreringen shippes; vakter avviser begge operasjonene i ethvert EF-forvaltet miljø.

### Dogfooding

FFB-verktøyet har en komplett side mot alle miljøer — party-anker med retning, hendelsestype-velger (tre med kaskade-valg og beskrivelser), filtervelgere drevet av filterverdi-endepunktene, og gruppering på operasjon/parent. Det er her UX-flyten frontend skal bygge er verifisert.

### Vurderte alternativer

- **Logging fra applikasjonskoden** — forkastet: mange skriveveier (API-er, jobber, import, A2-synk) gjør det lett å miste hendelser, og loggingen skjer utenfor transaksjonen.
- **Outbox/event-basert** — forkastet: eventual consistency og flere bevegelige deler, og driftserfaringen viser allerede at outbox-løsningen er skjør. (Loggen vurderes faktisk som nytt datagrunnlag for varsling, ikke omvendt.)
- **Avlede loggen fra audit-/historikktabellene ved lesing** — forkastet: ville krevd temporale joins over 10+ tabeller per spørring over ~86 millioner hendelser, uten stabile hendelsesbegreper og uten navnesnapshots.
- **CDC/logisk replikering** — forkastet: ny infrastruktur, forsinkelse, og berikelseslaget ville trengtes uansett.

**Hvorfor triggere vant:** komplett (fanger alle skriveveier, også manuelle), atomisk (samme transaksjon, samme garanti som audit), korrekte snapshots på hendelsestidspunktet, én lese-optimalisert tabell uten joins — og null ny infrastruktur, siden mønsteret (trigger-DDL i migreringer, audit-attribusjon) allerede er bevist i dette skjemaet.

## Sluttbruker-API (v2-områder)

Sluttbrukerflaten er delt i tre **områder**, hvert montert under sin domenerot i v2-API-et (`Controllers/V2`, `accessmanagement/api/v{version}/enduser/…`). Området er autorisasjonsgrensen og avgjør hvilken del av loggen som finnes der — det er ingen typefilter på tvers av områder:

| Områderot | Hendelser | Feature-flagg | Autorisasjon |
|---|---|---|---|
| `…/enduser/connections/activitylog` | Assignment + Delegation (maskinportenschema-hendelser ekskludert) | `AccessManagement.Enduser.ConnectionsActivityLogApi` | Connections-endepunktenes egne policies (betingede retningsscopes + personregelen for tilgangsstyrere) + tilgangsstyring-les for party |
| `…/enduser/request/activitylog` | Tilgangsforespørsler inkl. pakke-/tjenestebarn og statusendringer | `AccessManagement.Enduser.RequestActivityLogApi` | De eksisterende requests-lesescopene + tilgangsstyring-les for party, som naboendepunktene for forespørsler |
| `…/enduser/maskinporten/activitylog` | Maskinportenschema-delegeringer (Supplier-rolle-utsnittet) | `AccessManagement.Enduser.MaskinportenActivityLogApi` | Maskinporten supplier-scopene, som naboendepunktene |

Hvert område eksponerer de samme fire rutene:

| Rute | Formål |
|---|---|
| `GET {area}` | Områdets logghendelser, nyeste først |
| `GET {area}/filter/{field}` | Verdiene som forekommer i partyens utsnitt for ett filterfelt |
| `GET {area}/filter/fields` | Filterfeltene området tilbyr (statisk, anonym, cachet 1 t) |
| `GET {area}/types` | Katalogoppføringene området tar imot som `typeId`-input (statisk, anonym, cachet 1 t) |

### 1. Hovedspørring — `GET {area}`

Returnerer områdets logghendelser for partyen, sortert nyeste først (`when` synkende, id som tiebreaker).

**Forankring:** `party` er **påkrevd**, og spørringen må forankres på samme måte som connections-endepunktene: `from=party` (gitt) eller `to=party` (mottatt); den andre av de to forblir et motpartsfilter. De betingede retningsscopene og personregelen for tilgangsstyrere nøkler på nøyaktig disse rå parameterne — det er det som lar områdene gjenbruke nabopoliciene uendret. Det finnes ingen `direction`-parameter — from/to-ankeret er retningen — og `via`-forankring kommer tilbake med klientadministrasjonsbehovet.

**Filtre** (alle kan gjentas; verdier innenfor én parameter OR-es, ulike parametere AND-es):

| Parameter | Type | Matcher |
|---|---|---|
| `typeId` | guid | Oppføringer i hendelsestypekatalogen — se ekspansjonsreglene under |
| `type` / `subtype` / `trigger` / `status` | enum | De rå dimensjonskolonnene |
| `from` / `to` / `via` / `by` / `role` / `package` / `resource` | guid | Tilhørende id-kolonne |
| `source` | guid | Systemet/kanalen endringen kom gjennom |
| `operation` | string | Operasjons-id (trace) — alle hendelser skrevet av én brukerhandling |
| `instance` | string | Instans-URN |
| `itemId` / `parentId` | guid | Den berørte raden / dens hovedrad |
| `after` / `before` | datetime | Grenser på `when` |

**typeId-ekspansjon:** hver `typeId` peker på én katalogoppføring og ekspanderes til hele `(type, subtype, trigger, status)`-kombinasjonen; flere verdier OR-es som komplette kombinasjoner. En katalogoppføring med `subtype = null` matcher bare hovedrad-hendelser (eksakt null-match), mens `status = null` er et wildcard som matcher enhver status. Ukjente id-er — og id-er utenfor områdets aksepterte sett (`GET {area}/types`) — gir `400`.

**Paging:** sidebasert via `pageSize` (default 100, begrenset til 1–1000) og `pageNo` (0-basert). Responsen er den standard paginerte konvolutten: elementene pluss `links.next`, en ferdig URL som bare finnes når det er flere hendelser (bygget fra requesten med `pageNo` inkrementert).

**Responselementer** (`ActivityLogDto`): hendelsesdimensjonene (`type`, `subtype`, `trigger`, `status`), `when`, aktør og kanal (`byId`/`byName`, `sourceId`/`sourceName`), `operationId`, relasjonen med navnesnapshots (`fromId`/`fromName`/`fromType`, `toId`/`toName`/`toType`, `viaId`/`viaName`/`viaType`, `roleId`/`roleName`, `viaRoleId`/`viaRoleName`), objektet (`packageId`/`packageName`, `resourceId`/`resourceName`, `instanceId`), radidentitet (`itemId`, `parentId`), en `details`-JSON (forrige status, forespørselshandling, proveniens), og `activityTypeId` — katalogoppføringen slått opp med mest-spesifikk-vinner-regelen (eksakt statusmatch, ellers status-null-fallbacken), slik at klienter kan vise katalognavn/-beskrivelse uten å mappe rådimensjonene selv.

```
GET /accessmanagement/api/v2/enduser/connections/activitylog?party={party}&from={party}&typeId={guid}&after=2026-01-01T00:00:00Z&pageSize=50
```

### 2. Filterverdi-endepunktene — `GET {area}/filter/{field}`

*(De returnerer de distinkte verdiene som forekommer i loggen, avgrenset til gjeldende søk.)*

Returnerer de distinkte `(id, name)`-parene som forekommer i partyens utsnitt av området for ett felt, slik at filtervelgere bare tilbyr verdier som faktisk gir treff. `field` må være blant feltene området tilbyr (`GET {area}/filter/fields`) — maskinporten-området tilbyr f.eks. ikke `role`, siden Supplier-rollen er låst; alt annet gir `400`.

- **Samme filterflate som hovedspørringen** — party, from/to-ankeret og alle filterparametere gjelder, så velgeren snevres inn sammen med søket brukeren allerede har bygget.
- **Eget-felt-regelen:** filteret for feltet som slås opp ignoreres (et oppslag på `package` ser bort fra ethvert `package`-filter), slik at brukere kan utvide et flervalg uten at listen kollapser til det de alt har valgt. `party`-ankeret ignoreres aldri.
- **`term`:** case-insensitivt delstrengsøk kun på navn.
- **`orderBy`:** `Name` (default, alfabetisk — stabil på tvers av sider) eller `When` (nyeste forekomst per verdi først — nye hendelser kan forskyve sider).
- **Duplikater er tilsiktet:** navn er snapshots fra hendelsestidspunktet, så samme id kan gjenta seg med ulike navn (f.eks. etter navnebytte); alle par returneres slik at hver historisk etikett er søkbar. For `source` og `activitytype` kommer navnene fra de respektive katalogene i stedet for snapshots.
- **Språk:** `activitytype`-verdier oversettes fra katalogen (bokmål, nynorsk, engelsk via `Accept-Language`) før term-matching og sortering, slik at termen matcher navnene brukeren faktisk ser. Snapshot-navn er data og oversettes aldri.
- **Supplier-rollen dukker aldri opp som verdi:** maskinportenschema-hendelser når aldri de vanlige flatene (se tverrgående oppførsel), så rollen deres tilbys heller ikke.
- **Paging og konvolutt:** identisk med hovedspørringen (`pageSize`/`pageNo`, `links.next`).

```
GET /accessmanagement/api/v2/enduser/connections/activitylog/filter/package?party={party}&from={party}&term=skatt&pageSize=20
```

### 3. Områdediscovery — `GET {area}/filter/fields` og `GET {area}/types`

`filter/fields` lister filterfeltene området tilbyr (de gyldige verdiene for filterruten), og `types` lister katalogoppføringene området tar imot som `typeId`-input — `ActivityTypeDto` med `id`, nøkkelfeltene (`type`, `subtype`, `trigger`, `status`), `name` og `description`. Maskinporten-utsnittet kan ikke avledes fra den globale katalogen (Supplier-hendelser deler assignment-typen), og derfor serverer hvert område sin egen aksepterte delmengde.

Begge er statisk metadata uten persondata, derfor anonyme og respons-cachet (1 time, alle lokasjoner; `types` varierer på `Accept-Language`). `types` serverer `name` og `description` på forespurt språk — bokmål, nynorsk eller engelsk, oversatt i minnet fra de samme konstantlistene som ingest skriver til oversettelsestabellen. Innholdet endres bare ved deploy; kilden er `ActivityTypeConstants`, som også seeder hjelpetabellen `dbo.activitytype`.

```
GET /accessmanagement/api/v2/enduser/maskinporten/activitylog/types
```

### Tverrgående oppførsel

- **Maskinportenschema-hendelser finnes bare i maskinporten-området, og de to utsnittene blandes aldri:** Supplier-rollen brukes utelukkende for maskinportenschema-delegeringer, og selve servicen har to disjunkte flater — den vanlige ekskluderer alltid rollen (connections og request), maskinporten-flaten serverer kun den. Det finnes ingen opt-in; ingen kallerkombinasjon kan gi et blandet resultat.
- **Validering:** tom `party`, manglende from/to-forankring, oppgitt `direction`, `typeId`-verdier utenfor området, og filterfelt området ikke tilbyr gir alle `400` med problem details. Merk at scope-policyen på connections-området nøkler på de samme forankringsparameterne, så en uforankret spørring avvises der med `403` før valideringen kjører — samme karakteristikk som connections-endepunktene selv har.
- **Feature-flagg:** ett per område — `AccessManagement.Enduser.{Connections|Request|Maskinporten}ActivityLogApi` — alle deklarert i appens egen terraform sammen med de andre AccessManagement-flaggene (opprettes avslått; togles per miljø i App Configuration). Portal-frontenden bruker de samme områdeendepunktene; det finnes ingen egen intern flate.
- **Sorteringsgaranti:** `(when desc, id desc)` — stabil og duplikatfri på tvers av sider under paging.
- **Ingen joins ved lesing:** hvert navn i responsen er et denormalisert snapshot fra selve loggtabellen; loggen serveres fra én range-partisjonert tabell.
