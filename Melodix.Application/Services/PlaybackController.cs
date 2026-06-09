using Melodix.Application.Abstractions;
using Melodix.Application.DTOs;

namespace Melodix.Application.Services;

public sealed class PlaybackController
{
    private readonly IPlaybackService _playbackService;

    public PlaybackController(IPlaybackService playbackService)
    {
        _playbackService = playbackService;
    }

    public event EventHandler? PlaybackEnded
    {
        add => _playbackService.PlaybackEnded += value;
        remove => _playbackService.PlaybackEnded -= value;
    }

    public Task PlayTrackAsync(string filePath, CancellationToken cancellationToken = default)
        => _playbackService.PlayAsync(filePath, cancellationToken);

    public Task PausePlaybackAsync(CancellationToken cancellationToken = default)
        => _playbackService.PauseAsync(cancellationToken);

    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        => _playbackService.SeekAsync(position, cancellationToken);

    public Task<PlaybackStateSnapshot> GetCurrentPlaybackStateAsync(CancellationToken cancellationToken = default)
        => _playbackService.GetCurrentPlaybackStateAsync(cancellationToken);
}
