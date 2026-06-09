using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Melodix.Presentation.Services;

public sealed record TrackPresentationMetadata(
    string Title,
    string Artist,
    string? ArtworkPath);

public interface ITrackMetadataService
{
    Task<TrackPresentationMetadata> GetMetadataAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed class WindowsTrackMetadataService : ITrackMetadataService
{
    private readonly string _artworkCacheDirectory;

    public WindowsTrackMetadataService()
    {
        _artworkCacheDirectory = Path.Combine(FileSystem.CacheDirectory, "artwork");
        Directory.CreateDirectory(_artworkCacheDirectory);
    }

    public async Task<TrackPresentationMetadata> GetMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return CreateFallback(filePath);
        }

        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
            var musicProperties = await storageFile.Properties.GetMusicPropertiesAsync();

            var title = string.IsNullOrWhiteSpace(musicProperties.Title)
                ? Path.GetFileNameWithoutExtension(filePath)
                : musicProperties.Title;
            var artist = FirstNonEmpty(musicProperties.Artist, musicProperties.AlbumArtist);
            var artworkPath = await TryExtractArtworkAsync(storageFile, filePath, cancellationToken);

            return new TrackPresentationMetadata(title, artist, artworkPath);
        }
        catch
        {
            return CreateFallback(filePath);
        }
    }

    private async Task<string?> TryExtractArtworkAsync(StorageFile storageFile, string filePath, CancellationToken cancellationToken)
    {
        var thumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.MusicView, 160, ThumbnailOptions.UseCurrentScale);
        if (thumbnail is null || thumbnail.Size == 0 || thumbnail.Type != ThumbnailType.Image)
        {
            return null;
        }

        await using var thumbnailStream = thumbnail.AsStreamForRead();
        if (thumbnailStream.Length == 0)
        {
            return null;
        }

        var cacheKey = BuildCacheKey(filePath);
        var cachePath = Path.Combine(_artworkCacheDirectory, $"{cacheKey}.jpg");
        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        await using var fileStream = File.Create(cachePath);
        await thumbnailStream.CopyToAsync(fileStream, cancellationToken);
        return cachePath;
    }

    private static string BuildCacheKey(string filePath)
    {
        var lastWrite = File.GetLastWriteTimeUtc(filePath).Ticks;
        var payload = Encoding.UTF8.GetBytes($"{filePath}|{lastWrite}");
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static TrackPresentationMetadata CreateFallback(string filePath)
    {
        var title = string.IsNullOrWhiteSpace(filePath)
            ? "Nada seleccionado"
            : Path.GetFileNameWithoutExtension(filePath);
        return new TrackPresentationMetadata(title, string.Empty, null);
    }
}
