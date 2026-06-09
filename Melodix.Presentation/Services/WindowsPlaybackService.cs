using Melodix.Application.Abstractions;
using Melodix.Application.DTOs;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Melodix.Presentation.Services;

public sealed class WindowsPlaybackService : IPlaybackService, IDisposable
{
    private readonly MediaPlayer _mediaPlayer = new();
    private string? _currentFilePath;

    public WindowsPlaybackService()
    {
        _mediaPlayer.MediaEnded += OnMediaEnded;
    }

    public event EventHandler? PlaybackEnded;

    public Task PlayAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A valid file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The selected media file could not be found.", filePath);
        }

        if (!string.Equals(_currentFilePath, filePath, StringComparison.OrdinalIgnoreCase))
        {
            _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(filePath));
            _currentFilePath = filePath;
        }
        else if (_mediaPlayer.PlaybackSession.NaturalDuration > TimeSpan.Zero &&
                 _mediaPlayer.PlaybackSession.Position >= _mediaPlayer.PlaybackSession.NaturalDuration - TimeSpan.FromMilliseconds(300))
        {
            _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }

        _mediaPlayer.Play();
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _mediaPlayer.Pause();
        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var duration = _mediaPlayer.PlaybackSession.NaturalDuration;
        var target = position < TimeSpan.Zero ? TimeSpan.Zero : position;
        if (duration > TimeSpan.Zero && target > duration)
        {
            target = duration;
        }

        _mediaPlayer.PlaybackSession.Position = target;
        return Task.CompletedTask;
    }

    public Task<PlaybackStateSnapshot> GetCurrentPlaybackStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = _mediaPlayer.PlaybackSession;
        var isPlaying = session.PlaybackState == MediaPlaybackState.Playing;
        var duration = session.NaturalDuration > TimeSpan.Zero ? session.NaturalDuration : TimeSpan.Zero;
        var position = session.Position < TimeSpan.Zero ? TimeSpan.Zero : session.Position;

        return Task.FromResult(new PlaybackStateSnapshot(_currentFilePath, isPlaying, position, duration));
    }

    public void Dispose()
    {
        _mediaPlayer.MediaEnded -= OnMediaEnded;
        _mediaPlayer.Dispose();
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
}
