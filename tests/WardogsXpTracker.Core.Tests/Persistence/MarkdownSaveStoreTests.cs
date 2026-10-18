using WardogsXpTracker.Core.Persistence;

namespace WardogsXpTracker.Core.Tests.Persistence;

public sealed class MarkdownSaveStoreTests
{
    [Fact]
    public async Task RoundTripPreservesHistoryAndEnumKeysInReadableMarkdown()
    {
        using var temporary = new TemporaryDirectory();
        var store = new MarkdownSaveStore(temporary.Path);
        var original = new SavePayload
        {
            Name = "Season 2",
            Xp = new() { [TestClass.Recon] = 31175, [TestClass.Driver] = 59653 },
            History = ["Baseline captured", "XP changed"]
        };

        await store.SaveAsync("season-2.md", original);
        var restored = await store.LoadAsync<SavePayload>("season-2.md");

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Xp, restored.Xp);
        Assert.Equal(original.History, restored.History);
        string markdown = await File.ReadAllTextAsync(System.IO.Path.Combine(temporary.Path, "season-2.md"));
        Assert.StartsWith("# WARDOGS XP Tracker save", markdown);
        Assert.Contains("```json", markdown);
        Assert.Contains("\"SchemaVersion\": 1", markdown);
        Assert.Contains("\"Recon\": 31175", markdown);
        Assert.False(File.Exists(System.IO.Path.Combine(temporary.Path, "season-2.md.bak")));
    }

    [Fact]
    public async Task MissingFileDoesNotCreateDirectory()
    {
        using var temporary = new TemporaryDirectory();
        string absentDirectory = System.IO.Path.Combine(temporary.Path, "absent");
        var store = new MarkdownSaveStore(absentDirectory);

        Assert.Null(await store.LoadAsync<SavePayload>("season.md"));
        Assert.False(Directory.Exists(absentDirectory));
    }

    [Fact]
    public async Task ReplacementRetainsMostRecentGoodSaveAsBackup()
    {
        using var temporary = new TemporaryDirectory();
        var store = new MarkdownSaveStore(temporary.Path);
        string path = System.IO.Path.Combine(temporary.Path, "season.md");
        await store.SaveAsync("season.md", new SavePayload { Name = "First" });
        string first = await File.ReadAllTextAsync(path);

        await store.SaveAsync("season.md", new SavePayload { Name = "Second" });
        Assert.Equal(first, await File.ReadAllTextAsync(path + ".bak"));
        string second = await File.ReadAllTextAsync(path);

        await store.SaveAsync("season.md", new SavePayload { Name = "Third" });
        Assert.Equal(second, await File.ReadAllTextAsync(path + ".bak"));
        Assert.Equal("Third", (await store.LoadAsync<SavePayload>("season.md"))!.Name);
        Assert.Empty(Directory.GetFiles(temporary.Path, "*.tmp"));
    }

    [Fact]
    public async Task SaveTakesSnapshotBeforeReturningTask()
    {
        using var temporary = new TemporaryDirectory();
        var store = new MarkdownSaveStore(temporary.Path);
        var mutable = new SavePayload { Name = "Before", History = ["original"] };

        Task save = store.SaveAsync("snapshot.md", mutable);
        mutable.Name = "After";
        mutable.History.Add("later mutation");
        await save;

        var restored = await store.LoadAsync<SavePayload>("snapshot.md");
        Assert.Equal("Before", restored!.Name);
        Assert.Equal(["original"], restored.History);
    }

    [Theory]
    [InlineData("invalid JSON")]
    [InlineData("unsupported version")]
    [InlineData("invalid version type")]
    [InlineData("unsupported format")]
    [InlineData("truncated fence")]
    [InlineData("duplicate field")]
    [InlineData("missing data")]
    [InlineData("wrong data type")]
    [InlineData("missing heading")]
    [InlineData("null data")]
    public async Task InvalidSaveCannotBeLoadedOrOverwritten(string corruption)
    {
        using var temporary = new TemporaryDirectory();
        var store = new MarkdownSaveStore(temporary.Path);
        string path = System.IO.Path.Combine(temporary.Path, "season.md");
        await store.SaveAsync("season.md", new SavePayload { Name = "Original" });
        await store.SaveAsync("season.md", new SavePayload { Name = "Current" });
        string previousBackup = await File.ReadAllTextAsync(path + ".bak");
        string valid = await File.ReadAllTextAsync(path);
        string invalid = corruption switch
        {
            "invalid JSON" => valid.Replace("\"Name\": \"Current\"", "\"Name\": undefined", StringComparison.Ordinal),
            "unsupported version" => valid.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 2", StringComparison.Ordinal),
            "invalid version type" => valid.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": \"one\"", StringComparison.Ordinal),
            "unsupported format" => valid.Replace("WardogsXpTracker.MarkdownSave", "OtherApp.Save", StringComparison.Ordinal),
            "truncated fence" => valid[..valid.LastIndexOf("```", StringComparison.Ordinal)],
            "duplicate field" => valid.Replace("\"SchemaVersion\": 1,", "\"SchemaVersion\": 1, \"SchemaVersion\": 1,", StringComparison.Ordinal),
            "missing data" => valid.Replace("\"Data\":", "\"Unknown\":", StringComparison.Ordinal),
            "wrong data type" => ContainerWithData("123"),
            "missing heading" => valid.Replace("# WARDOGS XP Tracker save", "# Unrelated document", StringComparison.Ordinal),
            "null data" => ContainerWithData("null"),
            _ => throw new InvalidOperationException()
        };
        await File.WriteAllTextAsync(path, invalid);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync<SavePayload>("season.md"));
        await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync("season.md", new SavePayload { Name = "Replacement" }));

        Assert.Equal(invalid, await File.ReadAllTextAsync(path));
        Assert.Equal(previousBackup, await File.ReadAllTextAsync(path + ".bak"));
        Assert.Empty(Directory.GetFiles(temporary.Path, "*.tmp"));
    }

    [Theory]
    [InlineData("../season.md")]
    [InlineData("..\\season.md")]
    [InlineData("sub/season.md")]
    [InlineData("C:\\season.md")]
    [InlineData("season.txt")]
    [InlineData("season.md ")]
    [InlineData(".md")]
    [InlineData("CON.md")]
    [InlineData("nul.MD")]
    [InlineData("COM1.extra.md")]
    [InlineData("COM\u00B9.md")]
    [InlineData("bad:name.md")]
    public async Task FilenamesMustBePlainMarkdownBasenames(string filename)
    {
        using var temporary = new TemporaryDirectory();
        var store = new MarkdownSaveStore(temporary.Path);

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(filename, new SavePayload()));
        await Assert.ThrowsAsync<ArgumentException>(() => store.LoadAsync<SavePayload>(filename));
        Assert.Empty(Directory.GetFiles(temporary.Path));
    }

    [Fact]
    public async Task PreCanceledOperationsLeaveCurrentSaveAndBackupUnchanged()
    {
        using var temporary = new TemporaryDirectory();
        var store = new MarkdownSaveStore(temporary.Path);
        string path = System.IO.Path.Combine(temporary.Path, "season.md");
        await store.SaveAsync("season.md", new SavePayload { Name = "Original" });
        await store.SaveAsync("season.md", new SavePayload { Name = "Current" });
        string current = await File.ReadAllTextAsync(path);
        string backup = await File.ReadAllTextAsync(path + ".bak");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync("season.md", new SavePayload { Name = "Wrong" }, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadAsync<SavePayload>("season.md", cancellation.Token));

        Assert.Equal(current, await File.ReadAllTextAsync(path));
        Assert.Equal(backup, await File.ReadAllTextAsync(path + ".bak"));
        Assert.Empty(Directory.GetFiles(temporary.Path, "*.tmp"));
    }

    [Fact]
    public async Task CancellationDuringSnapshotDoesNotStartDiskWork()
    {
        using var temporary = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var store = new MarkdownSaveStore(temporary.Path);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.SaveAsync("canceled.md", new CancelingPayload(cancellation), cancellation.Token));

        Assert.Empty(Directory.GetFiles(temporary.Path));
    }

    [Fact]
    public async Task ConcurrentStoresWriteCompleteSnapshotsWithoutTemporaryFiles()
    {
        using var temporary = new TemporaryDirectory();
        var first = new MarkdownSaveStore(temporary.Path);
        var second = new MarkdownSaveStore(temporary.Path);
        var pending = Enumerable.Range(0, 16).Select(index =>
            (index % 2 == 0 ? first : second).SaveAsync("season.md", new SavePayload { Name = $"Save {index}" })).ToArray();

        await Task.WhenAll(pending);

        var result = await first.LoadAsync<SavePayload>("season.md");
        Assert.Equal("Save 15", result!.Name);
        Assert.True(File.Exists(System.IO.Path.Combine(temporary.Path, "season.md.bak")));
        Assert.Empty(Directory.GetFiles(temporary.Path, "*.tmp"));
    }

    private static string ContainerWithData(string json) =>
        $"# WARDOGS XP Tracker save\n\n```json\n{{\"Format\":\"WardogsXpTracker.MarkdownSave\",\"SchemaVersion\":1,\"Data\":{json}}}\n```\n";

    public enum TestClass { Recon, Driver }

    public sealed class SavePayload
    {
        public string Name { get; set; } = "Season";
        public Dictionary<TestClass, int> Xp { get; set; } = [];
        public List<string> History { get; set; } = [];
    }

    public sealed class CancelingPayload(CancellationTokenSource cancellation)
    {
        public string Value
        {
            get
            {
                cancellation.Cancel();
                return "cancel before disk work";
            }
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"WardogsXpTracker-save-tests-{Guid.NewGuid():N}");

        public TemporaryDirectory() => Directory.CreateDirectory(Path);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
