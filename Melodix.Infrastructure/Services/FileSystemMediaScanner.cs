using Melodix.Application.Abstractions;

namespace Melodix.Infrastructure.Services;

public sealed class FileSystemMediaScanner : IMediaScanner
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".flac",
        ".mp4",
        ".m4a"
    };

    // Busca archivos de audio compatibles dentro de la carpeta.
    public Task<IReadOnlyList<string>> ScanAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var files = new List<string>();
        foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SupportedExtensions.Contains(Path.GetExtension(filePath)))
            {
                files.Add(filePath);
            }
        }

        var orderedFiles = files
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyList<string>>(orderedFiles);
    }
}
