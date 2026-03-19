using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.ServiceOwner.Validation;

public static class ValidationErrorDescriptors
{
    private static readonly ValidationErrorDescriptorFactory _factory = ValidationErrorDescriptorFactory.New("AMSO");

    public static ValidationErrorDescriptor InvalidUrn { get; } = _factory.Create(1, "Invalid URN");

    public static ValidationErrorDescriptor NotFound { get; } = _factory.Create(2, "Object not found");

    public static ValidationErrorDescriptor RequestedResourceNotFound { get; } = _factory.Create(3, $"Requested resource was not found");

    public static ValidationErrorDescriptor RequestedPackageNotFound { get; } = _factory.Create(4, $"Requested package was not found");

    public static ValidationErrorDescriptor RequestResourceOrPackage { get; } = _factory.Create(5, $"Request can only contain package or resource");
}
