using System.Data;

namespace Altinn.AccessMgmt.Persistence.Core.Utilities;

/// <summary>
/// Defines methods for converting data from an <see cref="IDataReader"/> into a collection of objects.
/// </summary>
public interface IDbConverter
{
    /// <summary>
    /// Converts the data from the provided <see cref="IDataReader"/> into a list of objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of objects to create. The type must have a parameterless constructor.
    /// </typeparam>
    /// <param name="reader">The data reader that contains the data to convert.</param>
    /// <returns>
    /// A list of objects of type <typeparamref name="T"/> constructed from the data read.
    /// </returns>
    List<T> ConvertToObjects<T>(IDataReader reader) 
        where T : new();
}
