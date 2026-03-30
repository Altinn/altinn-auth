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

public class RequestApprovedNotificationHandler(
    AppDbContext db,
    IAltinnNotification notification,
    IFeatureManager featureManager,
    IEntityService entityService) : IOutboxHandler
{
    public async Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (await featureManager.IsDisabledAsync(AccessMgmtFeatureFlags.AccessMgmtCoreOutboxRequestNotifyApproved, cancellationToken))
        {
            db.OutboxMessageLogs.Add(message, $"Feature flag '{AccessMgmtFeatureFlags.AccessMgmtCoreOutboxRequestNotifyApproved}' is disabled.");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        var (recipient, approver, resources, packages, idempotencyId) = await GetContext(message, cancellationToken);

        NotificationOrderChainRequestExt content = new()
        {
            IdempotencyId = idempotencyId,
            Recipient = await CreateRecipient(recipient, approver, resources, packages, cancellationToken),
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

    private async Task<(Entity Recipient, Entity Approver, IEnumerable<Resource> Resources, IEnumerable<Package> Packages, string IdempotencyId)> GetContext(OutboxMessage message, CancellationToken cancellationToken)
    {
        var content = JsonSerializer.Deserialize<RequestApprovedNotificationMessage>(message.Data);
        if (content is null)
        {
            throw new InvalidOperationException("Data is empty. Can't send notification without content.");
        }

        var entityRecipient = await entityService.GetEntity(content.RecipientId, cancellationToken);
        if (entityRecipient is null)
        {
            throw new InvalidOperationException($"Recipient entity with id '{content.RecipientId}' not found.");
        }

        var entityApprover = await entityService.GetEntity(content.ApproverId, cancellationToken);
        if (entityApprover is null)
        {
            throw new InvalidOperationException($"Approver entity with id '{content.ApproverId}' not found.");
        }

        return (
            entityRecipient,
            entityApprover,
            await GetResources(content, cancellationToken),
            await GetPackages(content, cancellationToken),
            $"auth_resource_request_approved_{entityRecipient.Id}_{entityApprover.Id}_{content.InitiatedAt.Ticks}"
        );

        async Task<List<Resource>> GetResources(RequestApprovedNotificationMessage content, CancellationToken cancellationToken)
        {
            if (content.ResourceIds is { } && content.ResourceIds.Any())
            {
                return await db.Resources
                    .AsNoTracking()
                    .Where(r => content.ResourceIds.Contains(r.Id))
                    .ToListAsync(cancellationToken);
            }

            return [];
        }

        async Task<List<Package>> GetPackages(RequestApprovedNotificationMessage content, CancellationToken cancellationToken)
        {
            if (content.PackageIds is { } && content.PackageIds.Any())
            {
                var packages = await db.Packages
                    .AsNoTracking()
                    .Where(r => content.PackageIds.Contains(r.Id))
                    .ToListAsync(cancellationToken);
            }

            return [];
        }
    }

    private static async Task<NotificationRecipientExt> CreateRecipient(Entity recipient, Entity approver, IEnumerable<Resource> resources, IEnumerable<Package> packages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        if (recipient.TypeId == EntityTypeConstants.Person)
        {
            var emailContent = new StringBuilder();
            emailContent.AppendLine($"<p>{approver.Name} har akseptert din forespørsel om følgende fullmakter.</p>");

            AddResourcesAndPackage(resources, packages, emailContent);

            emailContent.AppendLine($"<p>Med vennlig hilsen</br>Altinn</p>");

            return new NotificationRecipientExt
            {
                RecipientPerson = new RecipientPersonExt
                {
                    NationalIdentityNumber = recipient.PersonIdentifier,
                    ChannelSchema = NotificationChannelExt.Email,
                    ResourceId = "urn:altinn:resource:altinn_access_management_hovedadmin",
                    EmailSettings = new EmailSendingOptionsExt
                    {
                        Subject = "Altinn Godkjent Tilgangsforespørsel",
                        Body = emailContent.ToString(),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime
                    }
                }
            };
        }
        else if (recipient.TypeId == EntityTypeConstants.Organization)
        {
            var emailContent = new StringBuilder();
            emailContent.AppendLine($"<p>{approver.Name} med Org.nr {recipient.OrganizationIdentifier} har akseptert din forespørsel om følgende fullmakter.</p>");

            AddResourcesAndPackage(resources, packages, emailContent);

            emailContent.AppendLine($"<p>Med vennlig hilsen</br>Altinn</p>");

            return new NotificationRecipientExt
            {
                RecipientOrganization = new RecipientOrganizationExt
                {
                    OrgNumber = recipient.OrganizationIdentifier,
                    ChannelSchema = NotificationChannelExt.Email,
                    ResourceId = "urn:altinn:resource:altinn_access_management_hovedadmin",
                    EmailSettings = new()
                    {
                        Subject = "Altinn Godkjent Tilgangsforespørsel",
                        Body = emailContent.ToString(),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime
                    }
                }
            };
        }

        throw new InvalidOperationException("Unsupported party type. not person or organization");

        static void AddResourcesAndPackage(IEnumerable<Resource> resources, IEnumerable<Package> packages, StringBuilder emailContent)
        {
            if (resources.Any())
            {
                emailContent.AppendLine("<ul>");
                foreach (var resource in resources)
                {
                    emailContent.AppendLine($"<li>{resource.Name}</li>");
                }

                emailContent.AppendLine("</ul>");
            }

            if (packages.Any())
            {
                emailContent.AppendLine("<p>og/eller følgende pakkeløsninger:</p>");
                emailContent.AppendLine("<ul>");
                foreach (var package in packages)
                {
                    emailContent.AppendLine($"<li>{package.Name}</li>");
                }

                emailContent.AppendLine("</ul>");
            }
        }
    }
}

/// <summary>
/// Model used for deserializing content of outbox message for resource request notification.
/// </summary>
public class RequestApprovedNotificationMessage
{
    /// <summary>
    /// Entity ID of the approver, either person or organization.
    /// </summary>
    public Guid ApproverId { get; set; }

    /// <summary>
    /// Entity ID of the recipient, either person or organization. 
    /// </summary>
    public Guid RecipientId { get; set; }

    /// <summary>
    /// Guid of resource.
    /// </summary>
    public IEnumerable<Guid> ResourceIds { get; set; }

    /// <summary>
    /// Guid of package
    /// </summary>
    public IEnumerable<Guid> PackageIds { get; set; }

    /// <summary>
    /// Used for idempotency.
    /// </summary>
    public DateTime InitiatedAt { get; set; }

    /// <summary>
    /// Number of updates.
    /// </summary>
    public int Updated { get; set; } = 0;
}
