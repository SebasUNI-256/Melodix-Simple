using Melodix.Infrastructure.Services;

namespace Melodix.Tests;

public sealed class FileSystemMediaScannerTests
{
    [Fact]
    // Verifica que solo salgan archivos compatibles.
    public async Task ScanAsync_ReturnsOnlySupportedFilesRecursively()
    {
        var root = CreateTempDirectory();
        var nested = Directory.CreateDirectory(Path.Combine(root.FullName, "nested"));

        File.WriteAllText(Path.Combine(root.FullName, "track-a.mp4"), "a");
        File.WriteAllText(Path.Combine(nested.FullName, "track-b.m4a"), "b");
        File.WriteAllText(Path.Combine(nested.FullName, "track-c.flac"), "c");
        File.WriteAllText(Path.Combine(nested.FullName, "ignore.txt"), "d");

        var scanner = new FileSystemMediaScanner();

        var result = await scanner.ScanAsync(root.FullName);

        Assert.Equal(3, result.Count);
        Assert.All(result, path => Assert.Contains(Path.GetExtension(path), new[] { ".mp4", ".m4a", ".flac" }));
        Assert.Contains(result, path => path.EndsWith("track-a.mp4", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, path => path.EndsWith("track-b.m4a", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, path => path.EndsWith("track-c.flac", StringComparison.OrdinalIgnoreCase));
    }

    // Crea una carpeta temporal para la prueba.
    private static DirectoryInfo CreateTempDirectory()
    {
        return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "MelodixTests", Guid.NewGuid().ToString("N")));
    }
}
