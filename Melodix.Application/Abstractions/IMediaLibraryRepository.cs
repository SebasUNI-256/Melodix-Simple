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
}
