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
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
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

        var (recipient, reviewer, items, idempotencyId) = await UnwrapMessage(message, cancellationToken);

        NotificationOrderChainRequestExt content = new()
        {
            IdempotencyId = idempotencyId,
            SendersReference = idempotencyId,
            Recipient = CreateRecipient(recipient, BuildEmailBody(reviewer, items)),
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
        IReadOnlyList<ReviewedItem> Items,
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

        List<ReviewedItem> items =
        [
            .. await GetPackages(content, cancellationToken),
            .. await GetResources(content, cancellationToken),
        ];

        return (
            entityRecipient,
            entityReviewer,
            items,
            $"auth_resource_request_review_{entityRecipient.Id}_{entityReviewer.Id}_{message.CreatedAt.Ticks}"
        );

        async Task<IEnumerable<ReviewedItem>> GetResources(RequestReviewNotificationMessage content, CancellationToken cancellationToken)
        {
            if (content.Resources is { } && content.Resources.Any())
            {
                var resources = await db.Resources
                    .AsNoTracking()
                    .Where(r => content.Resources.Select(r => r.Ref).Contains(r.Id))
                    .ToListAsync(cancellationToken);

                return resources.Select(r =>
                {
                    var response = content.Resources.First(f => f.Ref == r.Id);
                    return new ReviewedItem(r.Name, ReviewedItemType.Resource, response.RequestStatus, response.IsApproved);
                });
            }

            return [];
        }

        async Task<IEnumerable<ReviewedItem>> GetPackages(RequestReviewNotificationMessage content, CancellationToken cancellationToken)
        {
            if (content.Packages is { } && content.Packages.Any())
            {
                var packages = await db.Packages
                    .AsNoTracking()
                    .Where(r => content.Packages.Select(r => r.Ref).Contains(r.Id))
                    .ToListAsync(cancellationToken);

                return packages.Select(p =>
                {
                    var response = content.Packages.First(f => f.Ref == p.Id);
                    return new ReviewedItem(p.Name, ReviewedItemType.Package, response.RequestStatus, response.IsApproved);
                });
            }

            return [];
        }
    }

    private static NotificationRecipientExt CreateRecipient(Entity recipient, string emailBody)
    {
        ArgumentNullException.ThrowIfNull(recipient);

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
                        Body = emailBody,
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
                        Body = emailBody,
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
    }

    /// <summary>
    /// Builds the HTML body of the email that tells the requester their request was reviewed.
    /// </summary>
    /// <param name="reviewer">The person or organisation that reviewed the request.</param>
    /// <param name="items">The reviewed packages and resources, in the order they are listed.</param>
    /// <returns>The email body as HTML.</returns>
    internal static string BuildEmailBody(Entity reviewer, IEnumerable<ReviewedItem> items)
    {
        ArgumentNullException.ThrowIfNull(reviewer);

        var emailContent = new StringBuilder();
        AddEmailIngress(emailContent, reviewer);
        var list = items.ToList();
        AddSection(emailContent, "Tilgangspakker:", list.Where(i => i.Type == ReviewedItemType.Package));
        AddSection(emailContent, "Enkelttjenester:", list.Where(i => i.Type == ReviewedItemType.Resource));
        emailContent.AppendLine($"<p>Med vennlig hilsen,<br>Altinn</p>");
        emailContent.AppendLine(@"<em>Denne meldingen er automatisk generert. Svar til denne adressen vil ikke bli behandlet.</em>");
        return emailContent.ToString();

        static void AddEmailIngress(StringBuilder emailContent, Entity reviewer)
        {
            if (reviewer.TypeId == EntityTypeConstants.Organization)
            {
                emailContent.AppendLine($"<p>{reviewer.Name} med Org.nr {reviewer.OrganizationIdentifier} har svart på din forespørsel om fullmakter.</p>");
            }
            else if (reviewer.TypeId == EntityTypeConstants.Person)
            {
                emailContent.AppendLine($"<p>{reviewer.Name} har svart på din forespørsel om fullmakter.</p>");
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

        static void AddSection(StringBuilder emailContent, string heading, IEnumerable<ReviewedItem> items)
        {
            if (!items.Any())
            {
                return;
            }

            emailContent.Append($"<strong>{heading}</strong>");
            emailContent.AppendLine("<ul>");
            foreach (var item in items)
            {
                emailContent.AppendLine($"<li>{item.Name}. <strong><em>{GetStatusLabel(item.Status, item.IsApproved)}</em></strong></li>");
            }

            emailContent.AppendLine("</ul>");
        }
    }

    /// <summary>
    /// Gets the label shown after a reviewed item in the email.
    /// </summary>
    /// <param name="status">The review outcome, or null in messages written before the status was stored.</param>
    /// <param name="isApproved">The legacy outcome flag, used when <paramref name="status"/> is null or not a review outcome.</param>
    /// <returns>"Godkjent.", "Avslått." or "Godkjent med justeringer."</returns>
    internal static string GetStatusLabel(RequestStatusV2? status, bool isApproved) => status switch
    {
        RequestStatusV2.Approved => "Godkjent.",
        RequestStatusV2.Rejected => "Avslått.",
        RequestStatusV2.PartialApprove => "Godkjent med justeringer.",
        _ => isApproved ? "Godkjent." : "Avslått.",
    };
}

/// <summary>
/// Whether a reviewed item is an access package or a single resource.
/// </summary>
internal enum ReviewedItemType
{
    Package,
    Resource,
}

/// <summary>
/// A reviewed package or resource, as listed in the request-reviewed email.
/// </summary>
/// <param name="Name">The display name of the package or resource.</param>
/// <param name="Type">Whether it is a package or a resource.</param>
/// <param name="Status">The review outcome. Null in messages written before the status was stored.</param>
/// <param name="IsApproved">The legacy outcome flag, used when <paramref name="Status"/> is null.</param>
internal sealed record ReviewedItem(string Name, ReviewedItemType Type, RequestStatusV2? Status, bool IsApproved);

public class RequestReviewNotificationMessageResponse<T>
{
    /// <summary>
    /// Package or Resource
    /// </summary>
    public T Ref { get; set; }

    public bool IsApproved { get; set; }

    public RequestStatusV2? RequestStatus { get; set; }
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
