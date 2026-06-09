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

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        BindingContext = viewModel;
    }

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

    private void OnPlaybackProgressDragStarted(object? sender, EventArgs e)
    {
        _isSeeking = true;
        _viewModel.BeginSeekPreview();
    }

    private async void OnPlaybackProgressDragCompleted(object? sender, EventArgs e)
    {
        _isSeeking = false;
        await _viewModel.CompleteSeekAsync(PlaybackProgressSlider.Value);
    }

    private void OnPlaybackProgressValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isSeeking)
        {
            _viewModel.PreviewSeek(e.NewValue);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.SelectedTrack))
        {
            MainThread.BeginInvokeOnMainThread(SyncSelectionFromViewModel);
        }
    }

    private void SyncSelectionFromViewModel()
    {
        _isSyncingSelection = true;
        try
        {
            if (_viewModel.SelectedTrack is null)
            {
                TracksCollectionView.SelectedItem = null;
                return;
            }

            var matchingTrack = _viewModel.Tracks.FirstOrDefault(track =>
                string.Equals(track.FilePath, _viewModel.SelectedTrack.FilePath, StringComparison.OrdinalIgnoreCase));

            TracksCollectionView.SelectedItem = matchingTrack;
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }
}
