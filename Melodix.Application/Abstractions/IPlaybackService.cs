using Melodix.Application.DTOs;

namespace Melodix.Application.Abstractions;

public interface IPlaybackService
{
    event EventHandler? PlaybackEnded;

    Task PlayAsync(string filePath, CancellationToken cancellationToken = default);

    Task PauseAsync(CancellationToken cancellationToken = default);

    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

    Task<PlaybackStateSnapshot> GetCurrentPlaybackStateAsync(CancellationToken cancellationToken = default);
}
