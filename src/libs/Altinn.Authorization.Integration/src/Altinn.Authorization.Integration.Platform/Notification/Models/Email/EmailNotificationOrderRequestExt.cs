using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models.Email;

/// <summary>
/// Class representing an email notification order request
/// </summary>
/// <remarks>
/// External representation to be used in the API
/// </remarks>
public class EmailNotificationOrderRequestExt : NotificationOrderRequestBaseExt
{
    /// <summary>
    /// Gets or sets the subject of the email
    /// </summary>
    [JsonPropertyName("subject")]
    [Required]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the body of the email
    /// </summary>
    [JsonPropertyName("body")]
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type of the email
    /// </summary>
    [JsonPropertyName("contentType")]
    public EmailContentTypeExt? ContentType { get; set; }

    /// <summary>
    /// Json serialized the <see cref="EmailNotificationOrderRequestExt"/>
    /// </summary>
    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
