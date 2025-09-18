using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Represents a strongly typed constant definition with a fixed unique identifier (GUID),
/// an associated entity instance of type <typeparamref name="T"/>, and optional
/// translation entries for English (EN) and Norwegian Nynorsk (NN).
/// </summary>
/// <typeparam name="T">
/// The entity type this constant definition represents. Must implement <see cref="IEntityId"/>.
/// </typeparam>
public sealed class ConstantDefinition<T>(Guid id)
    where T : IEntityId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConstantDefinition{T}"/> class from a string identifier.
    /// </summary>
    /// <param name="id">A string representation of the unique GUID identifier.</param>
    public ConstantDefinition(string id)
        : this(Guid.Parse(id)) { }

    private readonly Guid _id = id;

    private T _entity = default!;
    private TranslationEntryList? _en;
    private TranslationEntryList? _nn;

    /// <summary>
    /// Gets the unique identifier (GUID) for this constant definition.
    /// </summary>
    public Guid Id => _id;

    /// <summary>
    /// Gets or initializes the associated entity of type <typeparamref name="T"/>.
    /// The entity is required and will automatically have its <see cref="IEntityId.Id"/> set to <see cref="Id"/>.
    /// </summary>
    public required T Entity
    {
        get => _entity;
        init
        {
            _entity = value;
            _entity.Id = _id;
        }
    }

    /// <summary>
    /// Gets or initializes the English translation entries for this entity.
    /// When initialized, the translation entry list will automatically be assigned this constant's <see cref="Id"/>,
    /// language code (<c>"eng"</c>), and entity type name.
    /// </summary>
    public TranslationEntryList? EN
    {
        get => _en;
        init
        {
            _en = value;
            if (_en is not null)
            {
                _en.Id = _id;
                _en.LanguageCode = "eng";
                _en.Type = typeof(T).Name;
            }
        }
    }

    /// <summary>
    /// Gets or initializes the Norwegian Nynorsk translation entries for this entity.
    /// When initialized, the translation entry list will automatically be assigned this constant's <see cref="Id"/>,
    /// language code (<c>"nno"</c>), and entity type name.
    /// </summary>
    public TranslationEntryList? NN
    {
        get => _nn;
        init
        {
            _nn = value;
            if (_nn is not null)
            {
                _nn.Id = _id;
                _nn.LanguageCode = "nno";
                _nn.Type = typeof(T).Name;
            }
        }
    }

    /// <summary>
    /// Implicitly converts a <see cref="ConstantDefinition{T}"/> to its unique identifier <see cref="Guid"/>.
    /// </summary>
    /// <param name="def">The constant definition.</param>
    public static implicit operator Guid(ConstantDefinition<T> def)
        => def._id;

    /// <summary>
    /// Implicitly converts a <see cref="ConstantDefinition{T}"/> to its associated entity instance of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="def">The constant definition.</param>
    public static implicit operator T(ConstantDefinition<T> def)
        => def._entity;

    /// <summary>
    /// Implicitly converts a <see cref="ConstantDefinition{T}"/> to a list of <see cref="TranslationEntry"/> objects,
    /// combining entries from both <see cref="EN"/> and <see cref="NN"/> (if available).
    /// </summary>
    /// <param name="def">The constant definition.</param>
    public static implicit operator List<TranslationEntry>(ConstantDefinition<T> def)
    {
        var result = new List<TranslationEntry>();

        if (def._en is { })
        {
            result.AddRange(def._en.SingleEntries());
        }

        if (def._nn is { })
        {
            result.AddRange(def._nn.SingleEntries());
        }

        return result;
    }
}
