using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Altinn.Platform.Events.Formatters
{
    /// <summary>
    /// A <see cref="TextOutputFormatter"/> that parses HTTP requests into CloudEvents.
    /// Inspired by: https://github.com/cloudevents/sdk-csharp/blob/main/samples/CloudNative.CloudEvents.AspNetCoreSample/CloudEventJsonOutputFormatter.cs
    /// </summary>
    public class RdfOutputFormatter : TextOutputFormatter
    {
        /// <summary>
        /// Constructs a new instance that uses the given formatter for deserialization.
        /// </summary>
        public RdfOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml+rdf"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        /// <inheritdoc />
        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;
            await response.WriteAsync(context.Object.ToString());
        }
    }
}