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

    private async void OnExpandedPlaybackProgressDragCompleted(object? sender, EventArgs e)
    {
        _isSeeking = false;
        await _viewModel.CompleteSeekAsync(ExpandedPlaybackProgressSlider.Value);
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

    private async void OnMiniPlayerTapped(object? sender, TappedEventArgs e)
    {
        await ExpandPlayerAsync();
    }

    private async void OnMinimizePlayerClicked(object? sender, EventArgs e)
    {
        await CollapsePlayerAsync();
    }

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
