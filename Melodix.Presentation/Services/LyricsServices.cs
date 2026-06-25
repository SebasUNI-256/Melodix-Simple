using System.Text;
using System.Text.RegularExpressions;
using Melodix.Presentation.Models;
using Microsoft.Maui.Platform;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Melodix.Presentation.Services;

// Contiene el archivo de letra y sus lineas ya interpretadas.
public sealed record LyricsDocument(
    string FilePath,
    bool IsSynchronized,
    IReadOnlyList<LyricLineViewItem> Lines);

// Abre el selector nativo para elegir un archivo de letra.
public interface ILyricsFilePickerService
{
    Task<string?> PickLyricsFileAsync(CancellationToken cancellationToken = default);
}

// Lee y parsea una letra desde disco.
public interface ILyricsLoaderService
{
    Task<LyricsDocument?> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}

// Usa el picker nativo de Windows para elegir el archivo de letra.
public sealed class LyricsFilePickerService : ILyricsFilePickerService
{
    // Abre el selector de archivo y devuelve la ruta elegida.
    public async Task<string?> PickLyricsFileAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
        if (currentWindow is null)
        {
            return null;
        }

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".lrc");
        picker.FileTypeFilter.Add(".txt");
        picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(currentWindow));

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}

// Convierte archivos .lrc o .txt en una estructura de letra para la vista.
public sealed class LyricsLoaderService : ILyricsLoaderService
{
    private static readonly Regex TimestampRegex = new(@"\[(?<min>\d{1,2}):(?<sec>\d{2})(?:\.(?<frac>\d{1,3}))?\]", RegexOptions.Compiled);
    private static readonly Regex InlineWordTimestampRegex = new(@"<(?<min>\d{1,2}):(?<sec>\d{2})(?:\.(?<frac>\d{1,3}))?>", RegexOptions.Compiled);

    // Carga el archivo y decide si tiene sincronizacion por tiempo.
    public async Task<LyricsDocument?> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        var extension = Path.GetExtension(filePath);
        if (string.Equals(extension, ".lrc", StringComparison.OrdinalIgnoreCase))
        {
            return await LoadLrcAsync(filePath, cancellationToken);
        }

        return await LoadPlainTextAsync(filePath, cancellationToken);
    }

    // Lee texto plano sin timestamps.
    private static async Task<LyricsDocument> LoadPlainTextAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);
        var items = lines.Length == 0
            ? [new LyricLineViewItem(null, "Sin contenido.")]
            : lines.Select(line => new LyricLineViewItem(null, string.IsNullOrWhiteSpace(line) ? " " : line)).ToArray();

        return new LyricsDocument(filePath, false, items);
    }

    // Lee un .lrc y separa cada linea sincronizada por timestamp.
    private static async Task<LyricsDocument> LoadLrcAsync(string filePath, CancellationToken cancellationToken)
    {
        var rawLines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);
        var items = new List<LyricLineViewItem>();

        foreach (var rawLine in rawLines)
        {
            var matches = TimestampRegex.Matches(rawLine);
            if (matches.Count == 0)
            {
                continue;
            }

            var lineContent = TimestampRegex.Replace(rawLine, string.Empty);
            var wordSegments = ParseWordSegments(lineContent);
            var text = InlineWordTimestampRegex.Replace(lineContent, string.Empty).Trim();
            foreach (Match match in matches)
            {
                items.Add(new LyricLineViewItem(
                    ParseTimestamp(match),
                    string.IsNullOrWhiteSpace(text) ? " " : text,
                    wordSegments));
            }
        }

        if (items.Count == 0)
        {
            return await LoadPlainTextAsync(filePath, cancellationToken);
        }

        return new LyricsDocument(
            filePath,
            true,
            items.OrderBy(item => item.Timestamp).ToArray());
    }

    // Convierte un timestamp de LRC en TimeSpan.
    private static TimeSpan ParseTimestamp(Match match)
    {
        var minutes = int.Parse(match.Groups["min"].Value);
        var seconds = int.Parse(match.Groups["sec"].Value);
        var fractionGroup = match.Groups["frac"].Value;
        var milliseconds = 0;

        if (!string.IsNullOrWhiteSpace(fractionGroup))
        {
            milliseconds = fractionGroup.Length switch
            {
                1 => int.Parse(fractionGroup) * 100,
                2 => int.Parse(fractionGroup) * 10,
                _ => int.Parse(fractionGroup[..3])
            };
        }

        return new TimeSpan(0, 0, minutes, seconds, milliseconds);
    }

    // Extrae timestamps por palabra desde un Enhanced LRC.
    private static IReadOnlyList<LyricWordSegment> ParseWordSegments(string lineContent)
    {
        var matches = InlineWordTimestampRegex.Matches(lineContent);
        if (matches.Count == 0)
        {
            return [];
        }

        var segments = new List<LyricWordSegment>(matches.Count);
        for (var index = 0; index < matches.Count; index++)
        {
            var startIndex = matches[index].Index + matches[index].Length;
            var endIndex = index + 1 < matches.Count
                ? matches[index + 1].Index
                : lineContent.Length;

            var text = lineContent[startIndex..endIndex];
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            segments.Add(new LyricWordSegment(ParseTimestamp(matches[index]), text));
        }

        return segments;
    }
}
