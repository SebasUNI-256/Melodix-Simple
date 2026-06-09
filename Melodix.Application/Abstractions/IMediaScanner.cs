namespace Melodix.Application.Abstractions;

public interface IMediaScanner
{
    Task<IReadOnlyList<string>> ScanAsync(string folderPath, CancellationToken cancellationToken = default);
}
