using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Melodix.Presentation.Models;

public sealed record LyricWordSegment(TimeSpan Timestamp, string Text);

// Representa una linea de letra con su tiempo y estado visual.
public class LyricLineViewItem : ObservableObject
{
    private static readonly Color ActiveWordColor = Colors.White;
    private static readonly Color PendingActiveLineColor = Color.FromArgb("#9CA3AF");
    private static readonly Color InactiveLineColor = Color.FromArgb("#6B7280");

    private readonly IReadOnlyList<LyricWordSegment> _wordSegments;

    // Guarda el tiempo y el texto de una linea de letra.
    public LyricLineViewItem(TimeSpan? timestamp, string text, IReadOnlyList<LyricWordSegment>? wordSegments = null)
    {
        Timestamp = timestamp;
        Text = text;
        _wordSegments = wordSegments ?? [];
        UpdateDisplay(TimeSpan.Zero, false);
    }

    public TimeSpan? Timestamp { get; }

    public string Text { get; }

    public bool HasWordTiming => _wordSegments.Count > 0;

    private FormattedString _displayText = new();

    public FormattedString DisplayText
    {
        get => _displayText;
        private set => SetProperty(ref _displayText, value);
    }

    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public void UpdateDisplay(TimeSpan currentPosition, bool isActive)
    {
        var formatted = new FormattedString();

        if (_wordSegments.Count == 0)
        {
            formatted.Spans.Add(new Span
            {
                Text = Text,
                TextColor = isActive ? ActiveWordColor : PendingActiveLineColor,
                FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None
            });

            DisplayText = formatted;
            return;
        }

        var currentWordIndex = -1;
        if (isActive)
        {
            for (var index = 0; index < _wordSegments.Count; index++)
            {
                if (_wordSegments[index].Timestamp > currentPosition)
                {
                    break;
                }

                currentWordIndex = index;
            }
        }

        for (var index = 0; index < _wordSegments.Count; index++)
        {
            var isCurrentWord = isActive && index == currentWordIndex;
            formatted.Spans.Add(new Span
            {
                Text = _wordSegments[index].Text,
                TextColor = isCurrentWord ? ActiveWordColor : (isActive ? PendingActiveLineColor : InactiveLineColor),
                FontAttributes = isCurrentWord ? FontAttributes.Bold : FontAttributes.None
            });
        }

        DisplayText = formatted;
    }
}
