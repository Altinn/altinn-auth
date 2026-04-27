using System.Diagnostics;
using System.Text.Json;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Notifications;
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
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Outbox;

public class RightholderRemovedNotificationHandler(AppDbContext db,
    IAltinnNotification notification,
    IFeatureManager featureManager,
    IEntityService entityService) : IOutboxHandler
{
    public async Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (await featureManager.IsDisabledAsync(AccessMgmtFeatureFlags.OutboxRightholderRemovedNotify, cancellationToken))
        {
            db.OutboxMessageLogs.Add(message, $"Feature flag '{AccessMgmtFeatureFlags.OutboxRightholderRemovedNotify}' is disabled.");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        var (from, to, idempotencyId) = await UnwrapMessage(message, cancellationToken);
        if (to.TypeId != EntityTypeConstants.Person)
        {
            db.OutboxMessageLogs.Add(message, "to entity type is not of type <Person>");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        if (from.TypeId != EntityTypeConstants.Person && from.TypeId != EntityTypeConstants.Organization)
        {
            db.OutboxMessageLogs.Add(message, "from entity type is not of type <Person | Organization>");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        var content = new NotificationOrderChainRequestExt()
        {
            IdempotencyId = idempotencyId,
            SendersReference = idempotencyId,
            Recipient = CreateRecipient(from, to),
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

    private async Task<(Entity From, Entity To, string IdempotencyId)> UnwrapMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        var content = JsonSerializer.Deserialize<RightholderRemovedNotificationMessage>(message.Data);
        if (content is null)
        {
            throw new InvalidOperationException("Data is empty. Can't send notification without content.");
        }

        var fromEntity = await entityService.GetEntity(content.FromId, cancellationToken);
        if (fromEntity is null)
        {
            throw new InvalidOperationException($"From entity with id '{content.FromId}' not found.");
        }

        var toEntity = await entityService.GetEntity(content.ToId, cancellationToken);
        if (toEntity is null)
        {
            throw new InvalidOperationException($"to entity with id '{content.ToId}' not found.");
        }

        return (
            fromEntity,
            toEntity,
            $"auth_{RightholderRemovedNotification.Handler}_{fromEntity.Id}_{toEntity.Id}_{message.CreatedAt.Ticks}"
        );
    }

    private static NotificationRecipientExt CreateRecipient(Entity from, Entity to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var pronoun = to.TypeId == EntityTypeConstants.Person ? "Du" : "Dere";
        var subject = $"{pronoun} har mistet en aktør i Altinn";

        if (to.TypeId == EntityTypeConstants.Person)
        {
            return new NotificationRecipientExt
            {
                RecipientPerson = new RecipientPersonExt
                {
                    NationalIdentityNumber = to.PersonIdentifier,
                    ChannelSchema = NotificationChannelExt.Email,
                    EmailSettings = new EmailSendingOptionsExt
                    {
                        Subject = subject,
                        Body = MailContent(from, to),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime,
                    }
                }
            };
        }
        else if (to.TypeId == EntityTypeConstants.Organization)
        {
            return new NotificationRecipientExt
            {
                RecipientOrganization = new RecipientOrganizationExt
                {
                    OrgNumber = to.OrganizationIdentifier,
                    ChannelSchema = NotificationChannelExt.Email,
                    ResourceId = "urn:altinn:resource:altinn_access_management_hovedadmin",
                    EmailSettings = new()
                    {
                        Subject = subject,
                        Body = MailContent(from, to),
                        ContentType = EmailContentTypeExt.Html,
                        SendingTimePolicy = SendingTimePolicyExt.Anytime,
                    }
                }
            };
        }

        throw new InvalidOperationException("to entity type must be of type <Person | Organization>");
    }

    private static string MailContent(Entity from, Entity to)
    {
        if (from.TypeId == EntityTypeConstants.Person && to.TypeId == EntityTypeConstants.Person)
        {
            return /*HTML*/ $@"
                <p>
                    Hei,
                </p>
                <p>
                    {from.Name} har fjernet deg som sin bruker i Altinn.
                </p>
                <p>
                    Med vennlig hilsen,<br>
                    Altinn
                </p>
                <em>
                    Denne meldingen er automatisk generert. Svar til denne adressen vil ikke bli behandlet.
                </em>
            ";
        }

        if (from.TypeId == EntityTypeConstants.Organization && to.TypeId == EntityTypeConstants.Person)
        {
            return /*HTML*/ $@"
                <p>
                    Hei,
                </p>
                <p>
                    {from.Name}, {from.OrganizationIdentifier} har fjernet deg som sin bruker i Altinn.
                </p>
                <p>
                    Med vennlig hilsen,<br>
                    Altinn
                </p>
                <em>
                    Denne meldingen er automatisk generert. Svar til denne adressen vil ikke bli behandlet.
                </em>
            ";
        }

        if (from.TypeId == EntityTypeConstants.Person && to.TypeId == EntityTypeConstants.Organization)
        {
            return /*HTML*/ $@"
                <p>
                    Hei,
                </p>
                <p>
                    {from.Name} har fjernet dere som sin bruker i Altinn.
                </p>
                <p>
                    Med vennlig hilsen,<br>
                    Altinn
                </p>
                <em>
                    Denne meldingen er automatisk generert. Svar til denne adressen vil ikke bli behandlet.
                </em>
            ";
        }

        if (from.TypeId == EntityTypeConstants.Organization && to.TypeId == EntityTypeConstants.Organization)
        {
            return /*HTML*/ $@"
                <p>
                    Hei,
                </p>
                <p>
                    {from.Name}, {from.OrganizationIdentifier} har fjernet dere som sin bruker i Altinn.
                </p>
                <p>
                    Med vennlig hilsen,<br>
                    Altinn
                </p>
                <em>
                    Denne meldingen er automatisk generert. Svar til denne adressen vil ikke bli behandlet.
                </em>
            ";
        }

        throw new InvalidOperationException("from and to entity type must be of type <Person | Organization>");
    }
}

public class RightholderRemovedNotificationMessage
{
    public Guid FromId { get; set; }

    public Guid ToId { get; set; }
}
