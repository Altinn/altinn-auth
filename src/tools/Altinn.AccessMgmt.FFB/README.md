# Altinn.AccessMgmt.FFB

Internt administrasjonsverktøy for Altinn 3 Access Management. Blazor Server + MudBlazor,
med direkte tilgang til AccessMgmt- og Register-databasene per miljø (PostgreSQL).

```
dotnet run --project src/Altinn.AccessMgmt.FFB
```

Miljøer konfigureres i `appsettings.json` under `Environments[]` (`Name`, `AccessMgmt`,
`Register`, `SystemAccountId`). Ukonfigurerte miljøer vises deaktivert i UI. Ved single-file
publish leses `appsettings.json` fra mappen ved siden av exe-fila.

## Arkitektur

| Mappe | Innhold |
|---|---|
| `Components/Pages` | Tynne sider: markup + UI-state. All DB-logikk ligger i tjenester. |
| `Components/Shared` | `EnvironmentPageBase`, `BackButton`, `SummaryCard`/`SummaryRow`, `EnvironmentMultiSelect`, `Confirm`/`ConfirmDialog`, `EntityLookupField`/`EntityLookupDrawer`, `JsonTree` |
| `Components/Jobs` | `JobRunList`, `JobRunCard` |
| `Components/Layout` | `MainLayout`, `NavMenu`, `NavRegistry` (én kilde for meny + Hjem-kort) |
| `Services/PageData` | Én tjeneste per side (spørringer). Auto-registrert i DI. |
| `Services/Tools` | Verktøytjenester + `TableRegistry`. Auto-registrert i DI. |
| `Services` | Infrastruktur: `EnvironmentState`, `EnvironmentDbContextFactory`, `ErrorText`, DI-scan |
| `Jobs` | Jobbklasser, `JobRunner`, `DuoRepo` (Dapper mot AccessMgmt/Register), `JobModels` |
| `Config` | `EnvironmentsConfig`, `JobSchedulesConfig` (`JobTypes` + `Scheduled*Options`), `NotificationsConfig` |

### Kjerneprinsipper

1. **Sidene er tynne.** DB- og forretningslogikk ligger i tjenester; siden holder kun UI-state.
2. **Uniform tjenestesignatur:** `Task<Xxx> MethodAsync(string environment, ..., CancellationToken ct = default)`.
   Tjenesten oppretter og disposer sin egen `AppDbContext` per metodekall via
   `IEnvironmentDbContextFactory`, og kaster ved feil (siden fanger via `RunAsync`).
   Del aldri én context mellom parallelle tasks — én context per task.
3. **`EnvironmentPageBase`** gir `Loading`/`Error`-state og `RunAsync(work)`: setter Loading,
   fanger exceptions til `Error` (via `ErrorText.Flatten`), kansellerer forrige kjøring og
   hindrer at en foreldet lasting overskriver nyere data. Overstyr `OnEnvironmentChangedAsync`
   — detaljsider relaster, verktøysider tømmer resultater. Overrides av `OnInitialized`/`Dispose`
   må kalle `base.`-varianten.
4. **Auto-DI:** alle `*Service`-klasser i `Services/Tools` og `Services/PageData` registreres
   automatisk som singletons (`ServiceCollectionExtensions.AddPageServices`). Ingen `Program.cs`-endring.
5. **`NavRegistry`:** én oppføring legger siden i både nav-menyen og Hjem-kortene.
6. **Språk:** UI-tekst på norsk, all kode/kommentarer/identifikatorer på engelsk.
7. **Knapper:** primær handling = `Filled`/`Primary` med `PlayArrow`; destruktiv = `Filled`/`Error`
   og alltid bak `Confirm.ShowAsync`; per-rad-fix = `Outlined`/`Small`.

## Oppskrifter

### Nytt verktøy

1. `Services/Tools/MinSideService.cs` — auto-registrert:
   ```csharp
   public sealed class MinSideService(IEnvironmentDbContextFactory dbFactory)
   {
       public async Task<MinResult> RunAsync(string environment, ..., CancellationToken ct = default)
       {
           using var db = dbFactory.CreateContext(environment);
           ...
       }
   }
   ```
2. `Components/Pages/Tools/MinSide.razor`:
   ```razor
   @page "/tools/min-side"
   @inherits EnvironmentPageBase
   @inject MinSideService Service

   private async Task Run() => await RunAsync(async ct =>
       _result = await Service.RunAsync(EnvState.Current, ..., ct));

   protected override Task OnEnvironmentChangedAsync()
   {
       _result = null;   // results belong to the previous environment
       Error = null;
       return Task.CompletedTask;
   }
   ```
3. Én oppføring i `Components/Layout/NavRegistry.cs` → siden vises i meny og på Hjem.

### Ny detaljside

1. `Services/PageData/XxxDetailsService.cs` — record + tjeneste, auto-registrert:
   ```csharp
   public sealed record XxxDetailsData(Xxx Xxx, IReadOnlyList<Related> Related);

   public sealed class XxxDetailsService(IEnvironmentDbContextFactory dbFactory)
   {
       // null when the id does not exist in the environment
       public async Task<XxxDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default) { ... }
   }
   ```
2. `Components/Pages/XxxDetails.razor` — fast `@code`-mal:
   ```csharp
   [Parameter] public Guid Id { get; set; }
   private XxxDetailsData? _data;

   protected override Task OnParametersSetAsync() => RunAsync(LoadAsync);
   protected override Task OnEnvironmentChangedAsync() => RunAsync(LoadAsync);
   private async Task LoadAsync(CancellationToken ct) => _data = await DataService.GetAsync(EnvState.Current, Id, ct);
   ```
   Grener: `Loading` → `Error` → `_data is null` («not found») → innhold.
   Header: `<BackButton />` + tittel; summary med `<SummaryCard>`/`<SummaryRow>`.
3. Detaljsider ligger ikke i menyen — legg `MudLink Href="/xxx/{id}"` fra sidene som skal lenke inn.

Glem ikke `OnEnvironmentChangedAsync` — uten den vises forrige miljøs data etter miljøbytte.

### Ny bakgrunnsjobb

Uten planlegging:

1. `Jobs/MinNyJob.cs` — statisk klasse med `public const string JobName = "MinNy";` og
   `RunAsync(DuoRepo repo, JobRun run, MinNyOptions opts, CancellationToken ct)`.
   Options-record ligger nederst i samme fil.
2. `StartMinNy(...)`-metode på `IJobRunner` **og** implementasjon i `Jobs/JobRunner.cs`
   (`CreateRun` + `FireAndForget`). En eventuell execute-fase bruker navnet `"{JobName}:Execute"`.
3. `Components/Pages/Jobs/MinNyJob.razor` — bruk `AssignmentSync.razor` som mal
   (`EntitySync` er den enkleste, uten options): `EnvironmentMultiSelect` med `@bind-Selection`,
   options-skjema, `<JobRunList @ref="_runList" JobName="@MinNyJob.JobName">` (kall
   `_runList.Refresh()` etter hvert `Start*`-kall), `Confirm.ShowAsync` før destruktive steg.
4. Oppføring i `NavRegistry`.

Med planlegging (alle i tillegg):

5. `JobTypes`-konstant i `Config/JobSchedulesConfig.cs` — bruk konstanten (aldri literal streng)
   både i scheduler og i sidens `OpenScheduleDialog`.
6. `ScheduledMinNyOptions`-klasse + `MinNyOptions`-property på `JobScheduleEntry` (samme fil).
7. `case` i `FireSchedule`-switchen + `FireMinNy`-mapper i `Services/JobSchedulerService.cs`,
   og legg konstanten til i `LogUnknownJobType`-listen.

Merk: scheduleren kjører kun preview-fasen (genererer SQL, kjører aldri execute automatisk),
og en glemt `FireSchedule`-case gjør at jobben aldri fyrer — eneste spor er en logglinje.

**Kjørenavn-grammatikken er lastbærende:**

| Kjøring | Navn i `JobRunStore` |
|---|---|
| Preview | `{JobName}` |
| Execute | `{JobName}:Execute` |
| Variant-navngitt preview (f.eks. IngestCleanup) | `{JobName}:{filter}` |
| Variant-navngitt execute (multi-runner) | `{JobName}:{filter}:Execute [i/n]` |

`JobRunList` matcher default eksakt navn + `:Execute`; sett `MatchByPrefix="true"` for
variant-navngitte jobber og `IncludeExecuteRuns="false"` for én-fase-jobber. `ExtraActions`-
predikatene på sidene må følge samme grammatikk (se `IngestCleanup.razor` for prefiks-varianten).

### Ny constants-sjekk

1. Skriv en privat `CheckXxxAsync(string environment, CancellationToken ct)`-metode i
   `Services/Tools/ConstantsCheckService.cs` (kopier en eksisterende). `allowDeleteExtra` og
   delete-fix-løkken hører sammen: `true` bare for tabeller som er 100 % konstant-styrte.
2. Registrer metoden i `checks`-listen i `RunAllChecksAsync` — en debug-assert fanger glemte
   registreringer. UI-en rendrer resultatet automatisk.

### Ny tabell i tabelloversikten

Én entry i `Services/Tools/TableRegistry.cs`. To feller: en ny `Category` må også inn i
`Categories`-listen (ellers vises ikke tabellen i oversikten), og `AuditKey` må peke på en
egen registry-entry med den nøkkelen.

## Planlagte kjøringer og varsler

- **Planer:** `JobSchedules.Enabled` er global kill-switch; planene ligger under
  `JobSchedules.Schedules`. Bruk «Generer konfig»-knappen på jobbsiden for et ferdig JSON-snippet.
- **Telegram:** `Notifications.BotToken` + `.ChatId`; varsler ved jobstart, fullføring og feil.

## Bygg og verifisering

```
dotnet build src/Altinn.AccessMgmt.FFB
```

Det finnes ikke noe testprosjekt — verifiser manuelt mot et dev-miljø. Sjekk alltid at
miljøbytte oppfører seg riktig på nye sider (relast eller tøm), og at knapper aktiveres/
deaktiveres når kun miljøvalget endres.
