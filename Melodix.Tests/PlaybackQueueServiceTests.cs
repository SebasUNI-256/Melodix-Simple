using Melodix.Application.DTOs;
using Melodix.Application.Services;

namespace Melodix.Tests;

public sealed class PlaybackQueueServiceTests
{
    // Verifica que el modo repeticion avance en ciclo.
    [Fact]
    public void CycleRepeatMode_RotatesThroughAvailableModes()
    {
        var service = new PlaybackQueueService();

        Assert.Equal(PlaybackRepeatMode.All, service.CycleRepeatMode(PlaybackRepeatMode.Off));
        Assert.Equal(PlaybackRepeatMode.One, service.CycleRepeatMode(PlaybackRepeatMode.All));
        Assert.Equal(PlaybackRepeatMode.Off, service.CycleRepeatMode(PlaybackRepeatMode.One));
    }

    // Verifica que repetir todo vuelva a la primera pista al terminar.
    [Fact]
    public void GetNextTrack_ReturnsFirstTrack_WhenRepeatAllAndPlaybackEnds()
    {
        var service = new PlaybackQueueService();
        var tracks = CreateTracks();

        var nextTrack = service.GetNextTrack(tracks, tracks[2], isShuffleEnabled: false, PlaybackRepeatMode.All, manualRequest: false);

        Assert.Equal(tracks[0], nextTrack);
    }

    // Verifica que la pista anterior se resuelva por indice.
    [Fact]
    public void GetPreviousTrack_ReturnsPreviousTrack_WhenCurrentTrackExists()
    {
        var service = new PlaybackQueueService();
        var tracks = CreateTracks();

        var previousTrack = service.GetPreviousTrack(tracks, tracks[1]);

        Assert.Equal(tracks[0], previousTrack);
    }

    // Crea una lista simple de pistas para probar la cola.
    private static IReadOnlyList<MediaTrackListItem> CreateTracks()
    {
        return
        [
            new MediaTrackListItem(Guid.NewGuid(), "Alpha", @"C:\music\alpha.m4a", ".m4a"),
            new MediaTrackListItem(Guid.NewGuid(), "Beta", @"C:\music\beta.mp4", ".mp4"),
            new MediaTrackListItem(Guid.NewGuid(), "Gamma", @"C:\music\gamma.flac", ".flac")
        ];
    }
}
