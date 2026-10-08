# Altinn.AccessMgmt.FFB — konvensjoner

Blazor Server-admin-verktøy (MudBlazor) mot AccessMgmt/Register-databasene per miljø.
Full arkitektur og oppskrifter: se `README.md` i denne mappen. Bygg:
`dotnet build src/Altinn.AccessMgmt.FFB` (ingen testprosjekt — verifiser manuelt).

## Ufravikelige mønstre

- **Tynne sider.** All DB-/forretningslogikk ligger i tjenester (`Services/Tools`,
  `Services/PageData`), aldri i `@code`-blokker. Sidene holder kun UI-state.
- **Tjenestesignatur:** `Task<Xxx> MethodAsync(string environment, ..., CancellationToken ct = default)`
  — `environment` først, `ct` sist, utpakkede parametere (ikke request-objekter). Tjenesten
  oppretter egen `AppDbContext` per kall via `IEnvironmentDbContextFactory` og kaster ved feil.
  Aldri del én context mellom parallelle tasks.
- **Auto-DI:** `*Service`-klasser i `Services/Tools` og `Services/PageData` registreres
  automatisk som singletons (`ServiceCollectionExtensions.AddPageServices`). Ikke legg til
  `AddSingleton`-linjer i `Program.cs` for disse.
- **Sider arver `EnvironmentPageBase`:** bruk `RunAsync(...)` for arbeid (`Loading`/`Error`
  håndteres, forrige kjøring kanselleres), og overstyr `OnEnvironmentChangedAsync`
  (detaljsider relaster, verktøysider tømmer resultater + `Error = null`). Overrides av
  `OnInitialized`/`Dispose` må kalle `base.`-varianten.
- **Navigasjon:** nye verktøy-/jobbsider registreres i `Components/Layout/NavRegistry.cs`
  (én oppføring → meny + Hjem). Tilbake-navigasjon er alltid `<BackButton />` (browser-historikk);
  dyplenkbare sider setter `FallbackHref` til naturlig foreldreside så knappen virker uten
  historikk. Detaljsider lenkes inn med `MudLink`.
- **Jobber:** statiske klasser i `Jobs/` med `JobName`-konstant og options-record; orkestreres
  av `IJobRunner`. Kjørenavn-grammatikk: preview = `{JobName}`, execute = `{JobName}:Execute`,
  variant = `{JobName}:{filter}` (→ `JobRunList MatchByPrefix="true"`). Bruk alltid
  `JobTypes`-konstantene, aldri literale jobbtype-strenger. Destruktive steg går alltid
  gjennom `Confirm.ShowAsync`.
- **Constants-sjekker:** ny `CheckXxxAsync`-metode MÅ registreres i `checks`-listen i
  `ConstantsCheckService.RunAllChecksAsync`.
- **Feilvisning:** alltid `ErrorText.Flatten(ex)`, aldri bare `ex.Message`.

## Språk og stil

- UI-tekst (labels, meldinger, beskrivelser) på norsk. Kode, kommentarer og identifikatorer
  på engelsk.
- Knapper: primær = `Filled`/`Primary` (+`PlayArrow`), destruktiv = `Filled`/`Error` bak
  bekreftelse, per-rad = `Outlined`/`Small`.
- Ikke over-engineer: konkrete tjenesteklasser (ingen interfaces for én implementasjon),
  ingen repository-lag eller MediatR. Record + tjeneste deles gjerne i samme fil
  (`XxxDetailsService.cs`).
