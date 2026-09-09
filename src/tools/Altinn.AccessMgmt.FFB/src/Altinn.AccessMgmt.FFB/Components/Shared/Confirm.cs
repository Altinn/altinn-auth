using MudBlazor;

namespace Altinn.AccessMgmt.FFB.Components.Shared;

/// <summary>
/// Convenience wrapper around <see cref="ConfirmDialog"/> so pages confirm
/// destructive actions the same way.
/// </summary>
public static class Confirm
{
    /// <summary>
    /// Shows the confirmation dialog and returns true when the user confirms.
    /// </summary>
    public static async Task<bool> ShowAsync(
        IDialogService dialogs,
        string title,
        string contentText,
        string buttonText,
        Color color = Color.Error)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, contentText },
            { x => x.ButtonText, buttonText },
            { x => x.Color, color },
        };

        var dialog = await dialogs.ShowAsync<ConfirmDialog>(title, parameters);
        var result = await dialog.Result;
        return result is { Canceled: false };
    }
}
