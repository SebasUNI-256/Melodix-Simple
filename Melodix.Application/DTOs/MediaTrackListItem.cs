using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Melodix.Application.DTOs;

// Representa una pista mostrada en la biblioteca y en la cola.
public sealed class MediaTrackListItem : INotifyPropertyChanged
{
    // Crea el item con sus datos base, orden y letra asociada.
    public MediaTrackListItem(
        Guid id,
        string fileName,
        string filePath,
        string extension,
        int sortOrder,
        string? lyricsFilePath)
    {
        Id = id;
        FileName = fileName;
        FilePath = filePath;
        Extension = extension;
        SortOrder = sortOrder;
        LyricsFilePath = lyricsFilePath;
    }

    public Guid Id { get; }

    public string FileName { get; }

    public string FilePath { get; }

    public string Extension { get; }

    public int SortOrder { get; set; }

    public string? LyricsFilePath { get; set; }

    private bool _isDropTarget;

    public bool IsDropTarget
    {
        get => _isDropTarget;
        set => SetProperty(ref _isDropTarget, value);
    }

    private bool _isBeingDragged;

    public bool IsBeingDragged
    {
        get => _isBeingDragged;
        set => SetProperty(ref _isBeingDragged, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
