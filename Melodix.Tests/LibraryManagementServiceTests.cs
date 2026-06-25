using Melodix.Application.Abstractions;
using Melodix.Application.Services;
using Melodix.Domain.Entities;

namespace Melodix.Tests;

public sealed class LibraryManagementServiceTests
{
    [Fact]
    // Verifica que iniciar sin carpeta devuelve vacio.
    public async Task InitializeAsync_ReturnsEmptyResult_WhenNoFolderWasSelected()
    {
        var repository = new InMemoryMediaLibraryRepository();
        var synchronizationService = new LibrarySynchronizationService(repository, new FakeMediaScanner());
        var service = new LibraryManagementService(repository, synchronizationService);

        var result = await service.InitializeAsync();

        Assert.False(result.HasLibraryFolder);
        Assert.Empty(result.Tracks);
        Assert.Null(result.LibraryFolderPath);
    }

    [Fact]
    // Verifica que elegir carpeta guarda y carga pistas.
    public async Task SelectLibraryFolderAsync_PersistsAndReturnsTracks()
    {
        var repository = new InMemoryMediaLibraryRepository();
        var scanner = new FakeMediaScanner(
            @"C:\music\alpha.m4a",
            @"C:\music\beta.mp4",
            @"C:\music\gamma.flac");
        var synchronizationService = new LibrarySynchronizationService(repository, scanner);
        var service = new LibraryManagementService(repository, synchronizationService);

        var result = await service.SelectLibraryFolderAsync(@"C:\music");

        Assert.True(result.HasLibraryFolder);
        Assert.Equal(@"C:\music", result.LibraryFolderPath);
        Assert.Equal(3, result.Tracks.Count);
        Assert.Equal(3, (await service.GetLibraryTracksAsync()).Count);
        Assert.Contains(result.Tracks, track => track.Extension == ".flac");
    }

    private sealed class FakeMediaScanner : IMediaScanner
    {
        private readonly IReadOnlyList<string> _files;

        // Recibe la lista fija de archivos a devolver.
        public FakeMediaScanner(params string[] files)
        {
            _files = files;
        }

        // Devuelve siempre los archivos configurados.
        public Task<IReadOnlyList<string>> ScanAsync(string folderPath, CancellationToken cancellationToken = default)
            => Task.FromResult(_files);
    }

    private sealed class InMemoryMediaLibraryRepository : IMediaLibraryRepository
    {
        private MusicFolder? _activeFolder;
        private readonly List<MediaTrack> _tracks = [];

        // Devuelve la carpeta activa en memoria.
        public Task<MusicFolder?> GetActiveFolderAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_activeFolder);

        // Guarda la carpeta activa en memoria.
        public Task<MusicFolder> UpsertActiveFolderAsync(string path, CancellationToken cancellationToken = default)
        {
            _activeFolder = new MusicFolder
            {
                Id = _activeFolder?.Id ?? Guid.NewGuid(),
                Path = path,
                IsActive = true,
                CreatedAt = _activeFolder?.CreatedAt ?? DateTimeOffset.UtcNow
            };

            return Task.FromResult(_activeFolder);
        }

        // Reemplaza las pistas en memoria para la carpeta.
        public Task ReplaceTracksForFolderAsync(
            Guid folderId,
            IReadOnlyCollection<MediaTrack> tracks,
            DateTimeOffset scannedAt,
            CancellationToken cancellationToken = default)
        {
            _tracks.Clear();
            _tracks.AddRange(tracks.Select(track => new MediaTrack
            {
                Id = track.Id,
                FileName = track.FileName,
                FilePath = track.FilePath,
                Extension = track.Extension,
                FolderId = folderId,
                SortOrder = track.SortOrder,
                LyricsFilePath = track.LyricsFilePath,
                DiscoveredAt = scannedAt
            }));

            if (_activeFolder is not null)
            {
                _activeFolder.LastScanAt = scannedAt;
            }

            return Task.CompletedTask;
        }

        // Devuelve las pistas guardadas para la carpeta.
        public Task<IReadOnlyList<MediaTrack>> GetTracksForFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MediaTrack>>(_tracks.Where(track => track.FolderId == folderId).ToArray());

        public Task UpdateTrackOrderAsync(Guid folderId, IReadOnlyList<(Guid TrackId, int SortOrder)> trackOrders, CancellationToken cancellationToken = default)
        {
            var orders = trackOrders.ToDictionary(item => item.TrackId, item => item.SortOrder);
            foreach (var track in _tracks.Where(item => item.FolderId == folderId))
            {
                if (orders.TryGetValue(track.Id, out var sortOrder))
                {
                    track.SortOrder = sortOrder;
                }
            }

            return Task.CompletedTask;
        }

        public Task UpdateTrackLyricsFilePathAsync(Guid trackId, string? lyricsFilePath, CancellationToken cancellationToken = default)
        {
            var track = _tracks.FirstOrDefault(item => item.Id == trackId);
            if (track is not null)
            {
                track.LyricsFilePath = lyricsFilePath;
            }

            return Task.CompletedTask;
        }
    }
}
