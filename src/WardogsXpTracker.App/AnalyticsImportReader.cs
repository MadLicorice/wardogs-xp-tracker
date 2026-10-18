using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WardogsXpTracker.Core.Analytics;
using WardogsXpTracker.Core.Persistence;
using WardogsXpTracker.Core.Tracking;

namespace WardogsXpTracker.App;

public sealed record AnalyticsImportData(SeasonDocument SourceSeason, SeasonAnalytics Report,
    string SourcePath, bool IsMockData, bool IsAnalyticsExport)
{
    public string FileName => Path.GetFileName(SourcePath);
}

/// <summary>Reads an isolated analytics source. It does not write or merge season data.</summary>
public static class AnalyticsImportReader
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static async Task<AnalyticsImportData?> LoadAsync(string? path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidDataException("Choose an analytics export or season Markdown file.");
        var filename = Path.GetFileName(fullPath);
        if (!filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Choose an analytics export or season file with the .md extension.");

        // The existing reader checks the Markdown heading, one JSON block, format, and container schema.
        var payload = await new MarkdownSaveStore(directory).LoadAsync<JsonElement>(filename, cancellationToken).ConfigureAwait(false);
        if (payload.ValueKind == JsonValueKind.Undefined)
            throw new FileNotFoundException("The selected analytics file no longer exists.", fullPath);
        if (payload.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The imported data must contain an analytics export or a season.");

        var isExport = payload.TryGetProperty("SourceSeason", out var source);
        var isMock = false;
        if (isExport)
        {
            if (source.ValueKind != JsonValueKind.Object
                || !payload.TryGetProperty("Report", out var cachedReport) || cachedReport.ValueKind != JsonValueKind.Object
                || !payload.TryGetProperty("IsMockData", out var mockFlag) || mockFlag.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidDataException("The analytics export is incomplete. It must contain its source season and data label.");
            isMock = mockFlag.GetBoolean();
        }
        else source = payload;

        SeasonDocument season;
        try
        {
            season = source.Deserialize<SeasonDocument>(ReadOptions)
                ?? throw new InvalidDataException("The source season is missing.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new InvalidDataException("The source season is invalid or incomplete.", exception);
        }
        cancellationToken.ThrowIfCancellationRequested();
        TrackingDocumentValidator.Validate(season);
        // Recalculate from validated records and saved rules; cached report values are not authoritative.
        var report = AnalyticsEngine.Build(season);
        cancellationToken.ThrowIfCancellationRequested();
        return new(season, report, fullPath, isMock, isExport);
    }
}
