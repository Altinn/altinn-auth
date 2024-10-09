using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using Altinn.Authorization.DeployApi.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Nerdbank.Streams;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Altinn.Authorization.DeployApi.Pipelines;

public sealed class PipelineContext
    : IServiceProvider
    , ISupportRequiredService
    , IKeyedServiceProvider
{
    internal static async Task Run(TaskPipeline pipeline, HttpContext context)
    {
        var ct = context.RequestAborted;
        var responseBody = context.Features.Get<IHttpResponseBodyFeature>()!;
        responseBody.DisableBuffering();

        var response = context.Response;
        response.StatusCode = 200;
        response.ContentType = "text/plain; charset=utf-8";

        await responseBody.StartAsync(ct);

        ////await using var textWriter = new StreamWriter(responseBody.Stream, Encoding.UTF8);
        await Run(pipeline, responseBody.Writer, context.RequestServices, ct);

        await responseBody.CompleteAsync();
    }

    internal static async Task<TaskPipelineResult> Run(TaskPipeline pipeline, WebSocket context, IServiceProvider services, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pipe = new Pipe();
        var ct = cts.Token;
        var reader = pipe.Reader;
        var writer = pipe.Writer;

        var readerTask = Task.Run(
            async () =>
            {
                ReadResult result;

                do
                {
                    result = await reader.ReadAsync(ct);
                    if (result.IsCanceled)
                    {
                        break;
                    }

                    var buffer = result.Buffer;
                    if (buffer.IsSingleSegment)
                    {
                        var segment = buffer.First;
                        await context.SendAsync(segment, WebSocketMessageType.Binary, true, ct);
                    }
                    else
                    {
                        foreach (var segment in buffer)
                        {
                            await context.SendAsync(segment, WebSocketMessageType.Binary, false, ct);
                        }

                        await context.SendAsync(ArraySegment<byte>.Empty, WebSocketMessageType.Binary, true, ct);
                    }

                    reader.AdvanceTo(buffer.End);
                }
                while (!result.IsCompleted);
            },
            ct);

        try
        {
            return await Run(pipeline, writer, services, ct);
        }
        catch (OperationCanceledException e) when (e.CancellationToken == ct)
        {
            return TaskPipelineResult.Canceled;
        }
        finally
        {
            await writer.CompleteAsync();

            try
            {
                await readerTask;
            }
            catch (OperationCanceledException e) when (e.CancellationToken == ct)
            {
            }
            catch (Exception e)
            {
            }
        }
    }

    private static async Task<TaskPipelineResult> Run(TaskPipeline pipeline, PipeWriter writer, IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var textWriter = new BufferTextWriter(writer, Encoding.UTF8);
        var consoleOutput = new ConsoleOutput(textWriter);
        var console = new Console(
            AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Yes,
                ColorSystem = ColorSystemSupport.EightBit,
                Out = consoleOutput,
                Interactive = InteractionSupport.Yes,
            }),
            textWriter,
            writer);

        console.WriteLine();
        var progress = console
            .Progress()
            .Columns([
                new TaskOutcomeColumn(),
                new TaskDescriptionColumn { Alignment = Justify.Left },
            ]);

        var ctx = new PipelineContext(console, progress, services);
        try
        {
            await pipeline.ExecuteAsync(ctx, cancellationToken);
            return TaskPipelineResult.Ok;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return TaskPipelineResult.Canceled;
        }
        catch (Exception ex)
        {
            try
            {
                console.WriteLine();
                console.WriteLine("::error ::Failed to create database");
                console.WriteException(ex);
            }
            catch
            {
                // Ignore exceptions from the exception handler.
            }

            return TaskPipelineResult.Error;
        }
    }

    private readonly IAnsiConsole _console;
    private readonly Progress _progress;
    private readonly IServiceProvider _services;

    private PipelineContext(IAnsiConsole console, Progress progress, IServiceProvider services)
    {
        _console = console;
        _progress = progress;
        _services = services;
    }

    object? IServiceProvider.GetService(Type serviceType)
        => _services.GetService(serviceType);

    object ISupportRequiredService.GetRequiredService(Type serviceType)
        => _services.GetRequiredService(serviceType);

    object? IKeyedServiceProvider.GetKeyedService(Type serviceType, object? serviceKey)
        => _services.GetKeyedServices(serviceType, serviceKey);

    object IKeyedServiceProvider.GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => _services.GetRequiredKeyedService(serviceType, serviceKey);

    public Task<T> RunTask<T>(StepTask<T> task, CancellationToken cancellationToken)
    {
        return RunTask(task.Name, task.ExecuteAsync, cancellationToken);
    }

    public Task RunTask(StepTask task, CancellationToken cancellationToken)
    {
        return RunTask(task.Name, task.ExecuteAsync, cancellationToken);
    }

    public Task RunTask(string description, Func<ProgressTask, CancellationToken, Task> task, CancellationToken cancellationToken)
    {
        return RunTask<object?>(
            description,
            async (ctx, ct) =>
            {
                await task(ctx, ct);
                return null;
            },
            cancellationToken);
    }

    public Task<T> RunTask<T>(string description, Func<ProgressTask, CancellationToken, Task<T>> task, CancellationToken cancellationToken)
    {
        return _progress.StartAsync(async ctx =>
        {
            var taskCtx = ctx.AddTask(description, autoStart: true);
            taskCtx.IsIndeterminate = true;

            var succeeded = false;
            try
            {
                var result = await task(taskCtx, cancellationToken);
                succeeded = true;
                return result;
            }
            finally
            {
                taskCtx.StopTask();
                taskCtx.Value(taskCtx.MaxValue);
                taskCtx.State.Update<TaskOutcome>("task:outcome", v =>
                {
                    if (v == TaskOutcome.None)
                    {
                        return succeeded ? TaskOutcome.Ok : TaskOutcome.Error;
                    }

                    return v;
                });
            }
        });
    }

    private class ConsoleOutput(TextWriter writer)
        : IAnsiConsoleOutput
    {
        public TextWriter Writer => writer;

        public bool IsTerminal => true;

        public int Width => 80;

        public int Height => 80;

        public void SetEncoding(Encoding encoding)
        {
        }
    }

    private class Console(IAnsiConsole inner, TextWriter writer, PipeWriter innerWriter)
        : IAnsiConsole
    {
        public Profile Profile => inner.Profile;

        public IAnsiConsoleCursor Cursor => inner.Cursor;

        public IAnsiConsoleInput Input => inner.Input;

        public IExclusivityMode ExclusivityMode => inner.ExclusivityMode;

        public RenderPipeline Pipeline => inner.Pipeline;

        public void Clear(bool home)
        {
            inner.Clear(home);
        }

        public void Write(IRenderable renderable)
        {
            inner.Write(renderable);
            writer.Flush();
            innerWriter.FlushAsync().AsTask().Wait();
        }
    }

    private class TaskOutcomeColumn
        : ProgressColumn
    {
        private static readonly IRenderable Checkmark = new Markup("[green]✓[/]");
        private static readonly IRenderable Cross = new Markup("[red]✗[/]");

        private readonly ProgressColumn _spinner;

        public TaskOutcomeColumn()
        {
            _spinner = new SpinnerColumn
            {
                Style = Style.Parse("green"),
            };
        }

        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            var outcome = task.State.Get<TaskOutcome>("task:outcome");

            return outcome switch
            {
                TaskOutcome.Ok => Checkmark,
                TaskOutcome.Error => Cross,
                _ => _spinner.Render(options, task, deltaTime),
            };
        }
    }

    private enum TaskOutcome
    {
        None = default,
        Ok,
        Error,
    }
}

public enum TaskPipelineResult
{
    Ok,
    Error,
    Canceled,
}
