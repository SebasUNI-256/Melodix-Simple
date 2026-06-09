namespace Melodix.Application.DTOs;

public sealed record LibraryLoadResult(
    bool HasLibraryFolder,
    string? LibraryFolderPath,
    IReadOnlyList<MediaTrackListItem> Tracks)
{
    public static LibraryLoadResult Empty { get; } = new(false, null, Array.Empty<MediaTrackListItem>());
}
