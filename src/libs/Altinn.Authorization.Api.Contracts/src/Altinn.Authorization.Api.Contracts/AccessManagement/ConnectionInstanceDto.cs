namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Instance reference exposed on a <see cref="ConnectionDto"/>. The instance belongs to the resource
/// identified by <see cref="ResourceRefId"/>, which the caller can correlate with
/// <see cref="ResourceDto.RefId"/> on entries in <see cref="ConnectionDto.Resources"/> when also requested.
/// </summary>
public class ConnectionInstanceDto
{
    /// <summary>
    /// Reference identifier of the resource the instance belongs to, as registered in the Resource Registry.
    /// </summary>
    public string ResourceRefId { get; set; } = string.Empty;

    /// <summary>
    /// Instance identifier scoped to the resource (e.g., "51599233/df333e75-5896-4254-a69f-146736eaf668").
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;
}
