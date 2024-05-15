using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Extensions;

/// <summary>
/// Extensions for <see cref="ProblemDetails"/>.
/// </summary>
internal static class ProblemDetailsExtensions
{
    /// <summary>
    /// Converts a <see cref="ProblemDetails"/> to an <see cref="ActionResult"/>.
    /// </summary>
    /// <param name="problemDetails">The <see cref="ProblemDetails"/>.</param>
    /// <returns></returns>
    public static ActionResult ToActionResult(this ProblemDetails problemDetails)
        => new HttpActionResult(TypedResults.Problem(problemDetails));

    /// <summary>
    /// An <see cref="ActionResult"/> that when executed will produce a response based on the <see cref="IResult"/> provided.
    /// </summary>
    private sealed class HttpActionResult : ActionResult
    {
        /// <summary>
        /// Gets the instance of the current <see cref="IResult"/>.
        /// </summary>
        public IResult Result { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpActionResult"/> class with the
        /// <see cref="IResult"/> provided.
        /// </summary>
        /// <param name="result">The <see cref="IResult"/> instance to be used during the <see cref="ExecuteResultAsync"/> invocation.</param>
        public HttpActionResult(IResult result)
        {
            Result = result;
        }

        /// <inheritdoc/>
        public override Task ExecuteResultAsync(ActionContext context)
            => Result.ExecuteAsync(context.HttpContext);
    }
}
