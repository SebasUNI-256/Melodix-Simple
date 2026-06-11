using Melodix.Application.Abstractions;
using Melodix.Application.DTOs;

namespace Melodix.Application.Services;

public sealed class PlaybackController
{
    private readonly IPlaybackService _playbackService;

    // Expone el servicio de reproduccion a la capa de aplicacion.
    public PlaybackController(IPlaybackService playbackService)
    {
        _playbackService = playbackService;
    }

    // Reexpone el evento de fin de reproduccion.
    public event EventHandler? PlaybackEnded
    {
        add => _playbackService.PlaybackEnded += value;
        remove => _playbackService.PlaybackEnded -= value;
    }

    // Reproduce una pista concreta.
    public Task PlayTrackAsync(string filePath, CancellationToken cancellationToken = default)
        => _playbackService.PlayAsync(filePath, cancellationToken);

    // Pausa la reproduccion actual.
    public Task PausePlaybackAsync(CancellationToken cancellationToken = default)
        => _playbackService.PauseAsync(cancellationToken);

    // Mueve la reproduccion a una posicion.
    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        => _playbackService.SeekAsync(position, cancellationToken);

    // Consulta el estado actual del reproductor.
    public Task<PlaybackStateSnapshot> GetCurrentPlaybackStateAsync(CancellationToken cancellationToken = default)
        => _playbackService.GetCurrentPlaybackStateAsync(cancellationToken);
}
