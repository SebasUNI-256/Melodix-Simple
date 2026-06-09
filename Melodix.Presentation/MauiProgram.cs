using Melodix.Application.Abstractions;
using Melodix.Application.Services;
using Melodix.Infrastructure.Extensions;
using Melodix.Presentation.Services;
using Melodix.Presentation.ViewModels;
using Microsoft.Extensions.Logging;

namespace Melodix.Presentation;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "melodix.db");

        builder.Services.AddMelodixInfrastructure(databasePath);
        builder.Services.AddSingleton<ILibrarySynchronizationService, LibrarySynchronizationService>();
        builder.Services.AddSingleton<LibraryManagementService>();
        builder.Services.AddSingleton<PlaybackController>();
        builder.Services.AddSingleton<IPlaybackService, WindowsPlaybackService>();
        builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
        builder.Services.AddSingleton<ITrackMetadataService, WindowsTrackMetadataService>();
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
