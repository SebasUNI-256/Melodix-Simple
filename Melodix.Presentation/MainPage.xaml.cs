using System.ComponentModel;
using Melodix.Application.DTOs;
using Melodix.Presentation.ViewModels;

namespace Melodix.Presentation;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;
    private bool _hasLoaded;
    private bool _isSeeking;
    private bool _isSyncingSelection;
    private bool _isPlayerExpanded;
    private bool _isPlayerAnimating;
    private Guid? _draggedTrackId;

    // Conecta la pagina con su vista modelo.
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        BindingContext = viewModel;
    }

    // Carga la vista una sola vez al mostrarse.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await _viewModel.InitializeAsync();
        SyncSelectionFromViewModel();
    }

    // Notifica al modelo cuando cambia la pista seleccionada.
    private async void OnTrackSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection)
        {
            return;
        }

        if (e.CurrentSelection.FirstOrDefault() is MediaTrackListItem track)
        {
            if (string.Equals(track.FilePath, _viewModel.SelectedTrack?.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _viewModel.SelectTrackAsync(track);
        }
    }

    // Activa el modo de arrastre del progreso.
    private void OnPlaybackProgressDragStarted(object? sender, EventArgs e)
    {
        _isSeeking = true;
        _viewModel.BeginSeekPreview();
    }

    // Confirma el salto al soltar el control.
    private async void OnPlaybackProgressDragCompleted(object? sender, EventArgs e)
    {
        _isSeeking = false;
        await _viewModel.CompleteSeekAsync(PlaybackProgressSlider.Value);
    }

    // Confirma el salto desde el reproductor expandido.
    private async void OnExpandedPlaybackProgressDragCompleted(object? sender, EventArgs e)
    {
        _isSeeking = false;
        await _viewModel.CompleteSeekAsync(ExpandedPlaybackProgressSlider.Value);
    }

    // Actualiza la vista previa mientras se arrastra.
    private void OnPlaybackProgressValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isSeeking)
        {
            _viewModel.PreviewSeek(e.NewValue);
        }
    }

    // Sincroniza la seleccion visual con el modelo.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.SelectedTrack))
        {
            MainThread.BeginInvokeOnMainThread(SyncSelectionFromViewModel);
        }

        if (e.PropertyName == nameof(MainPageViewModel.CurrentLyricLineIndex))
        {
            MainThread.BeginInvokeOnMainThread(ScrollLyricsIntoView);
        }
    }

    // Abre el reproductor expandido.
    private async void OnMiniPlayerTapped(object? sender, TappedEventArgs e)
    {
        await ExpandPlayerAsync();
    }

    // Vuelve al reproductor compacto.
    private async void OnMinimizePlayerClicked(object? sender, EventArgs e)
    {
        await CollapsePlayerAsync();
    }

    // Anima la transicion al modo expandido.
    private async Task ExpandPlayerAsync()
    {
        if (_isPlayerExpanded || _isPlayerAnimating)
        {
            return;
        }

        _isPlayerAnimating = true;
        _isPlayerExpanded = true;

        ExpandedPlayerBorder.IsVisible = true;
        ExpandedPlayerBorder.Opacity = 0;
        ExpandedPlayerBorder.Scale = 0.985;
        ExpandedPlayerBorder.TranslationY = 48;

        await Task.WhenAll(
            MiniPlayerBorder.FadeToAsync(0, 180, Easing.CubicOut),
            MiniPlayerBorder.ScaleToAsync(0.985, 180, Easing.CubicOut),
            ExpandedPlayerBorder.FadeToAsync(1, 280, Easing.CubicOut),
            ExpandedPlayerBorder.ScaleToAsync(1, 280, Easing.CubicOut),
            ExpandedPlayerBorder.TranslateToAsync(0, 0, 280, Easing.CubicOut));

        MiniPlayerBorder.IsVisible = false;
        _isPlayerAnimating = false;
    }

    // Anima el regreso al modo compacto.
    private async Task CollapsePlayerAsync()
    {
        if (!_isPlayerExpanded || _isPlayerAnimating)
        {
            return;
        }

        _isPlayerAnimating = true;

        MiniPlayerBorder.IsVisible = true;
        MiniPlayerBorder.Opacity = 0;
        MiniPlayerBorder.Scale = 0.98;

        await Task.WhenAll(
            ExpandedPlayerBorder.FadeToAsync(0, 220, Easing.CubicIn),
            ExpandedPlayerBorder.ScaleToAsync(0.985, 220, Easing.CubicIn),
            ExpandedPlayerBorder.TranslateToAsync(0, 48, 220, Easing.CubicIn),
            MiniPlayerBorder.FadeToAsync(1, 240, Easing.CubicOut),
            MiniPlayerBorder.ScaleToAsync(1, 240, Easing.CubicOut));

        ExpandedPlayerBorder.IsVisible = false;
        _isPlayerExpanded = false;
        _isPlayerAnimating = false;
    }

    // Ajusta la seleccion de la lista al modelo actual.
    private void SyncSelectionFromViewModel()
    {
        _isSyncingSelection = true;
        try
        {
            if (_viewModel.SelectedTrack is null)
            {
                TracksCollectionView.SelectedItem = null;
                ExpandedQueueCollectionView.SelectedItem = null;
                return;
            }

            var matchingTrack = _viewModel.Tracks.FirstOrDefault(track =>
                string.Equals(track.FilePath, _viewModel.SelectedTrack.FilePath, StringComparison.OrdinalIgnoreCase));

            TracksCollectionView.SelectedItem = matchingTrack;
            ExpandedQueueCollectionView.SelectedItem = matchingTrack;
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private async void OnExpandedQueueSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection)
        {
            return;
        }

        if (e.CurrentSelection.FirstOrDefault() is MediaTrackListItem track)
        {
            if (string.Equals(track.FilePath, _viewModel.SelectedTrack?.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _viewModel.SelectTrackAsync(track);
        }
    }

    private async void OnExpandedQueueTrackTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is MediaTrackListItem track)
        {
            if (string.Equals(track.FilePath, _viewModel.SelectedTrack?.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _viewModel.SelectTrackAsync(track);
        }
    }

    private void OnQueueTrackDragStarting(object? sender, DragStartingEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not MediaTrackListItem track)
        {
            return;
        }

        _draggedTrackId = track.Id;
        _viewModel.SetDraggedTrack(track.Id);
        e.Data.Properties.Add("TrackId", track.Id.ToString());
    }

    private void OnQueueTrackDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;

        if ((sender as Element)?.BindingContext is MediaTrackListItem targetTrack && _draggedTrackId != targetTrack.Id)
        {
            _viewModel.SetDropTarget(targetTrack.Id);
        }
    }

    private void OnQueueTrackDragLeave(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private async void OnQueueTrackDrop(object? sender, DropEventArgs e)
    {
        try
        {
            if ((sender as Element)?.BindingContext is not MediaTrackListItem targetTrack)
            {
                return;
            }

            var sourceTrackId = _draggedTrackId;
            if (sourceTrackId is null && e.Data.Properties.TryGetValue("TrackId", out var trackIdValue) && Guid.TryParse(trackIdValue?.ToString(), out var droppedTrackId))
            {
                sourceTrackId = droppedTrackId;
            }

            if (sourceTrackId is null)
            {
                return;
            }

            await _viewModel.HandleTrackDropAsync(sourceTrackId.Value, targetTrack.Id);
            e.Handled = true;
            SyncSelectionFromViewModel();
        }
        finally
        {
            _draggedTrackId = null;
            _viewModel.SetDraggedTrack(null);
            _viewModel.SetDropTarget(null);
        }
    }

    private void ScrollLyricsIntoView()
    {
        var index = _viewModel.CurrentLyricLineIndex;
        if (index < 0 || index >= _viewModel.LyricsLines.Count)
        {
            return;
        }

        LyricsCollectionView.ScrollTo(index, position: ScrollToPosition.Center, animate: false);
    }
}
