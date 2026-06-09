namespace Melodix.Application.DTOs;

public sealed record PlaybackStateSnapshot(
    string? CurrentFilePath,
    bool IsPlaying,
    TimeSpan Position,
    TimeSpan Duration);
