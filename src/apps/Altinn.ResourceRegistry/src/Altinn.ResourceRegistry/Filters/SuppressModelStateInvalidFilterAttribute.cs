#nullable enable

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Altinn.ResourceRegistry.Filters;

/// <summary>
/// Attribute that removes automatic model validation from an action. 
/// Used to prevent ASP.NET Core from automatically returning 400 Bad Request when model state is invalid.
/// </summary>
public class SuppressModelStateInvalidFilterAttribute : Attribute, IActionModelConvention
{
    private const string FilterTypeName = "ModelStateInvalidFilterFactory";

    /// <inheritdoc/>
    public void Apply(ActionModel action)
    {
        for (var i = 0; i < action.Filters.Count; i++)
        {
            if (action.Filters[i].GetType().Name == FilterTypeName)
            {
                action.Filters.RemoveAt(i);
                break;
            }
        }
    }
}
