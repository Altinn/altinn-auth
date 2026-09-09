using MudBlazor;

namespace Altinn.AccessMgmt.FFB.Components.Layout;

/// <summary>
/// Kind of page a nav entry points to. Tools and jobs render as separate
/// groups in the nav menu and as separate cards on the home page.
/// </summary>
public enum NavGroup
{
    Tool,
    Job,
}

/// <summary>
/// One navigable page: route, icon, menu title, and the description shown on the home page card.
/// </summary>
public sealed record NavEntry(string Href, string Icon, string Title, string Description, NavGroup Group);

/// <summary>
/// Single source of truth for the app's tool and job pages. NavMenu.razor and
/// Home.razor both render from this list, so one entry here puts the page in the
/// nav menu and on the home page. Details pages are not listed — they are reached
/// via links from search results and other pages.
/// </summary>
public static class NavRegistry
{
    public static readonly IReadOnlyList<NavEntry> All =
    [

        // ── Verktøy ───────────────────────────────────────────────────────────
        new("/entity-search", Icons.Material.Filled.ManageSearch, "Entity-søk",
            "Søk på entities via RefId, navn eller ID direkte mot databasen.", NavGroup.Tool),
        new("/tools/tables", Icons.Material.Filled.TableChart, "Tabeller",
            "Radantall og grupperingsanalyse for alle tabeller i skjemaet.", NavGroup.Tool),
        new("/tools/connection-query", Icons.Material.Filled.AccountTree, "Connection Query",
            "Kjør forhåndsdefinerte EF-spørringer på tvers av tabeller.", NavGroup.Tool),
        new("/tools/outbox", Icons.Material.Filled.Outbox, "Outbox",
            "Overvåk outbox-meldinger — se pending, prosesserte og feilede.", NavGroup.Tool),
        new("/tools/constants-check", Icons.Material.Filled.FactCheck, "Constants Check",
            "Sammenligner hardkodede konstanter i kode mot faktisk DB-innhold. Fikser avvik med INSERT/UPDATE/DELETE.", NavGroup.Tool),
        new("/tools/resource-cleanup", Icons.Material.Filled.DeleteSweep, "Resource Cleanup",
            "Finn og rydd opp foreldreløse resource-rader basert på et RefId-prefix.", NavGroup.Tool),
        new("/tools/grant-check", Icons.Material.Filled.Key, "Grant Check",
            "Sjekker at DB-rollene har riktige privilegier på alle tabellene i modellen. Kjører GRANT for manglende.", NavGroup.Tool),

        // ── Jobber ────────────────────────────────────────────────────────────
        new("/jobs/runs", Icons.Material.Filled.PlayCircle, "Kjøringer",
            "Sanntidsoversikt over alle aktive og avsluttede jobbkjøringer.", NavGroup.Job),
        new("/jobs/assignment-sync", Icons.Material.Filled.CompareArrows, "Assignment Sync",
            "To-pass diff mellom AccessMgmt og Register. Genererer INSERT/DELETE SQL for assignments.", NavGroup.Job),
        new("/jobs/entity-sync", Icons.Material.Filled.People, "Entity Sync",
            "To-pass diff for entities. Genererer korrigeringsskript mot Register.", NavGroup.Job),
        new("/jobs/ingest-cleanup", Icons.Material.Filled.CleaningServices, "Ingest Cleanup",
            "Identifiserer stale tabeller i ingest-skjemaet. Genererer og kan kjøre DROP-statements.", NavGroup.Job),
        new("/jobs/history-cleanup", Icons.Material.Filled.History, "History Cleanup",
            "Slår sammen og rydder utløpte historikkrader (audit_validto IS NOT NULL).", NavGroup.Job),
        new("/jobs/assignment-instance-cleanup", Icons.Material.Filled.SwapHoriz, "Assignment Instance Cleanup",
            "Flytter AssignmentInstance-rader til app-styrte rettighetshaver-forhold. Destruktiv engangsjobb uten forhåndsvisning.", NavGroup.Job),
        new("/jobs/schedules", Icons.Material.Filled.Schedule, "Planlagte kjøringer",
            "Cron-baserte planer konfigurert i appsettings.json. Kjør manuelt eller se neste tidspunkt.", NavGroup.Job),
    ];
}
