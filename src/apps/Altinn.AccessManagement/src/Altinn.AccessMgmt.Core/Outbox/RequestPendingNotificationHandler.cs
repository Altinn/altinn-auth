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

public class RequestPendingNotificationHandler(
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

        var (recipient, requester, resources, packages, idempotencyId) = await GetContext(message, cancellationToken);

        var content = new NotificationOrderChainRequestExt()
        {
            IdempotencyId = idempotencyId,
            Recipient = CreateRecipient(recipient, requester, resources, packages),
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

    private async Task<(Entity Recipient, Entity Requester, IEnumerable<Resource> Resources, IEnumerable<Package> Packages, string IdempotencyId)> GetContext(OutboxMessage message, CancellationToken cancellationToken)
    {
        var content = JsonSerializer.Deserialize<ResourceRequestPendingNotificationMessage>(message.Data);
        if (content is null)
        {
            throw new InvalidOperationException("Data is empty. Can't send notification without content.");
        }

        var entityRecipient = await entityService.GetEntity(content.RecipientId, cancellationToken);
        if (entityRecipient is null)
        {
            throw new InvalidOperationException($"Recipient entity with id '{content.RecipientId}' not found.");
        }

        var entityRequester = await entityService.GetEntity(content.RequesterId, cancellationToken);
        if (entityRequester is null)
        {
            throw new InvalidOperationException($"Sender entity with id '{content.RequesterId}' not found.");
        }

        return (
            entityRecipient,
            entityRequester,
            await GetResources(content, cancellationToken),
            await GetPackages(content, cancellationToken),
            $"auth_resource_request_pending_{entityRecipient.Id}_{entityRequester.Id}_{content.InitiatedAt.Ticks}"
        );

        async Task<List<Resource>> GetResources(ResourceRequestPendingNotificationMessage content, CancellationToken cancellationToken)
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

        async Task<List<Package>> GetPackages(ResourceRequestPendingNotificationMessage content, CancellationToken cancellationToken)
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

    private static NotificationRecipientExt CreateRecipient(Entity recipient, Entity requester, IEnumerable<Resource> resources, IEnumerable<Package> packages)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        if (recipient.TypeId == EntityTypeConstants.Person)
        {
            var emailContent = new StringBuilder();
            emailContent.AppendLine($"<p>{requester.Name} har bedt om følgende fullmakter fra deg.</p>");

            AddResourcesAndPackage(resources, packages, emailContent);

            emailContent.AppendLine("<p>Logg inn i Altinn, gå til tilgangsstyring og forespørsler for å behandle forespørselen.</p>");
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
                        Subject = "Altinn Tilgangsforespørsel",
                        Body = emailContent.ToString(),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime,
                    }
                }
            };
        }
        else if (recipient.TypeId == EntityTypeConstants.Organization)
        {
            var emailContent = new StringBuilder();
            emailContent.AppendLine($"<p>{requester.Name} har bedt om følgende fullmakter fra {recipient.Name} med Org.nr {recipient.OrganizationIdentifier}.</p>");

            AddResourcesAndPackage(resources, packages, emailContent);

            emailContent.AppendLine($"<p>Du mottar denne forespørselen fordi du har tilgangspakken hovedaministrator for {recipient.Name} i Altinn. Logg inn i Altinn velg riktig aktør og gå til tilgangsstyring og forespørsler for å behandle forespørselen.</p>");
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
                        Subject = "Altinn Tilgangsforespørsel",
                        Body = emailContent.ToString(),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime,
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
public class ResourceRequestPendingNotificationMessage
{
    /// <summary>
    /// Entity ID of the requester, either person or organization.
    /// </summary>
    public Guid RequesterId { get; set; }

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
