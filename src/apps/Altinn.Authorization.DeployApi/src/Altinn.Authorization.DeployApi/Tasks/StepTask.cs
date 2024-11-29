using Spectre.Console;

namespace Altinn.Authorization.DeployApi.Tasks;

public abstract class StepTask
{
    public abstract string Name { get; }

    public abstract new Task ExecuteAsync(ProgressTask task, CancellationToken cancellationToken);
}

public abstract class StepTask<T>
{
    public abstract string Name { get; }

    public abstract Task<T> ExecuteAsync(ProgressTask task, CancellationToken cancellationToken);
}
