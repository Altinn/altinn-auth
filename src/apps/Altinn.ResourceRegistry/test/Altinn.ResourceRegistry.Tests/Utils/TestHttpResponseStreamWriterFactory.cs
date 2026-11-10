using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Altinn.ResourceRegistry.Tests.Utils;

public class TestHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
{
    public const int DefaultBufferSize = 16 * 1024;

    public TextWriter CreateWriter(Stream stream, Encoding encoding)
    {
        return new HttpResponseStreamWriter(stream, encoding, DefaultBufferSize);
    }
}
