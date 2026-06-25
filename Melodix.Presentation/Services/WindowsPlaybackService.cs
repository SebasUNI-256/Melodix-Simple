using Melodix.Application.Abstractions;
using Melodix.Application.DTOs;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Melodix.Presentation.Services;

public sealed class WindowsPlaybackService : IPlaybackService, IDisposable
{
    private readonly MediaPlayer _mediaPlayer = new();
    private string? _currentFilePath;

    // Prepara el reproductor nativo y sus eventos.
    public WindowsPlaybackService()
    {
        _mediaPlayer.MediaEnded += OnMediaEnded;
    }

    public event EventHandler? PlaybackEnded;

    // Inicia la reproduccion de un archivo.
    public Task PlayAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Es necesario un archivo valido.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("El archivo multimedia no pudo ser encontrado.", filePath);
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

    // Pausa la reproduccion activa.
    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _mediaPlayer.Pause();
        return Task.CompletedTask;
    }

    // Mueve el punto de reproduccion.
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

    // Devuelve el estado actual del reproductor.
    public Task<PlaybackStateSnapshot> GetCurrentPlaybackStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = _mediaPlayer.PlaybackSession;
        var isPlaying = session.PlaybackState == MediaPlaybackState.Playing;
        var duration = session.NaturalDuration > TimeSpan.Zero ? session.NaturalDuration : TimeSpan.Zero;
        var position = session.Position < TimeSpan.Zero ? TimeSpan.Zero : session.Position;

        return Task.FromResult(new PlaybackStateSnapshot(_currentFilePath, isPlaying, position, duration));
    }

    // Libera los recursos del reproductor.
    public void Dispose()
    {
        _mediaPlayer.MediaEnded -= OnMediaEnded;
        _mediaPlayer.Dispose();
    }

    // Dispara el evento cuando termina una pista.
    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
}
