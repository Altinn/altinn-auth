using System.Text;
using System.Xml;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.Authorization.ABAC.Tests;

/// <summary>
/// Round-trip tests for <see cref="XacmlSerializer"/> — serializing a decision
/// response back to XACML 3.0 XML. Exercises the serializer and the context-result
/// object model, which the parser-driven PDP tests only read.
/// </summary>
[UnitTest]
public class XacmlSerializerTest
{
    [Fact]
    public void WriteContextResponse_PermitResult_WritesPermitDecision()
    {
        var response = new XacmlContextResponse(
            new XacmlContextResult(XacmlContextDecision.Permit)
            {
                Status = new XacmlContextStatus(XacmlContextStatusCode.Success),
            });

        string xml = Serialize(writer => XacmlSerializer.WriteContextResponse(writer, response));

        xml.Should().Contain("Result");
        xml.Should().Contain("Permit");
    }

    [Fact]
    public void WriteContextResponse_DenyResult_WritesDenyDecision()
    {
        var response = new XacmlContextResponse(
            new XacmlContextResult(XacmlContextDecision.Deny)
            {
                Status = new XacmlContextStatus(XacmlContextStatusCode.Success),
            });

        string xml = Serialize(writer => XacmlSerializer.WriteContextResponse(writer, response));

        xml.Should().Contain("Deny");
    }

    private static string Serialize(Action<XmlWriter> write)
    {
        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb))
        {
            write(writer);
        }

        return sb.ToString();
    }
}
