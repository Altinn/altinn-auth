using System.Net;
using System.Text;
using System.Text.Json;
using Altinn.Authorization.Integration.Platform;

namespace Altinn.Authorization.Integration.Tests;

/// <summary>
/// Pure-unit tests for <see cref="ResponseComposer"/> (no external dependencies).
/// </summary>
public class ResponseComposerTest
{
    // ── Handle ────────────────────────────────────────────────────────────────

    [Fact]
    public void Handle_SuccessResponse_IsSuccessfulTrueAndStatusCodeSet()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") };

        var result = ResponseComposer.Handle<string>(httpResponse);

        Assert.True(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public void Handle_FailedResponse_IsSuccessfulFalseAndStatusCodeSet()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{}") };

        var result = ResponseComposer.Handle<string>(httpResponse);

        Assert.False(result.IsSuccessful);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public void Handle_AppliesAllHandlersInOrder()
    {
        var callOrder = new List<int>();
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") };

        ResponseComposer.Handle<string>(
            httpResponse,
            _ => callOrder.Add(1),
            _ => callOrder.Add(2));

        Assert.Equal([1, 2], callOrder);
    }

    // ── DeserializeResponseOnSuccess ──────────────────────────────────────────

    [Fact]
    public void DeserializeResponseOnSuccess_SuccessWithJsonContent_DeserializesContent()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(@"""hello world""", Encoding.UTF8, "application/json")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserializeResponseOnSuccess);

        Assert.Equal("hello world", result.Content);
    }

    [Fact]
    public void DeserializeResponseOnSuccess_FailedResponse_ContentIsDefault()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(@"""should-not-be-read""", Encoding.UTF8, "application/json")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserializeResponseOnSuccess);

        Assert.Null(result.Content);
    }

    // ── DeserializeProblemDetailsOnUnsuccessStatusCode ────────────────────────

    [Fact]
    public void DeserializeProblemDetailsOnUnsuccessStatusCode_FailedWithValidJson_SetsProblemDetails()
    {
        const string problemJson = """{"type":"about:blank","title":"Not Found","status":404}""";
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode);

        Assert.NotNull(result.ProblemDetails);
    }

    [Fact]
    public void DeserializeProblemDetailsOnUnsuccessStatusCode_SuccessResponse_NoProblemDetails()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode);

        Assert.Null(result.ProblemDetails);
    }

    [Fact]
    public void DeserializeProblemDetailsOnUnsuccessStatusCode_InvalidJson_DoesNotThrow()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("not-json")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode);

        Assert.Null(result.ProblemDetails);
    }

    // ── DeserilizeProblemDetailsOnStatusCode ─────────────────────────────────

    [Fact]
    public void DeserilizeProblemDetailsOnStatusCode_MatchingStatus_SetsProblemDetails()
    {
        const string problemJson = """{"type":"about:blank","title":"Conflict","status":409}""";
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserilizeProblemDetailsOnStatusCode<string>(HttpStatusCode.Conflict));

        Assert.NotNull(result.ProblemDetails);
    }

    [Fact]
    public void DeserilizeProblemDetailsOnStatusCode_NonMatchingStatus_NoProblemDetails()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{}")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserilizeProblemDetailsOnStatusCode<string>(HttpStatusCode.Conflict));

        Assert.Null(result.ProblemDetails);
    }

    // ── SetBodyAsStringResultIfSuccesful ──────────────────────────────────────

    [Fact]
    public void SetBodyAsStringResultIfSuccesful_Success_SetsContentToBody()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("raw-body")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.SetBodyAsStringResultIfSuccesful);

        Assert.Equal("raw-body", result.Content);
    }

    [Fact]
    public void SetBodyAsStringResultIfSuccesful_Failure_ContentIsNull()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("ignored")
        };

        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.SetBodyAsStringResultIfSuccesful);

        Assert.Null(result.Content);
    }

    // ── ConfigureResultIfSuccessful ───────────────────────────────────────────

    [Fact]
    public void ConfigureResultIfSuccessful_Success_InvokesCallback()
    {
        const string jsonValue = @"""from-server""";
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonValue, Encoding.UTF8, "application/json")
        };

        string? captured = null;
        var result = ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.DeserializeResponseOnSuccess,
            ResponseComposer.ConfigureResultIfSuccessful<string>(v => captured = v + "-modified"));

        Assert.Equal("from-server-modified", captured);
    }

    [Fact]
    public void ConfigureResultIfSuccessful_Failure_DoesNotInvokeCallback()
    {
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("{}")
        };

        bool invoked = false;
        ResponseComposer.Handle<string>(
            httpResponse,
            ResponseComposer.ConfigureResultIfSuccessful<string>(_ => invoked = true));

        Assert.False(invoked);
    }
}
