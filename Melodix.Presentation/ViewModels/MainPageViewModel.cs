using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melodix.Application.DTOs;
using Melodix.Application.Services;
using Melodix.Presentation.Services;
using Microsoft.Maui.Graphics;

namespace Melodix.Presentation.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly LibraryManagementService _libraryManagementService;
    private readonly PlaybackController _playbackController;
    private readonly PlaybackQueueService _playbackQueueService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly ITrackMetadataService _trackMetadataService;

    private bool _hasLibraryFolder;
    private string _currentFolderPath = string.Empty;
    private string _emptyStateTitle = "Tu biblioteca esta vacia";
    private string _emptyStateMessage = "Agrega una carpeta para que Melodix busque archivos .mp4, .m4a y .flac.";
    private string _currentTrackTitle = "Nada seleccionado";
    private string _currentArtistName = string.Empty;
    private string _playbackStatusText = "Elige una pista para empezar.";
    private string? _currentArtworkPath;
    private bool _isPlaying;
    private bool _isBusy;
    private bool _isShuffleEnabled;
    private bool _isSeekPreviewActive;
    private double _trackPositionSeconds;
    private double _trackDurationSeconds;
    private MediaTrackListItem? _selectedTrack;
    private PlaybackRepeatMode _repeatMode = PlaybackRepeatMode.Off;
    private IDispatcherTimer? _playbackTimer;
    private bool _playbackEventsAttached;

    // Inicializa el estado de la vista y sus comandos.
    public MainPageViewModel(
        LibraryManagementService libraryManagementService,
        PlaybackController playbackController,
        PlaybackQueueService playbackQueueService,
        IFolderPickerService folderPickerService,
        ITrackMetadataService trackMetadataService)
    {
        _libraryManagementService = libraryManagementService;
        _playbackController = playbackController;
        _playbackQueueService = playbackQueueService;
        _folderPickerService = folderPickerService;
        _trackMetadataService = trackMetadataService;

        Tracks = [];
        PickFolderCommand = new AsyncRelayCommand(PickFolderAsync);
        RefreshLibraryCommand = new AsyncRelayCommand(RefreshLibraryAsync);
        TogglePlayPauseCommand = new AsyncRelayCommand(TogglePlayPauseAsync);
        SelectTrackCommand = new AsyncRelayCommand<MediaTrackListItem?>(SelectTrackAsync);
        PlayPreviousTrackCommand = new AsyncRelayCommand(PlayPreviousTrackAsync);
        PlayNextTrackCommand = new AsyncRelayCommand(PlayNextTrackAsync);
        ToggleShuffleCommand = new RelayCommand(ToggleShuffle);
        CycleRepeatModeCommand = new RelayCommand(CycleRepeatMode);
    }

    public ObservableCollection<MediaTrackListItem> Tracks { get; }

    public bool HasLibraryFolder
    {
        get => _hasLibraryFolder;
        private set
        {
            if (SetProperty(ref _hasLibraryFolder, value))
            {
                OnPropertyChanged(nameof(IsLibraryEmpty));
                OnPropertyChanged(nameof(HasTracks));
            }
        }
    }

    public string CurrentFolderPath
    {
        get => _currentFolderPath;
        private set => SetProperty(ref _currentFolderPath, value);
    }

    public string EmptyStateTitle
    {
        get => _emptyStateTitle;
        private set => SetProperty(ref _emptyStateTitle, value);
    }

    public string EmptyStateMessage
    {
        get => _emptyStateMessage;
        private set => SetProperty(ref _emptyStateMessage, value);
    }

    public string CurrentTrackTitle
    {
        get => _currentTrackTitle;
        private set => SetProperty(ref _currentTrackTitle, value);
    }

    public string CurrentArtistName
    {
        get => _currentArtistName;
        private set
        {
            if (SetProperty(ref _currentArtistName, value))
            {
                OnPropertyChanged(nameof(HasCurrentArtist));
            }
        }
    }

    public string PlaybackStatusText
    {
        get => _playbackStatusText;
        private set => SetProperty(ref _playbackStatusText, value);
    }

    public string? CurrentArtworkPath
    {
        get => _currentArtworkPath;
        private set
        {
            if (SetProperty(ref _currentArtworkPath, value))
            {
                OnPropertyChanged(nameof(HasCurrentArtwork));
                OnPropertyChanged(nameof(HasCurrentArtworkPlaceholder));
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                OnPropertyChanged(nameof(PlayPauseButtonText));
                OnPropertyChanged(nameof(PlayPauseButtonIcon));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanTogglePlayback));
                OnPropertyChanged(nameof(CanNavigateTracks));
            }
        }
    }

    public bool IsShuffleEnabled
    {
        get => _isShuffleEnabled;
        private set
        {
            if (SetProperty(ref _isShuffleEnabled, value))
            {
                OnPropertyChanged(nameof(ShuffleButtonText));
                OnPropertyChanged(nameof(ShuffleButtonBackgroundColor));
                OnPropertyChanged(nameof(ShuffleButtonTextColor));
                OnPropertyChanged(nameof(ShuffleButtonIcon));
            }
        }
    }

    public double TrackPositionSeconds
    {
        get => _trackPositionSeconds;
        private set
        {
            if (SetProperty(ref _trackPositionSeconds, value))
            {
                OnPropertyChanged(nameof(ElapsedTimeText));
            }
        }
    }

    public double TrackDurationSeconds
    {
        get => _trackDurationSeconds;
        private set
        {
            if (SetProperty(ref _trackDurationSeconds, value))
            {
                OnPropertyChanged(nameof(DurationTimeText));
                OnPropertyChanged(nameof(CanSeek));
            }
        }
    }

    public MediaTrackListItem? SelectedTrack
    {
        get => _selectedTrack;
        private set
        {
            if (SetProperty(ref _selectedTrack, value))
            {
                OnPropertyChanged(nameof(CanTogglePlayback));
                OnPropertyChanged(nameof(CanNavigateTracks));
                OnPropertyChanged(nameof(CanSeek));
            }
        }
    }

    public bool IsLibraryEmpty => Tracks.Count == 0;

    public bool HasTracks => Tracks.Count > 0;

    public bool HasCurrentArtist => !string.IsNullOrWhiteSpace(CurrentArtistName);

    public bool HasCurrentArtwork => !string.IsNullOrWhiteSpace(CurrentArtworkPath);

    public bool HasCurrentArtworkPlaceholder => !HasCurrentArtwork;

    public bool CanTogglePlayback => !IsBusy && SelectedTrack is not null;

    public bool CanNavigateTracks => !IsBusy && Tracks.Count > 0;

    public bool CanSeek => SelectedTrack is not null && TrackDurationSeconds > 0;

    public string PlayPauseButtonText => IsPlaying ? "Pausar" : "Reproducir";

    public string PlayPauseButtonIcon => IsPlaying ? "⏸" : "▶";

    public string PreviousTrackIcon => "⏮";

    public string NextTrackIcon => "⏭";

    public string ShuffleButtonText => IsShuffleEnabled ? "Azar On" : "Azar";

    public string ShuffleButtonIcon => "⇄";

    public string RepeatButtonText => _repeatMode switch
    {
        PlaybackRepeatMode.All => "Repetir Todo",
        PlaybackRepeatMode.One => "Repetir 1",
        _ => "Repetir"
    };

    public string RepeatButtonIcon => _repeatMode == PlaybackRepeatMode.One ? "↺" : "↻";

    public Color ShuffleButtonBackgroundColor => IsShuffleEnabled ? Color.FromArgb("#244A56") : Colors.Transparent;

    public Color ShuffleButtonTextColor => IsShuffleEnabled ? Color.FromArgb("#F4A261") : Colors.White;

    public Color RepeatButtonBackgroundColor => _repeatMode == PlaybackRepeatMode.Off ? Colors.Transparent : Color.FromArgb("#244A56");

    public Color RepeatButtonTextColor => _repeatMode == PlaybackRepeatMode.Off ? Colors.White : Color.FromArgb("#F4A261");

    public string ElapsedTimeText => FormatTime(TimeSpan.FromSeconds(TrackPositionSeconds));

    public string DurationTimeText => FormatTime(TimeSpan.FromSeconds(TrackDurationSeconds));

    public IAsyncRelayCommand PickFolderCommand { get; }

    public IAsyncRelayCommand RefreshLibraryCommand { get; }

    public IAsyncRelayCommand TogglePlayPauseCommand { get; }

    public IAsyncRelayCommand<MediaTrackListItem?> SelectTrackCommand { get; }

    public IAsyncRelayCommand PlayPreviousTrackCommand { get; }

    public IAsyncRelayCommand PlayNextTrackCommand { get; }

    public IRelayCommand ToggleShuffleCommand { get; }

    public IRelayCommand CycleRepeatModeCommand { get; }

    // Carga la biblioteca y sincroniza el reproductor.
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        EnsurePlaybackTracking();

        IsBusy = true;
        try
        {
            var result = await _libraryManagementService.InitializeAsync(cancellationToken);
            ApplyLibraryResult(result);
            await RefreshPlaybackStateAsync(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Reproduce la pista seleccionada por el usuario.
    public async Task SelectTrackAsync(MediaTrackListItem? track, CancellationToken cancellationToken = default)
    {
        if (track is null || IsBusy)
        {
            return;
        }

        await PlayTrackInternalAsync(track, cancellationToken);
    }

    // Activa la vista previa del desplazamiento.
    public void BeginSeekPreview()
    {
        _isSeekPreviewActive = true;
    }

    // Actualiza la posicion mostrada durante el arrastre.
    public void PreviewSeek(double positionInSeconds)
    {
        if (!_isSeekPreviewActive)
        {
            return;
        }

        TrackPositionSeconds = positionInSeconds;
    }

    // Confirma el salto de reproduccion.
    public async Task CompleteSeekAsync(double positionInSeconds, CancellationToken cancellationToken = default)
    {
        _isSeekPreviewActive = false;

        if (!CanSeek)
        {
            return;
        }

        await _playbackController.SeekAsync(TimeSpan.FromSeconds(positionInSeconds), cancellationToken);
        await RefreshPlaybackStateAsync(cancellationToken);
    }

    // Abre el selector de carpetas y guarda la seleccion.
    private async Task PickFolderAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var folderPath = await _folderPickerService.PickFolderAsync();
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            var result = await _libraryManagementService.SelectLibraryFolderAsync(folderPath);
            ApplyLibraryResult(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Vuelve a escanear la biblioteca activa.
    private async Task RefreshLibraryAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _libraryManagementService.RefreshLibraryAsync();
            ApplyLibraryResult(result);
            await RefreshPlaybackStateAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Alterna entre reproducir y pausar.
    private async Task TogglePlayPauseAsync()
    {
        if (SelectedTrack is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var state = await _playbackController.GetCurrentPlaybackStateAsync();
            if (state.IsPlaying)
            {
                await _playbackController.PausePlaybackAsync();
            }
            else
            {
                await _playbackController.PlayTrackAsync(SelectedTrack.FilePath);
            }

            await RefreshPlaybackStateAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Salta a la pista anterior o reinicia la actual.
    private async Task PlayPreviousTrackAsync()
    {
        if (SelectedTrack is null || Tracks.Count == 0 || IsBusy)
        {
            return;
        }

        var snapshot = await _playbackController.GetCurrentPlaybackStateAsync();
        if (snapshot.Position > TimeSpan.FromSeconds(3))
        {
            await _playbackController.SeekAsync(TimeSpan.Zero);
            await RefreshPlaybackStateAsync();
            return;
        }

        var previousTrack = _playbackQueueService.GetPreviousTrack(Tracks, SelectedTrack);
        if (previousTrack is null)
        {
            return;
        }

        await PlayTrackInternalAsync(previousTrack);
    }

    // Avanza a la siguiente pista disponible.
    private async Task PlayNextTrackAsync()
    {
        var nextTrack = _playbackQueueService.GetNextTrack(Tracks, SelectedTrack, IsShuffleEnabled, _repeatMode, manualRequest: true);
        if (nextTrack is null)
        {
            return;
        }

        await PlayTrackInternalAsync(nextTrack);
    }

    // Activa o desactiva el modo aleatorio.
    private void ToggleShuffle()
    {
        IsShuffleEnabled = _playbackQueueService.ToggleShuffle(IsShuffleEnabled);
    }

    // Cambia entre los modos de repeticion.
    private void CycleRepeatMode()
    {
        _repeatMode = _playbackQueueService.CycleRepeatMode(_repeatMode);

        OnPropertyChanged(nameof(RepeatButtonText));
        OnPropertyChanged(nameof(RepeatButtonBackgroundColor));
        OnPropertyChanged(nameof(RepeatButtonTextColor));
        OnPropertyChanged(nameof(RepeatButtonIcon));
    }

    // Sincroniza el estado visual con el resultado de la biblioteca.
    private void ApplyLibraryResult(LibraryLoadResult result)
    {
        Tracks.Clear();
        foreach (var track in result.Tracks)
        {
            Tracks.Add(track);
        }

        HasLibraryFolder = result.HasLibraryFolder;
        CurrentFolderPath = result.LibraryFolderPath ?? string.Empty;

        if (!result.HasLibraryFolder)
        {
            EmptyStateTitle = "Tu biblioteca esta vacia";
            EmptyStateMessage = "Agrega una carpeta para que Melodix busque archivos .mp4, .m4a y .flac.";
        }
        else if (result.Tracks.Count == 0)
        {
            EmptyStateTitle = "No encontramos audio compatible";
            EmptyStateMessage = "La carpeta seleccionada no contiene archivos .mp4, .m4a o .flac. Puedes elegir otra carpeta o volver a escanear.";
        }
        else
        {
            EmptyStateTitle = string.Empty;
            EmptyStateMessage = string.Empty;
        }

        if (SelectedTrack is not null)
        {
            SelectedTrack = Tracks.FirstOrDefault(track => track.FilePath == SelectedTrack.FilePath);
        }

        if (SelectedTrack is null && result.Tracks.Count > 0)
        {
            CurrentTrackTitle = "Selecciona una pista";
            CurrentArtistName = string.Empty;
            CurrentArtworkPath = null;
            PlaybackStatusText = $"{result.Tracks.Count} pistas disponibles.";
        }
        else if (SelectedTrack is null)
        {
            CurrentTrackTitle = "Nada seleccionado";
            CurrentArtistName = string.Empty;
            CurrentArtworkPath = null;
            PlaybackStatusText = result.HasLibraryFolder
                ? "Aun no hay pistas disponibles en esta carpeta."
                : "Elige una carpeta para empezar.";
        }

        OnPropertyChanged(nameof(IsLibraryEmpty));
        OnPropertyChanged(nameof(HasTracks));
        OnPropertyChanged(nameof(CanNavigateTracks));
    }

    // Carga metadatos y arranca la reproduccion de una pista.
    private async Task PlayTrackInternalAsync(MediaTrackListItem track, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            SelectedTrack = track;
            await ApplyTrackPresentationMetadataAsync(track, cancellationToken);
            await _playbackController.PlayTrackAsync(track.FilePath, cancellationToken);
            await RefreshPlaybackStateAsync(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Obtiene titulo, artista y caratula para la pista actual.
    private async Task ApplyTrackPresentationMetadataAsync(MediaTrackListItem track, CancellationToken cancellationToken)
    {
        var metadata = await _trackMetadataService.GetMetadataAsync(track.FilePath, cancellationToken);
        CurrentTrackTitle = string.IsNullOrWhiteSpace(metadata.Title) ? track.FileName : metadata.Title;
        CurrentArtistName = metadata.Artist;
        CurrentArtworkPath = metadata.ArtworkPath;
    }

    // Arranca el timer y el evento que siguen la reproduccion.
    private void EnsurePlaybackTracking()
    {
        if (!_playbackEventsAttached)
        {
            _playbackController.PlaybackEnded += OnPlaybackEnded;
            _playbackEventsAttached = true;
        }

        if (_playbackTimer is not null)
        {
            return;
        }

        var dispatcher = Microsoft.Maui.Controls.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        _playbackTimer = dispatcher.CreateTimer();
        _playbackTimer.Interval = TimeSpan.FromMilliseconds(500);
        _playbackTimer.Tick += async (_, _) => await RefreshPlaybackStateAsync();
        _playbackTimer.Start();
    }

    // Reactiva la logica cuando termina una pista.
    private async void OnPlaybackEnded(object? sender, EventArgs e)
    {
        await HandlePlaybackEndedAsync();
    }

    // Decide que pista reproducir al terminar la actual.
    private async Task HandlePlaybackEndedAsync()
    {
        if (SelectedTrack is null)
        {
            return;
        }

        if (_repeatMode == PlaybackRepeatMode.One)
        {
            await PlayTrackInternalAsync(SelectedTrack);
            return;
        }

        var nextTrack = _playbackQueueService.GetNextTrack(Tracks, SelectedTrack, IsShuffleEnabled, _repeatMode, manualRequest: false);
        if (nextTrack is null)
        {
            IsPlaying = false;
            PlaybackStatusText = "Reproduccion finalizada.";
            TrackPositionSeconds = TrackDurationSeconds;
            return;
        }

        await PlayTrackInternalAsync(nextTrack);
    }

    // Sincroniza el estado del reproductor con la UI.
    private async Task RefreshPlaybackStateAsync(CancellationToken cancellationToken = default)
    {
        var state = await _playbackController.GetCurrentPlaybackStateAsync(cancellationToken);
        IsPlaying = state.IsPlaying;

        if (!_isSeekPreviewActive)
        {
            TrackPositionSeconds = state.Position.TotalSeconds;
        }

        TrackDurationSeconds = state.Duration.TotalSeconds;

        if (SelectedTrack is null)
        {
            return;
        }

        PlaybackStatusText = state.IsPlaying
            ? "Reproduciendo ahora."
            : "Lista para reproducir.";
    }

    // Convierte una duracion a texto corto.
    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return time.ToString(@"h\:mm\:ss");
        }

        return time.ToString(@"m\:ss");
    }
}
