namespace Altinn.AccessManagement.Api.Enduser.Mappers;

/// <summary>
/// Defines a contract for mapping objects of type <typeparamref name="TFrom"/> to type <typeparamref name="TTo"/>.
/// </summary>
/// <typeparam name="TTo">The target type to map to.</typeparam>
/// <typeparam name="TFrom">The source type to map from.</typeparam>
public interface IMapper<out TTo, in TFrom>
{
    /// <summary>
    /// Maps an instance of <typeparamref name="TFrom"/> to an instance of <typeparamref name="TTo"/>.
    /// </summary>
    /// <param name="from">The source object to map from.</param>
    /// <returns>A mapped instance of <typeparamref name="TTo"/>.</returns>
    TTo Map(TFrom from);
}
