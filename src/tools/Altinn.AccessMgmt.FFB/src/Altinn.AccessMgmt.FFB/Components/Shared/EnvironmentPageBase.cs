using Altinn.AccessMgmt.FFB.Services;
using Microsoft.AspNetCore.Components;

namespace Altinn.AccessMgmt.FFB.Components.Shared;

/// <summary>
/// Base class for pages that load data per environment. Owns the page-blocking
/// <see cref="Loading"/> flag, the page-level <see cref="Error"/> message, and the
/// <see cref="EnvironmentState"/> subscription. Pages that override
/// <see cref="OnInitialized"/> must call <c>base.OnInitialized()</c>.
/// </summary>
public abstract class EnvironmentPageBase : ComponentBase, IDisposable
{
    private CancellationTokenSource? _cts;
    private int _version;

    [Inject]
    protected EnvironmentState EnvState { get; set; } = default!;

    /// <summary>The single page-blocking busy flag, managed by <see cref="RunAsync(Func{CancellationToken, Task})"/>.</summary>
    protected bool Loading { get; private set; }

    /// <summary>
    /// Page-level error message, set from <see cref="ErrorText.Flatten"/> by <see cref="RunAsync(Func{CancellationToken, Task})"/>.
    /// Pages may also set it directly for input validation, or clear it when resetting state.
    /// </summary>
    protected string? Error { get; set; }

    protected override void OnInitialized()
    {
        EnvState.OnChange += HandleEnvironmentChanged;
    }

    /// <summary>
    /// Unsubscribes from environment changes and cancels any in-flight run.
    /// Overrides must call <c>base.Dispose()</c>.
    /// </summary>
    public virtual void Dispose()
    {
        EnvState.OnChange -= HandleEnvironmentChanged;
        _cts?.Cancel();
    }

    /// <summary>
    /// Runs page work with Loading/Error handling and re-renders before and after.
    /// </summary>
    protected Task RunAsync(Func<Task> work) => RunAsync(_ => work());

    /// <summary>
    /// Runs page work with Loading/Error handling and re-renders before and after.
    /// Starting a new run cancels the previous one and takes over the page state,
    /// so a stale load (e.g. from before an environment switch) cannot overwrite
    /// a newer one's data, Loading flag, or Error.
    /// </summary>
    protected async Task RunAsync(Func<CancellationToken, Task> work)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var version = ++_version;

        Loading = true;
        Error = null;
        StateHasChanged();

        try
        {
            await work(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Superseded by a newer run, which owns the page state now.
        }
        catch (Exception ex)
        {
            if (version == _version)
            {
                Error = ErrorText.Flatten(ex);
            }
        }
        finally
        {
            if (version == _version)
            {
                Loading = false;
                StateHasChanged();
            }

            if (ReferenceEquals(_cts, cts))
            {
                _cts = null;
            }

            cts.Dispose();
        }
    }

    /// <summary>
    /// Called when the selected environment changes. Details pages reload their data;
    /// tool pages clear stale results.
    /// </summary>
    protected virtual Task OnEnvironmentChangedAsync() => Task.CompletedTask;

    private void HandleEnvironmentChanged()
    {
        // Invalidate any in-flight run. Details pages start a new run (which also
        // cancels), but tool pages only clear their fields — without this cancel the
        // old run would keep going and write the previous environment's results back
        // after the clear. Deliberately no version bump: the cancelled run's finally
        // block must still clear Loading, since no new run may be starting.
        _cts?.Cancel();

        _ = InvokeAsync(async () =>
        {
            await OnEnvironmentChangedAsync();
            StateHasChanged();
        });
    }
}
