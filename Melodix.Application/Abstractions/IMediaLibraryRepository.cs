using Melodix.Domain.Entities;

namespace Melodix.Application.Abstractions;

public interface IMediaLibraryRepository
{
    Task<MusicFolder?> GetActiveFolderAsync(CancellationToken cancellationToken = default);

    Task<MusicFolder> UpsertActiveFolderAsync(string path, CancellationToken cancellationToken = default);

    Task ReplaceTracksForFolderAsync(
        Guid folderId,
        IReadOnlyCollection<MediaTrack> tracks,
        DateTimeOffset scannedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaTrack>> GetTracksForFolderAsync(Guid folderId, CancellationToken cancellationToken = default);

    Task UpdateTrackOrderAsync(
        Guid folderId,
        IReadOnlyList<(Guid TrackId, int SortOrder)> trackOrders,
        CancellationToken cancellationToken = default);

    Task UpdateTrackLyricsFilePathAsync(
        Guid trackId,
        string? lyricsFilePath,
        CancellationToken cancellationToken = default);
}
