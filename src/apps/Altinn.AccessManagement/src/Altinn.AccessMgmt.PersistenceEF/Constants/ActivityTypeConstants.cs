using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines the constant <see cref="ActivityType"/> catalog: every valid activity log
/// combination with a display name and description. Resolution follows a most-specific-wins
/// rule via <see cref="Resolve"/>: an exact (type, subtype, trigger, status) match beats the
/// row with <c>Status = null</c>, which acts as the fallback for any status. Subtype
/// <c>null</c> is an exact match (entries about the main record), never a wildcard.
/// </summary>
public static class ActivityTypeConstants
{
    /// <summary>
    /// Try to get <see cref="ActivityType"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<ActivityType>? result)
        => ConstantLookup.TryGetById(typeof(ActivityTypeConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<ActivityType>> AllEntities()
        => ConstantLookup.AllEntities<ActivityType>(typeof(ActivityTypeConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<ActivityType>(typeof(ActivityTypeConstants));

    /// <summary>
    /// Resolves the catalog entry for an event: exact status match first, then the
    /// status-null fallback row. Returns <see langword="null"/> for combinations
    /// without a catalog entry.
    /// </summary>
    public static ConstantDefinition<ActivityType>? Resolve(ActivityLogType type, ActivityLogSubtype? subtype, ActivityLogTrigger trigger, RequestStatus? status)
    {
        if (status is not null && ByKey.Value.TryGetValue((type, subtype, trigger, status), out var exact))
        {
            return exact;
        }

        return ByKey.Value.TryGetValue((type, subtype, trigger, null), out var fallback) ? fallback : null;
    }

    private static readonly Lazy<IReadOnlyDictionary<(ActivityLogType, ActivityLogSubtype?, ActivityLogTrigger, RequestStatus?), ConstantDefinition<ActivityType>>> ByKey =
        new(() => AllEntities().ToDictionary(d => (d.Entity.Type, d.Entity.Subtype, d.Entity.Trigger, d.Entity.Status)));

    #region Assignment

    /// <summary>
    /// Assignment / Created — a role assignment between two parties was created.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentCreated { get; } = new ConstantDefinition<ActivityType>("228ceaa6-0213-4673-94d2-d12572869e99")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Trigger = ActivityLogTrigger.Created,
            Name = "Rolletildeling opprettet",
            Description = "En part fikk en rolle hos en annen part, f.eks. som rettighetshaver eller tilgangsstyrer.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Role assignment created"),
            KeyValuePair.Create("Description", "A party was given a role for another party, e.g. as rightholder or access manager.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rolletildeling oppretta"),
            KeyValuePair.Create("Description", "Ein part fekk ei rolle hos ein annan part, t.d. som rettshavar eller tilgangsstyrar.")),
    };

    /// <summary>
    /// Assignment / Deleted — a role assignment between two parties was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentDeleted { get; } = new ConstantDefinition<ActivityType>("1ad8e8b5-cd6e-49d6-9de8-6118dab071e7")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Rolletildeling fjernet",
            Description = "Rolletildelingen mellom partene ble fjernet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Role assignment removed"),
            KeyValuePair.Create("Description", "The role assignment between the parties was removed.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Rolletildeling fjerna"),
            KeyValuePair.Create("Description", "Rolletildelinga mellom partane vart fjerna.")),
    };

    /// <summary>
    /// Assignment / Package / Created — an access package was added to the assignment.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentPackageCreated { get; } = new ConstantDefinition<ActivityType>("928868d6-5c58-4815-be71-f944a7edcf02")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Created,
            Name = "Tilgangspakke tildelt",
            Description = "En tilgangspakke ble lagt til i rolletildelingen mellom partene.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package granted"),
            KeyValuePair.Create("Description", "An access package was added to the role assignment between the parties.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tilgangspakke tildelt"),
            KeyValuePair.Create("Description", "Ein tilgangspakke vart lagd til i rolletildelinga mellom partane.")),
    };

    /// <summary>
    /// Assignment / Package / Deleted — an access package was removed from the assignment.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentPackageDeleted { get; } = new ConstantDefinition<ActivityType>("43ec6f13-c464-4667-9b91-a21a385c1044")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Tilgangspakke fjernet",
            Description = "Tilgangspakken ble fjernet fra rolletildelingen.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package removed"),
            KeyValuePair.Create("Description", "The access package was removed from the role assignment.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tilgangspakke fjerna"),
            KeyValuePair.Create("Description", "Tilgangspakken vart fjerna frå rolletildelinga.")),
    };

    /// <summary>
    /// Assignment / Resource / Created — access to a single service was granted.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentResourceCreated { get; } = new ConstantDefinition<ActivityType>("cdb21c9c-efe9-4dd8-a4ff-05e6ed85b363")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Created,
            Name = "Enkelttjeneste tildelt",
            Description = "Det ble gitt tilgang til en enkelttjeneste.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service granted"),
            KeyValuePair.Create("Description", "Access to a single service was granted.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Enkelttjeneste tildelt"),
            KeyValuePair.Create("Description", "Det vart gitt tilgang til ei enkeltteneste.")),
    };

    /// <summary>
    /// Assignment / Resource / Deleted — access to a single service was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentResourceDeleted { get; } = new ConstantDefinition<ActivityType>("2cc78d26-16a1-4a88-95cd-3ca427fb0731")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Enkelttjeneste fjernet",
            Description = "Tilgangen til enkelttjenesten ble fjernet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service removed"),
            KeyValuePair.Create("Description", "Access to the single service was removed.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Enkelttjeneste fjerna"),
            KeyValuePair.Create("Description", "Tilgangen til enkelttenesta vart fjerna.")),
    };

    /// <summary>
    /// Assignment / Instance / Created — access to a single instance was granted.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentInstanceCreated { get; } = new ConstantDefinition<ActivityType>("6ef1a7af-4660-4c1e-b67c-2dc42b930dcc")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Subtype = ActivityLogSubtype.Instance,
            Trigger = ActivityLogTrigger.Created,
            Name = "Instanstilgang tildelt",
            Description = "Det ble gitt tilgang til en enkelt instans, f.eks. et skjema eller en dialog.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Instance access granted"),
            KeyValuePair.Create("Description", "Access to a single instance was granted, e.g. a form or a dialog.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Instanstilgang tildelt"),
            KeyValuePair.Create("Description", "Det vart gitt tilgang til ein enkelt instans, t.d. eit skjema eller ein dialog.")),
    };

    /// <summary>
    /// Assignment / Instance / Updated — the instance access was moved to another assignment.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentInstanceUpdated { get; } = new ConstantDefinition<ActivityType>("61fc5569-a7e8-49b3-841b-ace4a7d4accd")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Subtype = ActivityLogSubtype.Instance,
            Trigger = ActivityLogTrigger.Updated,
            Name = "Instanstilgang flyttet",
            Description = "Tilgangen til instansen ble flyttet til en annen rolletildeling (administrativ opprydding).",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Instance access moved"),
            KeyValuePair.Create("Description", "The instance access was moved to another role assignment (administrative cleanup).")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Instanstilgang flytta"),
            KeyValuePair.Create("Description", "Tilgangen til instansen vart flytta til ei anna rolletildeling (administrativ opprydding).")),
    };

    /// <summary>
    /// Assignment / Instance / Deleted — access to the instance was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> AssignmentInstanceDeleted { get; } = new ConstantDefinition<ActivityType>("6424f43d-5507-423b-a281-22994e75c573")
    {
        Entity = new()
        {
            Type = ActivityLogType.Assignment,
            Subtype = ActivityLogSubtype.Instance,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Instanstilgang fjernet",
            Description = "Tilgangen til instansen ble fjernet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Instance access removed"),
            KeyValuePair.Create("Description", "Access to the instance was removed.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Instanstilgang fjerna"),
            KeyValuePair.Create("Description", "Tilgangen til instansen vart fjerna.")),
    };

    #endregion

    #region Delegation

    /// <summary>
    /// Delegation / Created — a facilitator connected a client to an agent.
    /// </summary>
    public static ConstantDefinition<ActivityType> DelegationCreated { get; } = new ConstantDefinition<ActivityType>("9ab53bde-0e35-4532-bb1d-236ca5c87de2")
    {
        Entity = new()
        {
            Type = ActivityLogType.Delegation,
            Trigger = ActivityLogTrigger.Created,
            Name = "Klientdelegering opprettet",
            Description = "En fasilitator, f.eks. et regnskapsbyrå, koblet en klient til en agent.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Client delegation created"),
            KeyValuePair.Create("Description", "A facilitator, e.g. an accounting firm, connected a client to an agent.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Klientdelegering oppretta"),
            KeyValuePair.Create("Description", "Ein fasilitator, t.d. eit rekneskapsbyrå, kopla ein klient til ein agent.")),
    };

    /// <summary>
    /// Delegation / Deleted — the connection between client and agent was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> DelegationDeleted { get; } = new ConstantDefinition<ActivityType>("a654f39c-1aa8-45c4-82ea-f6ac16c529f7")
    {
        Entity = new()
        {
            Type = ActivityLogType.Delegation,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Klientdelegering fjernet",
            Description = "Koblingen mellom klient og agent ble fjernet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Client delegation removed"),
            KeyValuePair.Create("Description", "The connection between client and agent was removed.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Klientdelegering fjerna"),
            KeyValuePair.Create("Description", "Koplinga mellom klient og agent vart fjerna.")),
    };

    /// <summary>
    /// Delegation / Package / Created — an access package was delegated onwards to the agent.
    /// </summary>
    public static ConstantDefinition<ActivityType> DelegationPackageCreated { get; } = new ConstantDefinition<ActivityType>("ce3dff23-95d0-4b7c-997a-5f65edddb696")
    {
        Entity = new()
        {
            Type = ActivityLogType.Delegation,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Created,
            Name = "Tilgangspakke delegert",
            Description = "En tilgangspakke ble delegert videre fra klient til agent via fasilitator.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package delegated"),
            KeyValuePair.Create("Description", "An access package was delegated onwards from client to agent via the facilitator.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tilgangspakke delegert"),
            KeyValuePair.Create("Description", "Ein tilgangspakke vart delegert vidare frå klient til agent via fasilitator.")),
    };

    /// <summary>
    /// Delegation / Package / Deleted — the delegated access package was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> DelegationPackageDeleted { get; } = new ConstantDefinition<ActivityType>("f598e047-e908-45cf-867c-fb839e5587bc")
    {
        Entity = new()
        {
            Type = ActivityLogType.Delegation,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Delegert tilgangspakke fjernet",
            Description = "Den delegerte tilgangspakken ble fjernet fra klientdelegeringen.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegated access package removed"),
            KeyValuePair.Create("Description", "The delegated access package was removed from the client delegation.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegert tilgangspakke fjerna"),
            KeyValuePair.Create("Description", "Den delegerte tilgangspakken vart fjerna frå klientdelegeringa.")),
    };

    /// <summary>
    /// Delegation / Resource / Created — a single service was delegated onwards to the agent.
    /// </summary>
    public static ConstantDefinition<ActivityType> DelegationResourceCreated { get; } = new ConstantDefinition<ActivityType>("d013ef2e-91f7-48e1-94cc-2f1884727fe0")
    {
        Entity = new()
        {
            Type = ActivityLogType.Delegation,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Created,
            Name = "Enkelttjeneste delegert",
            Description = "En enkelttjeneste ble delegert videre fra klient til agent via fasilitator.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service delegated"),
            KeyValuePair.Create("Description", "A single service was delegated onwards from client to agent via the facilitator.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Enkelttjeneste delegert"),
            KeyValuePair.Create("Description", "Ei enkeltteneste vart delegert vidare frå klient til agent via fasilitator.")),
    };

    /// <summary>
    /// Delegation / Resource / Deleted — the delegated single service was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> DelegationResourceDeleted { get; } = new ConstantDefinition<ActivityType>("cbd8d3b1-1df4-4125-9ca5-7b137e18dd48")
    {
        Entity = new()
        {
            Type = ActivityLogType.Delegation,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Delegert enkelttjeneste fjernet",
            Description = "Den delegerte enkelttjenesten ble fjernet fra klientdelegeringen.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegated single service removed"),
            KeyValuePair.Create("Description", "The delegated single service was removed from the client delegation.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Delegert enkeltteneste fjerna"),
            KeyValuePair.Create("Description", "Den delegerte enkelttenesta vart fjerna frå klientdelegeringa.")),
    };

    #endregion

    #region Request

    /// <summary>
    /// Request / Created — a request between the parties was created.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestCreated { get; } = new ConstantDefinition<ActivityType>("904cdd82-37b6-41d7-b4e3-a85fcc152d25")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Trigger = ActivityLogTrigger.Created,
            Name = "Forespørsel opprettet",
            Description = "Det ble opprettet en forespørsel mellom partene.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Request created"),
            KeyValuePair.Create("Description", "A request between the parties was created.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad oppretta"),
            KeyValuePair.Create("Description", "Det vart oppretta ein førespurnad mellom partane.")),
    };

    /// <summary>
    /// Request / Deleted — the request was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestDeleted { get; } = new ConstantDefinition<ActivityType>("0a0ee722-e6e9-4462-873f-93f2c02f7836")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Forespørsel fjernet",
            Description = "Forespørselen ble fjernet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Request removed"),
            KeyValuePair.Create("Description", "The request was removed.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad fjerna"),
            KeyValuePair.Create("Description", "Førespurnaden vart fjerna.")),
    };

    /// <summary>
    /// Request / Package / Created (any status) — a request for an access package was created.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageCreated { get; } = new ConstantDefinition<ActivityType>("37bac747-9827-4472-adce-0371dec07bfd")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Created,
            Name = "Forespørsel om tilgangspakke opprettet",
            Description = "Det ble opprettet en forespørsel om tilgang til en tilgangspakke.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request created"),
            KeyValuePair.Create("Description", "A request for access to an access package was created.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke oppretta"),
            KeyValuePair.Create("Description", "Det vart oppretta ein førespurnad om tilgang til ein tilgangspakke.")),
    };

    /// <summary>
    /// Request / Package / Created with status Draft — a draft request was created but not yet sent.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageCreatedDraft { get; } = new ConstantDefinition<ActivityType>("0b818b59-1066-4734-bf65-894905c0a1e8")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Created,
            Status = RequestStatus.Draft,
            Name = "Utkast til forespørsel om tilgangspakke opprettet",
            Description = "Et utkast til forespørsel om tilgangspakke ble opprettet, men er ikke sendt ennå.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request draft created"),
            KeyValuePair.Create("Description", "A draft request for an access package was created but not yet sent.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Utkast til førespurnad om tilgangspakke oppretta"),
            KeyValuePair.Create("Description", "Eit utkast til førespurnad om tilgangspakke vart oppretta, men er ikkje sendt enno.")),
    };

    /// <summary>
    /// Request / Package / Created with status Pending — the request was created and sent for review.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageCreatedPending { get; } = new ConstantDefinition<ActivityType>("e786a7fd-22c4-4498-891b-68961535ee2d")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Created,
            Status = RequestStatus.Pending,
            Name = "Forespørsel om tilgangspakke sendt",
            Description = "Forespørselen om tilgangspakke ble sendt til mottakeren for behandling.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request sent"),
            KeyValuePair.Create("Description", "The access package request was sent to the recipient for review.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke send"),
            KeyValuePair.Create("Description", "Førespurnaden om tilgangspakke vart send til mottakaren for handsaming.")),
    };

    /// <summary>
    /// Request / Package / Updated (any status) — the request changed status.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageUpdated { get; } = new ConstantDefinition<ActivityType>("0dcbb3e0-69c6-4a21-b03c-be6a654bfc40")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Updated,
            Name = "Forespørsel om tilgangspakke oppdatert",
            Description = "Forespørselen om tilgangspakke endret status.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request updated"),
            KeyValuePair.Create("Description", "The access package request changed status.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke oppdatert"),
            KeyValuePair.Create("Description", "Førespurnaden om tilgangspakke endra status.")),
    };

    /// <summary>
    /// Request / Package / Updated to Pending — the draft was confirmed and sent for review.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageUpdatedPending { get; } = new ConstantDefinition<ActivityType>("5d3b7916-0938-432c-a798-ffc4ffdfb482")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Pending,
            Name = "Forespørsel om tilgangspakke sendt",
            Description = "Utkastet ble bekreftet og forespørselen sendt til mottakeren for behandling.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request sent"),
            KeyValuePair.Create("Description", "The draft was confirmed and the request sent to the recipient for review.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke send"),
            KeyValuePair.Create("Description", "Utkastet vart stadfesta og førespurnaden send til mottakaren for handsaming.")),
    };

    /// <summary>
    /// Request / Package / Updated to Approved — the recipient approved the request.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageUpdatedApproved { get; } = new ConstantDefinition<ActivityType>("c2dcc001-9ec0-4d66-a0b3-93f675241459")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Approved,
            Name = "Forespørsel om tilgangspakke godkjent",
            Description = "Mottakeren godkjente forespørselen, og tilgangen ble opprettet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request approved"),
            KeyValuePair.Create("Description", "The recipient approved the request and the access was created.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke godkjend"),
            KeyValuePair.Create("Description", "Mottakaren godkjende førespurnaden, og tilgangen vart oppretta.")),
    };

    /// <summary>
    /// Request / Package / Updated to Rejected — the recipient rejected the request.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageUpdatedRejected { get; } = new ConstantDefinition<ActivityType>("815978d1-42a0-4b2d-94ec-9de231d17a1e")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Rejected,
            Name = "Forespørsel om tilgangspakke avslått",
            Description = "Mottakeren avslo forespørselen.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request rejected"),
            KeyValuePair.Create("Description", "The recipient rejected the request.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke avslått"),
            KeyValuePair.Create("Description", "Mottakaren avslo førespurnaden.")),
    };

    /// <summary>
    /// Request / Package / Updated to Withdrawn — the sender withdrew the request.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageUpdatedWithdrawn { get; } = new ConstantDefinition<ActivityType>("43400193-44eb-49dd-b715-7220abb631f1")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Withdrawn,
            Name = "Forespørsel om tilgangspakke trukket",
            Description = "Avsenderen trakk forespørselen tilbake.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request withdrawn"),
            KeyValuePair.Create("Description", "The sender withdrew the request.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke trekt"),
            KeyValuePair.Create("Description", "Avsendaren trekte førespurnaden tilbake.")),
    };

    /// <summary>
    /// Request / Package / Deleted — the access package request was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestPackageDeleted { get; } = new ConstantDefinition<ActivityType>("a71dff2e-b269-49cc-a660-3ad409569bce")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Package,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Forespørsel om tilgangspakke fjernet",
            Description = "Forespørselen om tilgangspakke ble fjernet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Access package request removed"),
            KeyValuePair.Create("Description", "The access package request was removed.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om tilgangspakke fjerna"),
            KeyValuePair.Create("Description", "Førespurnaden om tilgangspakke vart fjerna.")),
    };

    /// <summary>
    /// Request / Resource / Created (any status) — a request for a single service was created.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceCreated { get; } = new ConstantDefinition<ActivityType>("c807957c-3bb3-4a3a-8e2d-46e7aa64a606")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Created,
            Name = "Forespørsel om enkelttjeneste opprettet",
            Description = "Det ble opprettet en forespørsel om tilgang til en enkelttjeneste.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request created"),
            KeyValuePair.Create("Description", "A request for access to a single service was created.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste oppretta"),
            KeyValuePair.Create("Description", "Det vart oppretta ein førespurnad om tilgang til ei enkeltteneste.")),
    };

    /// <summary>
    /// Request / Resource / Created with status Draft — a draft request was created but not yet sent.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceCreatedDraft { get; } = new ConstantDefinition<ActivityType>("8c22da2f-27c3-4daa-9ecb-f57401409ed8")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Created,
            Status = RequestStatus.Draft,
            Name = "Utkast til forespørsel om enkelttjeneste opprettet",
            Description = "Et utkast til forespørsel om enkelttjeneste ble opprettet, men er ikke sendt ennå.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request draft created"),
            KeyValuePair.Create("Description", "A draft request for a single service was created but not yet sent.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Utkast til førespurnad om enkeltteneste oppretta"),
            KeyValuePair.Create("Description", "Eit utkast til førespurnad om enkeltteneste vart oppretta, men er ikkje sendt enno.")),
    };

    /// <summary>
    /// Request / Resource / Created with status Pending — the request was created and sent for review.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceCreatedPending { get; } = new ConstantDefinition<ActivityType>("e744329d-a35d-4fc9-a8a2-af4dd5f16a6c")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Created,
            Status = RequestStatus.Pending,
            Name = "Forespørsel om enkelttjeneste sendt",
            Description = "Forespørselen om enkelttjeneste ble sendt til mottakeren for behandling.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request sent"),
            KeyValuePair.Create("Description", "The single service request was sent to the recipient for review.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste send"),
            KeyValuePair.Create("Description", "Førespurnaden om enkeltteneste vart send til mottakaren for handsaming.")),
    };

    /// <summary>
    /// Request / Resource / Updated (any status) — the request changed status.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceUpdated { get; } = new ConstantDefinition<ActivityType>("f52ceef9-4c7d-485c-a793-4251cd744885")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Updated,
            Name = "Forespørsel om enkelttjeneste oppdatert",
            Description = "Forespørselen om enkelttjeneste endret status.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request updated"),
            KeyValuePair.Create("Description", "The single service request changed status.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste oppdatert"),
            KeyValuePair.Create("Description", "Førespurnaden om enkeltteneste endra status.")),
    };

    /// <summary>
    /// Request / Resource / Updated to Pending — the draft was confirmed and sent for review.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceUpdatedPending { get; } = new ConstantDefinition<ActivityType>("92b177e3-a739-4e70-8203-f0a2b0d766d4")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Pending,
            Name = "Forespørsel om enkelttjeneste sendt",
            Description = "Utkastet ble bekreftet og forespørselen sendt til mottakeren for behandling.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request sent"),
            KeyValuePair.Create("Description", "The draft was confirmed and the request sent to the recipient for review.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste send"),
            KeyValuePair.Create("Description", "Utkastet vart stadfesta og førespurnaden send til mottakaren for handsaming.")),
    };

    /// <summary>
    /// Request / Resource / Updated to Approved — the recipient approved the request.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceUpdatedApproved { get; } = new ConstantDefinition<ActivityType>("84dee7b3-5e0d-4dd7-b65f-5fea095bf6c5")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Approved,
            Name = "Forespørsel om enkelttjeneste godkjent",
            Description = "Mottakeren godkjente forespørselen, og tilgangen ble opprettet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request approved"),
            KeyValuePair.Create("Description", "The recipient approved the request and the access was created.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste godkjend"),
            KeyValuePair.Create("Description", "Mottakaren godkjende førespurnaden, og tilgangen vart oppretta.")),
    };

    /// <summary>
    /// Request / Resource / Updated to Rejected — the recipient rejected the request.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceUpdatedRejected { get; } = new ConstantDefinition<ActivityType>("f590cb1b-84eb-4e10-971f-96e2a8c51c27")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Rejected,
            Name = "Forespørsel om enkelttjeneste avslått",
            Description = "Mottakeren avslo forespørselen.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request rejected"),
            KeyValuePair.Create("Description", "The recipient rejected the request.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste avslått"),
            KeyValuePair.Create("Description", "Mottakaren avslo førespurnaden.")),
    };

    /// <summary>
    /// Request / Resource / Updated to Withdrawn — the sender withdrew the request.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceUpdatedWithdrawn { get; } = new ConstantDefinition<ActivityType>("b5d6d610-a6eb-421d-9923-e36f1488452e")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Updated,
            Status = RequestStatus.Withdrawn,
            Name = "Forespørsel om enkelttjeneste trukket",
            Description = "Avsenderen trakk forespørselen tilbake.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request withdrawn"),
            KeyValuePair.Create("Description", "The sender withdrew the request.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste trekt"),
            KeyValuePair.Create("Description", "Avsendaren trekte førespurnaden tilbake.")),
    };

    /// <summary>
    /// Request / Resource / Deleted — the single service request was removed.
    /// </summary>
    public static ConstantDefinition<ActivityType> RequestResourceDeleted { get; } = new ConstantDefinition<ActivityType>("a3052a8c-8f2b-44c5-ae0e-3983e6ff212e")
    {
        Entity = new()
        {
            Type = ActivityLogType.Request,
            Subtype = ActivityLogSubtype.Resource,
            Trigger = ActivityLogTrigger.Deleted,
            Name = "Forespørsel om enkelttjeneste fjernet",
            Description = "Forespørselen om enkelttjeneste ble fjernet.",
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Single service request removed"),
            KeyValuePair.Create("Description", "The single service request was removed.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Førespurnad om enkeltteneste fjerna"),
            KeyValuePair.Create("Description", "Førespurnaden om enkeltteneste vart fjerna.")),
    };

    #endregion
}
