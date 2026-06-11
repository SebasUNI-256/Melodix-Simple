using Melodix.Application.DTOs;

namespace Melodix.Application.Services;

public enum PlaybackRepeatMode
{
    Off,
    All,
    One
}

public sealed class PlaybackQueueService
{
    private readonly Random _random = new();

    // Alterna el estado del modo aleatorio.
    public bool ToggleShuffle(bool isShuffleEnabled)
    {
        return !isShuffleEnabled;
    }

    // Cambia entre los modos de repeticion disponibles.
    public PlaybackRepeatMode CycleRepeatMode(PlaybackRepeatMode currentMode)
    {
        return currentMode switch
        {
            PlaybackRepeatMode.Off => PlaybackRepeatMode.All,
            PlaybackRepeatMode.All => PlaybackRepeatMode.One,
            _ => PlaybackRepeatMode.Off
        };
    }

    // Calcula la pista anterior en una lista ordenada.
    public MediaTrackListItem? GetPreviousTrack(IReadOnlyList<MediaTrackListItem> tracks, MediaTrackListItem? selectedTrack)
    {
        if (tracks.Count == 0)
        {
            return null;
        }

        if (selectedTrack is null)
        {
            return tracks[0];
        }

        var currentIndex = FindTrackIndex(tracks, selectedTrack);
        if (currentIndex <= 0)
        {
            return tracks[0];
        }

        return tracks[currentIndex - 1];
    }

    // Calcula la siguiente pista segun shuffle y repeticion.
    public MediaTrackListItem? GetNextTrack(
        IReadOnlyList<MediaTrackListItem> tracks,
        MediaTrackListItem? selectedTrack,
        bool isShuffleEnabled,
        PlaybackRepeatMode repeatMode,
        bool manualRequest)
    {
        if (tracks.Count == 0)
        {
            return null;
        }

        if (selectedTrack is null)
        {
            return tracks[0];
        }

        if (isShuffleEnabled && tracks.Count > 1)
        {
            var candidates = tracks.Where(track => track.FilePath != selectedTrack.FilePath).ToArray();
            return candidates[_random.Next(candidates.Length)];
        }

        var currentIndex = FindTrackIndex(tracks, selectedTrack);
        if (currentIndex < 0)
        {
            return tracks[0];
        }

        var nextIndex = currentIndex + 1;
        if (nextIndex < tracks.Count)
        {
            return tracks[nextIndex];
        }

        if (!manualRequest && repeatMode == PlaybackRepeatMode.All)
        {
            return tracks[0];
        }

        return null;
    }

    // Busca el indice de una pista por su ruta.
    private static int FindTrackIndex(IReadOnlyList<MediaTrackListItem> tracks, MediaTrackListItem selectedTrack)
    {
        for (var index = 0; index < tracks.Count; index++)
        {
            if (string.Equals(tracks[index].FilePath, selectedTrack.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}
