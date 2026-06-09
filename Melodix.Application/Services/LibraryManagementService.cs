using Melodix.Application.Abstractions;
using Melodix.Application.DTOs;

namespace Melodix.Application.Services;

public sealed class LibraryManagementService
{
    private readonly IMediaLibraryRepository _mediaLibraryRepository;
    private readonly ILibrarySynchronizationService _librarySynchronizationService;

    public LibraryManagementService(
        IMediaLibraryRepository mediaLibraryRepository,
        ILibrarySynchronizationService librarySynchronizationService)
    {
        _mediaLibraryRepository = mediaLibraryRepository;
        _librarySynchronizationService = librarySynchronizationService;
    }

    public Task<LibraryLoadResult> InitializeAsync(CancellationToken cancellationToken = default)
        => _librarySynchronizationService.SynchronizeAsync(cancellationToken);

    public async Task<LibraryLoadResult> SelectLibraryFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return await GetCurrentLibraryAsync(cancellationToken);
        }

        await _mediaLibraryRepository.UpsertActiveFolderAsync(folderPath.Trim(), cancellationToken);
        return await _librarySynchronizationService.SynchronizeAsync(cancellationToken);
    }

    public Task<LibraryLoadResult> RefreshLibraryAsync(CancellationToken cancellationToken = default)
        => _librarySynchronizationService.SynchronizeAsync(cancellationToken);

    public async Task<IReadOnlyList<MediaTrackListItem>> GetLibraryTracksAsync(CancellationToken cancellationToken = default)
    {
        var activeFolder = await _mediaLibraryRepository.GetActiveFolderAsync(cancellationToken);
        if (activeFolder is null)
        {
            return Array.Empty<MediaTrackListItem>();
        }

        var tracks = await _mediaLibraryRepository.GetTracksForFolderAsync(activeFolder.Id, cancellationToken);
        return tracks
            .OrderBy(track => track.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(track => new MediaTrackListItem(track.Id, track.FileName, track.FilePath, track.Extension))
            .ToArray();
    }

    public async Task<LibraryLoadResult> GetCurrentLibraryAsync(CancellationToken cancellationToken = default)
    {
        var activeFolder = await _mediaLibraryRepository.GetActiveFolderAsync(cancellationToken);
        if (activeFolder is null)
        {
            return LibraryLoadResult.Empty;
        }

        var tracks = await GetLibraryTracksAsync(cancellationToken);
        return new LibraryLoadResult(true, activeFolder.Path, tracks);
    }
}
