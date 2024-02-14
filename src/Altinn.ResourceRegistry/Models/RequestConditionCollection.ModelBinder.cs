#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Models.ModelBinding;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Altinn.ResourceRegistry.Models;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "Documented in main declaration file")]
public static partial class RequestConditionCollection
{
    /// <summary>
    /// Model binder for <see cref="RequestConditionCollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The entity tag type. Parsed as an <see cref="Opaque{T}"/>.</typeparam>
    private sealed class ModelBinder<T> : IModelBinder
        where T : notnull
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.IsNotNull(bindingContext);

            var modelName = bindingContext.ModelName;

            var headers = bindingContext.HttpContext.Request.Headers;
            if (headers == null)
            {
                bindingContext.Result = ModelBindingResult.Success(Empty<T>());
                return Task.CompletedTask;
            }

            var method = bindingContext.HttpContext.Request.Method;
            var isReadRequest = HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
            var hasErrors = false;
            ImmutableArray<IVersionedEntityCondition<T>>.Builder? builder = null;
            
            if (TryReadETags(modelName, bindingContext.ModelState, "If-Match", headers.IfMatch, ref hasErrors, out var conditions))
            {
                builder ??= ImmutableArray.CreateBuilder<IVersionedEntityCondition<T>>();
                if (conditions.IsDefault)
                {
                    builder.Add(RequestCondition.Exists<T>());
                }
                else
                {
                    builder.Add(RequestCondition.IsMatch(conditions));
                }
            }
            else
            {
                // Only read the If-Unmodified-Since header if If-Match is not present.
                if (TryReadDate(bindingContext.ModelState, "If-Unmodified-Since", headers.IfUnmodifiedSince, out var date))
                {
                    builder ??= ImmutableArray.CreateBuilder<IVersionedEntityCondition<T>>();
                    builder.Add(RequestCondition.IsUnmodifiedSince<T>(date));
                }
            }

            if (TryReadETags(modelName, bindingContext.ModelState, "If-None-Match", headers.IfNoneMatch, ref hasErrors, out conditions))
            {
                builder ??= ImmutableArray.CreateBuilder<IVersionedEntityCondition<T>>();
                if (conditions.IsDefault)
                {
                    builder.Add(RequestCondition.NotExists<T>(isReadRequest));
                }
                else
                {
                    builder.Add(RequestCondition.IsDifferent(conditions, isReadRequest));
                }
            }
            else
            {
                // Only read the If-Modified-Since header if If-None-Match is not present.
                if (TryReadDate(bindingContext.ModelState, "If-Modified-Since", headers.IfModifiedSince, out var date))
                {
                    builder ??= ImmutableArray.CreateBuilder<IVersionedEntityCondition<T>>();
                    builder.Add(RequestCondition.IsModifiedSince<T>(date, isReadRequest));
                }
            }

            if (hasErrors)
            {
                return Task.CompletedTask;
            }

            var result = builder is null ? Empty<T>() : Create(builder.DrainToImmutable());
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        /// <returns>
        /// <see langword="true"/> if the header values were present and successfully parsed, <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// The <paramref name="conditions"/> parameter will be set to <see langword="default"/> if the header value is the wildcard etag.
        /// </remarks>
        private static bool TryReadETags(string modelName, ModelStateDictionary modelState, string headerName, StringValues headerValues, ref bool hasErrors, out ImmutableArray<T> conditions)
        {
            Debug.Assert(modelState != null);
            Debug.Assert(headerName != null);

            if (StringValues.IsNullOrEmpty(headerValues))
            {
                conditions = default;
                return false;
            }

            if (!EntityTagHeaderValue.TryParseList(headerValues, out var etags))
            {
                modelState.TryAddModelError(modelName, $"Invalid {headerName} header.");
                hasErrors = true;
                conditions = default;
                return false;
            }

            if (etags.Count == 0)
            {
                conditions = default;
                return false;
            }

            if (etags is [{ IsWeak: false, Tag: var tag }] && tag == "*")
            {
                conditions = default;
                return true;
            }

            var builder = ImmutableArray.CreateBuilder<T>(etags.Count);
            foreach (var etag in etags)
            {
                if (!etag.IsWeak)
                {
                    modelState.TryAddModelError(modelName, $"Invalid {headerName} header. Only weak etags are supported.");
                    hasErrors = true;
                    continue;
                }

                if (!Opaque<T>.TryParse(etag.Tag.AsSpan()[1..^1], null, out var value))
                {
                    modelState.TryAddModelError(modelName, $"Invalid {headerName} header. Invalid etag.");
                    hasErrors = true;
                    continue;
                }

                builder.Add(value.Value);
            }

            if (hasErrors)
            {
                conditions = default;
                return false;
            }

            conditions = builder.MoveToImmutable();
            return true;
        }

        /// <returns>
        /// <see langword="true"/> if the header values were present and successfully parsed, <see langword="false"/> otherwise.
        /// </returns>
        private static bool TryReadDate(ModelStateDictionary modelState, string headerName, StringValues headerValues, out HttpDateTimeHeaderValue date)
        {
            Debug.Assert(modelState != null);
            Debug.Assert(headerName != null);

            if (StringValues.IsNullOrEmpty(headerValues))
            {
                date = default;
                return false;
            }

            if (headerValues.Count > 1)
            {
                // The spec requires that the header is ignored if it contains multiple values.
                date = default;
                return false;
            }

            if (!HeaderUtilities.TryParseDate(headerValues[0], out var parsed))
            {
                // The spec requires that the header is ignored if it contains an invalid date.
                date = default;
                return false;
            }

            date = new(parsed);
            return true;
        }
    }

    /// <summary>
    /// <see cref="IModelBinderProvider"/> for <see cref="RequestConditionCollection{T}"/>.
    /// </summary>
    internal sealed class ModelBinderProvider 
        : IModelBinderProvider
        , ISingleton<ModelBinderProvider>
    {
        /// <summary>
        /// Gets a singleton instance of <see cref="ModelBinderProvider"/>.
        /// </summary>
        public static ModelBinderProvider Instance { get; } = new ModelBinderProvider();

        private ModelBinderProvider()
        {
        }

        private ImmutableDictionary<Type, IModelBinder> _binders = ImmutableDictionary<Type, IModelBinder>.Empty;

        /// <inheritdoc/>
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            Guard.IsNotNull(context);

            if (GetETagType(context.Metadata.ModelType) is { } etagType)
            {
                return ImmutableInterlocked.GetOrAdd(ref _binders, etagType, CreateBinder);
            }

            return null;
        }

        private static IModelBinder CreateBinder(Type etagType)
        {
            return new BinderTypeModelBinder(typeof(ModelBinder<>).MakeGenericType(etagType));
        }
    }

    /// <summary>
    /// <see cref="IBindingSourceMetadata"/> for <see cref="RequestConditionCollection{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal sealed class BindingSourceAttribute : Attribute, IBindingSourceMetadata
    {
        /// <inheritdoc/>
        public BindingSource? BindingSource => BindingSource.Header;
    }
}
