namespace Altinn.Authorization.DeployApi.Pipelines;

internal abstract class Pipeline
{
    protected internal abstract Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken);

    public Task Run(HttpContext context)
        => PipelineContext.Run(this, context, context.RequestAborted);
}
