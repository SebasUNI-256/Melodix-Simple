namespace Melodix.Domain.Entities;

// Guarda la informacion persistida de cada archivo de audio.
public sealed class MediaTrack
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public Guid FolderId { get; set; }

    public MusicFolder? Folder { get; set; }

    public int SortOrder { get; set; }

    public string? LyricsFilePath { get; set; }

    public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
}
