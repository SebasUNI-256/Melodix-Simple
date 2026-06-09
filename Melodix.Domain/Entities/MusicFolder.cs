namespace Melodix.Domain.Entities;

public sealed class MusicFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Path { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastScanAt { get; set; }

    public List<MediaTrack> Tracks { get; set; } = [];
}
