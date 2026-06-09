using Microsoft.Maui.Platform;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Melodix.Presentation.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}

public sealed class FolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
        if (currentWindow is null)
        {
            return null;
        }

        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(currentWindow));

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
