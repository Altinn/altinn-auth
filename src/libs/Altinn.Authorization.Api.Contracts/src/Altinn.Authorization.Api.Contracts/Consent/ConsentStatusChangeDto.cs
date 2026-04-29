namespace Altinn.Authorization.Api.Contracts.Consent
{
    /// <summary>
    /// DTO representing a consent status change
    /// </summary>
    public class ConsentStatusChangeDto
    {
        public required Guid ConsentRequestId { get; set; }

        public required string EventType { get; set; }

        public required DateTimeOffset ChangedDate { get; set; }
    }
}
