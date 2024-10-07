using System.Text;
using Altinn.Authorization.DeployApi.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Altinn.Authorization.DeployApi.Pipelines;

public sealed class PipelineContext
    : IServiceProvider
    , ISupportRequiredService
    , IKeyedServiceProvider
{
    internal static async Task Run(Pipeline pipeline, HttpContext context, CancellationToken cancellationToken)
    {
        var responseBody = context.Features.Get<IHttpResponseBodyFeature>()!;
        responseBody.DisableBuffering();

        var response = context.Response;
        response.StatusCode = 200;
        response.ContentType = "text/plain; charset=utf-8";

        await responseBody.StartAsync(cancellationToken);

        await using var textWriter = new StreamWriter(responseBody.Stream, Encoding.UTF8);
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
            responseBody.Stream);

        console.WriteLine();
        var progress = console
            .Progress()
            .Columns([
                new TaskOutcomeColumn(),
                new TaskDescriptionColumn { Alignment = Justify.Left },
            ]);

        var ctx = new PipelineContext(console, progress, context);
        try
        {
            await pipeline.ExecuteAsync(ctx, cancellationToken);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
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
        }

        await responseBody.CompleteAsync();
    }

    private readonly IAnsiConsole _console;
    private readonly Progress _progress;
    private readonly HttpContext _context;

    private PipelineContext(IAnsiConsole console, Progress progress, HttpContext context)
    {
        _console = console;
        _progress = progress;
        _context = context;
    }

    object? IServiceProvider.GetService(Type serviceType)
        => _context.RequestServices.GetService(serviceType);

    object ISupportRequiredService.GetRequiredService(Type serviceType)
        => _context.RequestServices.GetRequiredService(serviceType);

    object? IKeyedServiceProvider.GetKeyedService(Type serviceType, object? serviceKey)
        => _context.RequestServices.GetKeyedServices(serviceType, serviceKey);

    object IKeyedServiceProvider.GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => _context.RequestServices.GetRequiredKeyedService(serviceType, serviceKey);

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

    private class Console(IAnsiConsole inner, TextWriter writer, Stream stream)
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
            stream.Flush();
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
