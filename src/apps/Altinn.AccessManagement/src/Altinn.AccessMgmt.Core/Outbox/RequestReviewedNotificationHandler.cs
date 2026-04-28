using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Altinn.Authorization.Integration.Platform.Notification;
using Altinn.Authorization.Integration.Platform.Notification.Models;
using Altinn.Authorization.Integration.Platform.Notification.Models.Email;
using Altinn.Authorization.Integration.Platform.Notification.Models.Recipient;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Outbox;

public class RequestReviewedNotificationHandler(
    AppDbContext db,
    IAltinnNotification notification,
    IFeatureManager featureManager,
    IEntityService entityService) : IOutboxHandler
{
    public async Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (await featureManager.IsDisabledAsync(AccessMgmtFeatureFlags.OutboxRequestReviewedNotify, cancellationToken))
        {
            db.OutboxMessageLogs.Add(message, $"Feature flag '{AccessMgmtFeatureFlags.OutboxRequestReviewedNotify}' is disabled.");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        var (recipient, reviewer, resources, packages, idempotencyId) = await UnwrapMessage(message, cancellationToken);

        NotificationOrderChainRequestExt content = new()
        {
            IdempotencyId = idempotencyId,
            SendersReference = idempotencyId,
            Recipient = CreateRecipient(recipient, reviewer, resources, packages),
        };

        var response = await notification.Send(content, cancellationToken);

        if (response.IsProblem)
        {
            // Contact information for organization / person is missing
            if (response.ProblemDetails?.ErrorCode.ToString() == "NOT-00001")
            {
                db.OutboxMessageLogs.Add(
                    message,
                    response.ProblemDetails?.Title ?? "Missing contact information for recipient(s)"
                );

                await db.SaveChangesAsync(cancellationToken);
                return OutboxStatus.Completed;
            }

            var errorMessage = $@"Failed to send notification.
                Payload: {JsonSerializer.Serialize(content)}
                CorrelationId: {Activity.Current?.TraceId}
                Status Code: {response.StatusCode}
                Problem Title: {response.ProblemDetails?.Title}
                Problem Details: {response.ProblemDetails?.Detail}
                Problem Instance: {response.ProblemDetails?.Instance}
                Problem Type: {response.ProblemDetails?.Type}
                Problem Error Code: {response.ProblemDetails?.ErrorCode}
                Problem Extensions: {JsonSerializer.Serialize(response.ProblemDetails?.Extensions ?? new Dictionary<string, object>())}";

            db.OutboxMessageLogs.Add(
                message,
                errorMessage
            );

            await db.SaveChangesAsync(cancellationToken);

            return OutboxStatus.Failed;
        }

        return OutboxStatus.Completed;
    }

    private async Task<(
        Entity Recipient,
        Entity Reviewer,
        IEnumerable<RequestReviewNotificationMessageResponse<Resource>> Resources,
        IEnumerable<RequestReviewNotificationMessageResponse<Package>> Packages,
        string IdempotencyId)>
        UnwrapMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        var content = JsonSerializer.Deserialize<RequestReviewNotificationMessage>(message.Data);
        if (content is null)
        {
            throw new InvalidOperationException("Data is empty. Can't send notification without content.");
        }

        var entityRecipient = await entityService.GetEntity(content.RecipientId, cancellationToken);
        if (entityRecipient is null)
        {
            throw new InvalidOperationException($"Recipient entity with id '{content.RecipientId}' not found.");
        }

        var entityReviewer = await entityService.GetEntity(content.ReviewerId, cancellationToken);
        if (entityReviewer is null)
        {
            throw new InvalidOperationException($"Reviewer entity with id '{content.ReviewerId}' not found.");
        }

        return (
            entityRecipient,
            entityReviewer,
            await GetResources(content, cancellationToken),
            await GetPackages(content, cancellationToken),
            $"auth_resource_request_review_{entityRecipient.Id}_{entityReviewer.Id}_{message.CreatedAt.Ticks}"
        );

        async Task<IEnumerable<RequestReviewNotificationMessageResponse<Resource>>> GetResources(RequestReviewNotificationMessage content, CancellationToken cancellationToken)
        {
            if (content.Resources is { } && content.Resources.Any())
            {
                var resources = await db.Resources
                    .AsNoTracking()
                    .Where(r => content.Resources.Select(r => r.Ref).Contains(r.Id))
                    .ToListAsync(cancellationToken);

                return resources.Select(r => new RequestReviewNotificationMessageResponse<Resource>()
                {
                    Ref = r,
                    IsApproved = content.Resources.First(f => f.Ref == r.Id).IsApproved
                });
            }

            return [];
        }

        async Task<IEnumerable<RequestReviewNotificationMessageResponse<Package>>> GetPackages(RequestReviewNotificationMessage content, CancellationToken cancellationToken)
        {
            if (content.Packages is { } && content.Packages.Any())
            {
                var packages = await db.Packages
                    .AsNoTracking()
                    .Where(r => content.Packages.Select(r => r.Ref).Contains(r.Id))
                    .ToListAsync(cancellationToken);

                return packages.Select(r => new RequestReviewNotificationMessageResponse<Package>()
                {
                    Ref = r,
                    IsApproved = content.Packages.First(f => f.Ref == r.Id).IsApproved
                });
            }

            return [];
        }
    }

    private static NotificationRecipientExt CreateRecipient(
        Entity recipient,
        Entity reviewer,
        IEnumerable<RequestReviewNotificationMessageResponse<Resource>> resources,
        IEnumerable<RequestReviewNotificationMessageResponse<Package>> packages)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        var emailContent = new StringBuilder();
        AddEmailIngress(emailContent, reviewer);
        AddResourcesAndPackage(emailContent, resources, packages);
        emailContent.AppendLine($"<p>Med vennlig hilsen,<br>Altinn</p>");
        emailContent.AppendLine(@"<em>Denne meldingen er automatisk generert. Svar til denne adressen vil ikke bli behandlet.</em>");

        if (recipient.TypeId == EntityTypeConstants.Person)
        {
            return new NotificationRecipientExt
            {
                RecipientPerson = new RecipientPersonExt
                {
                    NationalIdentityNumber = recipient.PersonIdentifier,
                    ChannelSchema = NotificationChannelExt.Email,
                    EmailSettings = new EmailSendingOptionsExt
                    {
                        Subject = "Altinn Behandlet tilgangsforespørsel",
                        Body = emailContent.ToString(),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime
                    }
                }
            };
        }
        else if (recipient.TypeId == EntityTypeConstants.Organization)
        {
            return new NotificationRecipientExt
            {
                RecipientOrganization = new RecipientOrganizationExt
                {
                    OrgNumber = recipient.OrganizationIdentifier,
                    ChannelSchema = NotificationChannelExt.Email,
                    ResourceId = "urn:altinn:resource:altinn_access_management_hovedadmin",
                    EmailSettings = new()
                    {
                        Subject = "Altinn Behandlet tilgangsforespørsel",
                        Body = emailContent.ToString(),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime
                    }
                }
            };
        }

        if (!EntityTypeConstants.TryGetById(recipient.TypeId, out var entityType))
        {
            throw new InvalidOperationException($"Couldn't find recipient entity with typeid {recipient.TypeId}");
        }

        throw new InvalidOperationException($"Unsupported recipient entity type with uuid '{recipient.Id}', must be a person or organization, not '{entityType.Entity.Name}'.");

        static void AddEmailIngress(StringBuilder emailContent, Entity reviewer)
        {
            if (reviewer.TypeId == EntityTypeConstants.Organization)
            {
                emailContent.AppendLine($"<p>{reviewer.Name} med Org.nr {reviewer.OrganizationIdentifier} har svart på forespørsel om følgende fullmakter.</p>");
            }
            else if (reviewer.TypeId == EntityTypeConstants.Person)
            {
                emailContent.AppendLine($"<p>{reviewer.Name} har svart på forespørsel om følgende fullmakter.</p>");
            }
            else
            {
                if (!EntityTypeConstants.TryGetById(reviewer.TypeId, out var entityType))
                {
                    throw new InvalidOperationException($"Couldn't find request rejecter with entity with typeid {reviewer.TypeId}");
                }

                throw new InvalidOperationException($"Unsupported request rejecter entity type with uuid '{reviewer.Id}', must be a person or organization, not '{entityType.Entity.Name}'.");
            }
        }

        static void AddResourcesAndPackage(
            StringBuilder emailContent,
            IEnumerable<RequestReviewNotificationMessageResponse<Resource>> resources,
            IEnumerable<RequestReviewNotificationMessageResponse<Package>> packages)
        {
            var approvedResources = resources.Where(r => r.IsApproved);
            var approvedPackages = packages.Where(p => p.IsApproved);
            var rejectedResources = resources.Where(r => !r.IsApproved);
            var rejectedPackages = packages.Where(p => !p.IsApproved);

            if (approvedPackages.Any() || approvedResources.Any())
            {
                emailContent.AppendLine($"<p><strong>Aksepterte</strong> fullmakter.</p>");
                if (approvedPackages.Any())
                {
                    emailContent.Append("<strong>Tilgangspakker:</strong>");
                    ListRefs(emailContent, approvedPackages.Select(p => p.Ref.Name));
                }

                if (approvedResources.Any())
                {
                    emailContent.Append("<strong>Ressurser:</strong>");
                    ListRefs(emailContent, approvedResources.Select(p => p.Ref.Name));
                }
            }

            if (rejectedPackages.Any() || rejectedResources.Any())
            {
                emailContent.AppendLine($"<p><strong>Avslåtte</strong> fullmakter.</p>");
                if (rejectedPackages.Any())
                {
                    emailContent.Append("<strong>Tilgangspakker:</strong>");
                    ListRefs(emailContent, rejectedPackages.Select(p => p.Ref.Name));
                }

                if (rejectedResources.Any())
                {
                    emailContent.Append("<strong>Ressurser:</strong>");
                    ListRefs(emailContent, rejectedResources.Select(p => p.Ref.Name));
                }
            }

            static void ListRefs(StringBuilder emailContent, IEnumerable<string> names)
            {
                emailContent.AppendLine("<ul>");
                foreach (var name in names)
                {
                    emailContent.AppendLine($"<li>{name}</li>");
                }

                emailContent.AppendLine("</ul>");
            }
        }
    }
}

public class RequestReviewNotificationMessageResponse<T>
{
    /// <summary>
    /// Package or Resource
    /// </summary>
    public T Ref { get; set; }

    public bool IsApproved { get; set; }
}

/// <summary>
/// Model used for deserializing content of outbox message for resource request notification.
/// </summary>
public class RequestReviewNotificationMessage
{
    /// <summary>
    /// Entity ID of the Reviewer, either person or organization.
    /// </summary>
    public Guid ReviewerId { get; set; }

    /// <summary>
    /// Entity ID of the recipient, either person or organization. 
    /// </summary>
    public Guid RecipientId { get; set; }

    /// <summary>
    /// Guids of approved / rejected resource.
    /// </summary>
    public List<RequestReviewNotificationMessageResponse<Guid>> Resources { get; set; } = [];

    /// <summary>
    /// Guids of approved / rejected package.
    /// </summary>
    public List<RequestReviewNotificationMessageResponse<Guid>> Packages { get; set; } = [];

    /// <summary>
    /// Number of updates.
    /// </summary>
    public int Updated { get; set; } = 0;
}
