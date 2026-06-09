using Melodix.Application.Abstractions;
using Melodix.Application.Services;
using Melodix.Domain.Entities;

namespace Melodix.Tests;

public sealed class LibrarySynchronizationServiceTests
{
    [Fact]
    public async Task SynchronizeAsync_ReplacesRemovedTracksWithCurrentScan()
    {
        var repository = new InMemoryMediaLibraryRepository(@"C:\music");
        var scanner = new SequenceMediaScanner(
            [@"C:\music\first.m4a", @"C:\music\second.mp4", @"C:\music\third.flac"],
            [@"C:\music\second.mp4", @"C:\music\third.flac"]);
        var service = new LibrarySynchronizationService(repository, scanner);

        var firstSync = await service.SynchronizeAsync();
        var secondSync = await service.SynchronizeAsync();

        Assert.Equal(3, firstSync.Tracks.Count);
        Assert.Equal(2, secondSync.Tracks.Count);
        Assert.Contains(secondSync.Tracks, track => track.Extension == ".flac");
        Assert.DoesNotContain(secondSync.Tracks, track => track.FileName == "first");
    }

    private sealed class SequenceMediaScanner : IMediaScanner
    {
        private readonly Queue<IReadOnlyList<string>> _scanResults;

        public SequenceMediaScanner(params IReadOnlyList<string>[] scanResults)
        {
            _scanResults = new Queue<IReadOnlyList<string>>(scanResults);
        }

        public Task<IReadOnlyList<string>> ScanAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            var result = _scanResults.Count > 1 ? _scanResults.Dequeue() : _scanResults.Peek();
            return Task.FromResult(result);
        }
    }

    private sealed class InMemoryMediaLibraryRepository : IMediaLibraryRepository
    {
        private readonly MusicFolder _folder;
        private List<MediaTrack> _tracks = [];

        public InMemoryMediaLibraryRepository(string path)
        {
            _folder = new MusicFolder
            {
                Path = path,
                IsActive = true
            };
        }

        public Task<MusicFolder?> GetActiveFolderAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<MusicFolder?>(_folder);

        public Task<MusicFolder> UpsertActiveFolderAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult(_folder);

        public Task ReplaceTracksForFolderAsync(
            Guid folderId,
            IReadOnlyCollection<MediaTrack> tracks,
            DateTimeOffset scannedAt,
            CancellationToken cancellationToken = default)
        {
            _tracks = tracks.Select(track => new MediaTrack
            {
                Id = track.Id,
                FileName = track.FileName,
                FilePath = track.FilePath,
                Extension = track.Extension,
                FolderId = folderId,
                DiscoveredAt = scannedAt
            }).ToList();

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MediaTrack>> GetTracksForFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MediaTrack>>(_tracks.ToArray());
    }
}
