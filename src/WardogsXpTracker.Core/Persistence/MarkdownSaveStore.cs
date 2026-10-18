using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WardogsXpTracker.Core.Persistence;

/// <summary>Stores local data in a readable Markdown container with a versioned JSON payload.</summary>
public sealed class MarkdownSaveStore
{
    private const string Heading = "# WARDOGS XP Tracker save";
    private const string Format = "WardogsXpTracker.MarkdownSave";
    private const int ContainerSchemaVersion = 1;
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> DirectoryGates = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly SemaphoreSlim gate;

    public MarkdownSaveStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        RootDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        gate = DirectoryGates.GetOrAdd(RootDirectory, static _ => new SemaphoreSlim(1, 1));
    }

    public string RootDirectory { get; }

    /// <summary>Returns null for an absent file. Invalid files remain unchanged and raise an error.</summary>
    public Task<T?> LoadAsync<T>(string filename, CancellationToken cancellationToken = default)
    {
        string path = GetSavePath(filename);
        cancellationToken.ThrowIfCancellationRequested();
        return LoadQueuedAsync<T>(path, cancellationToken);
    }

    private async Task<T?> LoadQueuedAsync<T>(string path, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(async () =>
            {
                if (!File.Exists(path)) return default;
                return await ReadDataAsync<T>(path, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Snapshots the data before scheduling disk work. A replacement retains the previous valid file as .bak.</summary>
    public Task SaveAsync<T>(string filename, T data, CancellationToken cancellationToken = default)
    {
        string path = GetSavePath(filename);
        ArgumentNullException.ThrowIfNull(data);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] snapshot;
        try
        {
            var envelope = new SaveEnvelope<T>(Format, ContainerSchemaVersion, data);
            string json = JsonSerializer.Serialize(envelope, JsonOptions);
            string markdown = $"{Heading}\n\nThis file contains local app data. Keep the JSON block intact.\n\n```json\n{json}\n```\n";
            snapshot = Utf8.GetBytes(markdown);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new InvalidDataException("The save data cannot be serialized. Existing files were not changed.", ex);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return SaveSnapshotAsync<T>(path, snapshot, cancellationToken);
    }

    private async Task SaveSnapshotAsync<T>(string path, byte[] snapshot, CancellationToken cancellationToken)
    {
        // Enqueue before scheduling disk work so later calls cannot overtake an earlier save.
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Task.Run(() => WriteSnapshotAsync<T>(path, snapshot, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task WriteSnapshotAsync<T>(string path, byte[] snapshot, CancellationToken cancellationToken)
    {
        string? temporaryPath = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(RootDirectory);
            if (File.Exists(path))
            {
                // Do not replace an unreadable file with defaults or a newer in-memory value.
                _ = await ReadDataAsync<T>(path, cancellationToken).ConfigureAwait(false);
            }

            temporaryPath = Path.Combine(RootDirectory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            await using (var stream = new FileStream(temporaryPath, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            }))
            {
                await stream.WriteAsync(snapshot, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                // Flush the OS buffers before the atomic replacement. Disk work runs outside the UI thread.
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path))
                File.Replace(temporaryPath, path, path + ".bak");
            else
                File.Move(temporaryPath, path);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static async Task<T?> ReadDataAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            string markdown = await File.ReadAllTextAsync(path, Utf8, cancellationToken).ConfigureAwait(false);
            string json = ExtractJson(markdown);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw InvalidSave("The JSON container must be an object.");

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!names.Add(property.Name) || property.Name is not ("Format" or "SchemaVersion" or "Data"))
                    throw InvalidSave("The JSON container has duplicate or unsupported fields.");
            }
            if (!root.TryGetProperty("Format", out var format) || format.ValueKind != JsonValueKind.String ||
                format.GetString() != Format)
                throw InvalidSave("The save format is unsupported.");
            if (!root.TryGetProperty("SchemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number ||
                !schema.TryGetInt32(out int version) ||
                version != ContainerSchemaVersion)
                throw InvalidSave("The save container version is unsupported.");
            if (!root.TryGetProperty("Data", out var data) || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                throw InvalidSave("The save data is missing.");

            T? result = data.Deserialize<T>(JsonOptions);
            if (result is null) throw InvalidSave("The save data is missing.");
            return result;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or DecoderFallbackException)
        {
            throw new InvalidDataException("The save file is invalid. Existing files were not changed.", ex);
        }
    }

    private static string ExtractJson(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length == 0 || lines[0] != Heading)
            throw InvalidSave("The Markdown save heading is missing or unsupported.");

        int opening = -1, closing = -1;
        for (int index = 1; index < lines.Length; index++)
        {
            if (lines[index] == "```json")
            {
                if (opening >= 0) throw InvalidSave("The save file has more than one JSON block.");
                opening = index;
            }
            else if (lines[index] == "```")
            {
                if (opening < 0 || closing >= 0) throw InvalidSave("The save file has an invalid JSON block.");
                closing = index;
            }
            else if (closing >= 0 && !string.IsNullOrWhiteSpace(lines[index]))
                throw InvalidSave("The save file has unsupported content after its JSON block.");
        }
        if (opening < 0 || closing <= opening + 1)
            throw InvalidSave("The save file has no complete JSON block.");
        return string.Join('\n', lines[(opening + 1)..closing]);
    }

    private string GetSavePath(string filename)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);
        if (!filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(filename[..^3]) || filename.Trim() != filename ||
            filename.IndexOfAny(['/', '\\', ':', '<', '>', '"', '|', '?', '*']) >= 0 ||
            filename.Any(char.IsControl) || Path.IsPathRooted(filename))
            throw new ArgumentException("Use one plain filename with the .md extension.", nameof(filename));

        string deviceName = filename.Split('.')[0].TrimEnd(' ');
        if (deviceName.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
            deviceName.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            deviceName.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
            deviceName.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            (deviceName.Length == 4 && deviceName[3] is >= '1' and <= '9' or '\u00B9' or '\u00B2' or '\u00B3' &&
             (deviceName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || deviceName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Use a filename that is not a reserved Windows device name.", nameof(filename));

        return Path.Combine(RootDirectory, filename);
    }

    private static InvalidDataException InvalidSave(string message) =>
        new($"{message} Existing files were not changed.");

    private sealed record SaveEnvelope<T>(string Format, int SchemaVersion, T Data);
}
