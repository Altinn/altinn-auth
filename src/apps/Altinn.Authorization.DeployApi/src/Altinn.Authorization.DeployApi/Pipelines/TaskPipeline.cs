using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using Nerdbank.Streams;

namespace Altinn.Authorization.DeployApi.Pipelines;

internal abstract class TaskPipeline
{
    protected internal abstract Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken);

    public Task Run(HttpContext context)
        => PipelineContext.Run(this, context);

    public Task<TaskPipelineResult> Run(WebSocket context, IServiceProvider services, CancellationToken cancellationToken)
        => PipelineContext.Run(this, context, services, cancellationToken);
}

internal static class TaskPipelineExtensions
{
    private static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);

    public static IEndpointConventionBuilder MapTaskPipeline<TPipeline>(this IEndpointRouteBuilder endpoints, string pattern)
        where TPipeline : TaskPipeline
        => endpoints.Map(pattern, async (HttpContext context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync("altinn.task-pipeline");
                TPipeline? pipeline;

                {
                    using var sequence = new Sequence<byte>(ArrayPool<byte>.Shared);
                    var result = await webSocket.ReceiveAsync(sequence, context.RequestAborted);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", context.RequestAborted);
                        return;
                    }

                    pipeline = DeserializePipeline<TPipeline>(sequence.AsReadOnlySequence);
                }

                if (pipeline is null)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, "Missing pipeline payload", context.RequestAborted);
                    return;
                }

                TaskPipelineResult pipelineResult;
                try
                {
                    pipelineResult = await pipeline.Run(webSocket, context.RequestServices, context.RequestAborted);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == context.RequestAborted)
                {
                    pipelineResult = TaskPipelineResult.Canceled;
                }
                catch (Exception)
                {
                    pipelineResult = TaskPipelineResult.Error;
                }

                var (closeCode, closeDescription) = pipelineResult switch
                {
                    TaskPipelineResult.Ok => ((WebSocketCloseStatus)4000, "ok"),
                    TaskPipelineResult.Canceled => ((WebSocketCloseStatus)4001, "canceled"),
                    TaskPipelineResult.Error => ((WebSocketCloseStatus)4002, "error"),
                    _ => (WebSocketCloseStatus.InternalServerError, "unexpected pipeline result"),
                };

                await webSocket.CloseAsync(closeCode, closeDescription, context.RequestAborted);
            }
            else if (HttpMethods.IsPost(context.Request.Method))
            {
                var pipeline = await context.Request.ReadFromJsonAsync<TPipeline>(Options, context.RequestAborted);
                if (pipeline is null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                await pipeline.Run(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
        });

    private static TPipeline? DeserializePipeline<TPipeline>(ReadOnlySequence<byte> sequence)
        where TPipeline : TaskPipeline
    {
        var reader = new Utf8JsonReader(sequence);
        return JsonSerializer.Deserialize<TPipeline>(ref reader, Options);
    }

    private static async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(this WebSocket webSocket, IBufferWriter<byte> writer, CancellationToken cancellationToken)
    {
        ValueWebSocketReceiveResult result;

        do
        {
            var memory = writer.GetMemory(4096);
            result = await webSocket.ReceiveAsync(memory, cancellationToken);
            writer.Advance(result.Count);
        } 
        while (!result.EndOfMessage);

        return result;
    }
}