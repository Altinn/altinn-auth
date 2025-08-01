using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Altinn.AccessManagement.Api.Internal.Models;

/// <summary>
/// Page Inputs.
/// </summary>
public class PagingInput : IExamplesProvider<PagingInput>
{
    /// <summary>
    /// Number of elements requested from page. 
    /// </summary>
    [FromHeader(Name = "X-Page-Size")]
    [DefaultValue(100)]
    [SwaggerSchema(Description = "Page Size", Format = "[0, 100]")]
    public uint PageSize { get; set; } = 100;

    /// <summary>
    /// Number of paged skipped
    /// </summary>
    [DefaultValue(0)]
    [FromHeader(Name = "X-Page-Number")]
    [SwaggerSchema(Description = "Page Number", Format = "[0, ∞)")]
    public uint PageNumber { get; set; } = 0;

    /// <inheritdoc/>
    public PagingInput GetExamples()
    {
        return new PagingInput
        {
            PageNumber = 2,
            PageSize = 56,
        };
    }

    internal string ToOpaqueToken() =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(this));

    static internal PagingInput CreateFromToken(string token) =>
        JsonSerializer.Deserialize<PagingInput>(Convert.FromBase64String(token));
}
