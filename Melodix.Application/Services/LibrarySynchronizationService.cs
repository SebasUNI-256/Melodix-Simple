using Melodix.Application.Abstractions;
using Melodix.Application.DTOs;
using Melodix.Domain.Entities;

namespace Melodix.Application.Services;

// Sincroniza la carpeta activa con las pistas detectadas en disco.
public sealed class LibrarySynchronizationService : ILibrarySynchronizationService
{
    private readonly IMediaLibraryRepository _mediaLibraryRepository;
    private readonly IMediaScanner _mediaScanner;

    // Conecta el sincronizador con el repositorio y el escaner.
    public LibrarySynchronizationService(
        IMediaLibraryRepository mediaLibraryRepository,
        IMediaScanner mediaScanner)
    {
        _mediaLibraryRepository = mediaLibraryRepository;
        _mediaScanner = mediaScanner;
    }

    // Reescanea la carpeta activa y actualiza las pistas guardadas.
    public async Task<LibraryLoadResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var activeFolder = await _mediaLibraryRepository.GetActiveFolderAsync(cancellationToken);
        if (activeFolder is null)
        {
            return LibraryLoadResult.Empty;
        }

        var discoveredFiles = await _mediaScanner.ScanAsync(activeFolder.Path, cancellationToken);
        var scannedAt = DateTimeOffset.UtcNow;
        var discoveredTracks = discoveredFiles
            .Select(filePath => new MediaTrack
            {
                FilePath = filePath,
                FileName = Path.GetFileNameWithoutExtension(filePath),
                Extension = Path.GetExtension(filePath).ToLowerInvariant(),
                FolderId = activeFolder.Id,
                DiscoveredAt = scannedAt
            })
            .ToArray();

        await _mediaLibraryRepository.ReplaceTracksForFolderAsync(activeFolder.Id, discoveredTracks, scannedAt, cancellationToken);

        var storedTracks = await _mediaLibraryRepository.GetTracksForFolderAsync(activeFolder.Id, cancellationToken);
        var items = storedTracks
            .OrderBy(track => track.SortOrder)
            .ThenBy(track => track.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(track => new MediaTrackListItem(track.Id, track.FileName, track.FilePath, track.Extension, track.SortOrder, track.LyricsFilePath))
            .ToArray();

        return new LibraryLoadResult(true, activeFolder.Path, items);
    }
}
