namespace Melodix.Application.DTOs;

public sealed record MediaTrackListItem(
    Guid Id,
    string FileName,
    string FilePath,
    string Extension);
